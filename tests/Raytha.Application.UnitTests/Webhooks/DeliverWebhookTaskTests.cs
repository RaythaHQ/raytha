using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Webhooks;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Webhooks;

public class DeliverWebhookTaskTests
{
    private const string HookUrl = "http://receiver.invalid/hook";

    private Webhook _webhook = null!;
    private WebhookDelivery _delivery = null!;
    private BackgroundTask _job = null!;
    private StubHandler _http = null!;
    private DeliverWebhookTask _task = null!;

    [SetUp]
    public void Setup()
    {
        _webhook = new Webhook
        {
            Id = Guid.NewGuid(),
            Name = "receiver",
            Url = HookUrl,
            Secret = "0123456789abcdef0123456789abcdef",
            MaxAttempts = 3,
            TimeoutSeconds = 5,
        };
        _delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookId = _webhook.Id,
            EventName = "content_item.created",
            Payload = "{\"hello\":\"world\"}",
            Status = WebhookDeliveryStatus.Pending,
            NextRetryAt = DateTime.UtcNow.AddMinutes(10),
        };
        _job = new BackgroundTask { Id = Guid.NewGuid(), Name = "job", Args = "{}" };

        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.Webhooks).Returns(new[] { _webhook }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.WebhookDeliveries)
            .Returns(new[] { _delivery }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.BackgroundTasks).Returns(new[] { _job }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _http = new StubHandler();
        var factory = new Mock<IHttpClientFactory>();
        factory
            .Setup(f => f.CreateClient(DeliverWebhookTask.HttpClientName))
            .Returns(() => new HttpClient(_http, disposeHandler: false));

        _task = new DeliverWebhookTask(db.Object, factory.Object, NullLogger<DeliverWebhookTask>.Instance);
    }

    [Test]
    public async Task A_failed_attempt_schedules_the_retry_and_leaves_the_delivery_pending()
    {
        _http.Respond(HttpStatusCode.InternalServerError);
        var before = DateTime.UtcNow;

        await Execute();

        var after = DateTime.UtcNow;
        _http.Requests.Should().Be(1);
        _delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        _delivery.AttemptCount.Should().Be(1);
        _delivery.ResponseCode.Should().Be(500);
        _delivery.ErrorMessage.Should().Be("Received HTTP 500.");
        _delivery.CompletionTime.Should().BeNull();
        _delivery.NextRetryAt.Should().BeOnOrAfter(before.AddSeconds(2)).And.BeOnOrBefore(after.AddSeconds(2));
        _job.PercentComplete.Should().Be(100);
        _job.StatusInfo.Should().Be(
            $"Attempt 1 of 3 failed (Received HTTP 500.); retry in 2s at {_delivery.NextRetryAt:yyyy-MM-dd HH:mm:ss} UTC."
        );
    }

    [Test]
    public async Task The_backoff_follows_the_delivery_attempt_count_not_the_run()
    {
        _delivery.AttemptCount = 1;
        _http.Respond(HttpStatusCode.BadGateway);
        var before = DateTime.UtcNow;

        await Execute();

        _http.Requests.Should().Be(1);
        _delivery.AttemptCount.Should().Be(2);
        _delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        _delivery.NextRetryAt.Should().BeOnOrAfter(before.AddSeconds(4)).And.BeOnOrBefore(DateTime.UtcNow.AddSeconds(4));
    }

    [Test]
    public async Task The_last_allowed_attempt_failing_marks_the_delivery_failed()
    {
        _delivery.AttemptCount = 2;
        _http.Respond(HttpStatusCode.InternalServerError);

        await Execute();

        _http.Requests.Should().Be(1);
        _delivery.Status.Should().Be(WebhookDeliveryStatus.Failed);
        _delivery.AttemptCount.Should().Be(3);
        _delivery.NextRetryAt.Should().BeNull();
        _delivery.CompletionTime.Should().NotBeNull();
        _job.StatusInfo.Should().Be("Failed after 3 attempt(s): Received HTTP 500.");
    }

    [Test]
    public async Task A_successful_attempt_marks_the_delivery_succeeded_and_clears_the_lease()
    {
        _http.Respond(HttpStatusCode.OK, "ok");

        await Execute();

        _http.Requests.Should().Be(1);
        _delivery.Status.Should().Be(WebhookDeliveryStatus.Succeeded);
        _delivery.AttemptCount.Should().Be(1);
        _delivery.ResponseCode.Should().Be(200);
        _delivery.ResponseBody.Should().Be("ok");
        _delivery.NextRetryAt.Should().BeNull();
        _delivery.CompletionTime.Should().NotBeNull();
        _job.StatusInfo.Should().Be($"Delivered to {HookUrl} (HTTP 200).");
        _job.PercentComplete.Should().Be(100);
    }

    [Test]
    public async Task The_signature_covers_the_timestamp_header_and_the_body()
    {
        _http.Respond(HttpStatusCode.OK);

        await Execute();

        var timestamp = _http.LastHeaders[WebhookSigner.TimestampHeader];
        var expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_webhook.Secret),
            Encoding.UTF8.GetBytes($"{timestamp}.{_http.LastBody}")
        );
        _http.LastBody.Should().Be(_delivery.Payload);
        _http.LastHeaders[WebhookSigner.SignatureHeader]
            .Should()
            .Be($"sha256={Convert.ToHexStringLower(expected)}");
    }

    [Test]
    public async Task A_delivery_that_already_succeeded_makes_no_request()
    {
        _delivery.Status = WebhookDeliveryStatus.Succeeded;
        _http.Respond(HttpStatusCode.OK);

        await Execute();

        _http.Requests.Should().Be(0);
        _job.StatusInfo.Should().Be("Delivery already succeeded.");
    }

    [Test]
    public async Task A_delivery_that_already_failed_makes_no_request()
    {
        _delivery.Status = WebhookDeliveryStatus.Failed;
        _delivery.AttemptCount = 3;
        _http.Respond(HttpStatusCode.OK);

        await Execute();

        _http.Requests.Should().Be(0);
        _delivery.AttemptCount.Should().Be(3);
        _job.StatusInfo.Should().Be("Delivery already failed.");
    }

    [TestCase(1, 2)]
    [TestCase(2, 4)]
    [TestCase(3, 8)]
    [TestCase(4, 16)]
    [TestCase(5, 32)]
    public void ComputeBackoff_DoublesEachAttempt(int attempt, int expectedSeconds)
    {
        DeliverWebhookTask.ComputeBackoff(attempt).Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Test]
    public void ComputeBackoff_IsCappedAtSixtySeconds()
    {
        DeliverWebhookTask.ComputeBackoff(6).Should().Be(TimeSpan.FromSeconds(60));
        DeliverWebhookTask.ComputeBackoff(50).Should().Be(TimeSpan.FromSeconds(60));
    }

    [Test]
    public void ComputeBackoff_TreatsNonPositiveAttemptsAsFirst()
    {
        DeliverWebhookTask.ComputeBackoff(0).Should().Be(TimeSpan.FromSeconds(2));
        DeliverWebhookTask.ComputeBackoff(-3).Should().Be(TimeSpan.FromSeconds(2));
    }

    private Task Execute()
    {
        var args = JsonSerializer.SerializeToElement(new DeliverWebhookTask.Args { DeliveryId = _delivery.Id });
        return _task.Execute(_job.Id, args, CancellationToken.None);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private HttpStatusCode _status = HttpStatusCode.OK;
        private string _body = string.Empty;

        public int Requests { get; private set; }
        public Dictionary<string, string> LastHeaders { get; } = new();
        public string LastBody { get; private set; } = string.Empty;

        public void Respond(HttpStatusCode status, string body = "")
        {
            _status = status;
            _body = body;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;
            LastHeaders.Clear();
            foreach (var header in request.Headers)
            {
                LastHeaders[header.Key] = string.Join(",", header.Value);
            }
            LastBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status) { Content = new StringContent(_body) };
        }
    }
}
