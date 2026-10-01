using System.Text.Json;
using CSharpVitamins;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Webhooks;
using Raytha.Application.Webhooks.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Webhooks;

/// <summary>
/// Every path that hands a delivery to a background job must lease it through NextRetryAt,
/// so a delivery whose job died is picked up again by the scheduler.
/// </summary>
public class WebhookDeliveryLeaseTests
{
    private Webhook _webhook = null!;
    private List<WebhookDelivery> _deliveries = null!;
    private Mock<IRaythaDbContext> _db = null!;
    private Mock<IBackgroundTaskQueue> _queue = null!;
    private object? _enqueuedArgs;

    [SetUp]
    public void Setup()
    {
        _webhook = new Webhook
        {
            Id = Guid.NewGuid(),
            Name = "receiver",
            Url = "http://receiver.invalid/hook",
            SubscribedEvents = new[] { Webhook.SubscribeToAll },
        };
        _deliveries = new List<WebhookDelivery>();

        var deliverySet = _deliveries.AsQueryable().BuildMockDbSet();
        deliverySet
            .Setup(s => s.Add(It.IsAny<WebhookDelivery>()))
            .Callback<WebhookDelivery>(d => _deliveries.Add(d));

        _db = new Mock<IRaythaDbContext>();
        _db.Setup(x => x.Webhooks).Returns(new[] { _webhook }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.WebhookDeliveries).Returns(deliverySet.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _queue = new Mock<IBackgroundTaskQueue>();
        _queue
            .Setup(q => q.EnqueueAsync<DeliverWebhookTask>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((args, _) => _enqueuedArgs = args)
            .ReturnsAsync(Guid.NewGuid());
    }

    [Test]
    public async Task Publishing_creates_a_pending_delivery_leased_for_ten_minutes()
    {
        var publisher = new WebhookEventPublisher(
            _db.Object,
            _queue.Object,
            NullLogger<WebhookEventPublisher>.Instance
        );
        var before = DateTime.UtcNow;

        var deliveryId = await publisher.PublishToAsync(
            _webhook.Id,
            "content_item.created",
            new { id = 1 },
            CancellationToken.None
        );

        var delivery = _deliveries.Should().ContainSingle().Subject;
        delivery.Id.Should().Be(deliveryId);
        delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        delivery
            .NextRetryAt.Should()
            .BeOnOrAfter(before.AddMinutes(10))
            .And.BeOnOrBefore(DateTime.UtcNow.AddMinutes(10));
        EnqueuedDeliveryId().Should().Be(deliveryId);
    }

    [Test]
    public async Task Redelivering_resets_the_delivery_and_leases_it_for_ten_minutes()
    {
        var delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookId = _webhook.Id,
            Status = WebhookDeliveryStatus.Failed,
            AttemptCount = 3,
            ErrorMessage = "Received HTTP 500.",
            ResponseCode = 500,
            CompletionTime = DateTime.UtcNow.AddHours(-1),
        };
        _deliveries.Add(delivery);
        var handler = new RedeliverWebhookDelivery.Handler(_db.Object, _queue.Object);
        var before = DateTime.UtcNow;

        var response = await handler.Handle(
            new RedeliverWebhookDelivery.Command { Id = new ShortGuid(delivery.Id) },
            CancellationToken.None
        );

        response.Success.Should().BeTrue();
        delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        delivery.AttemptCount.Should().Be(0);
        delivery.ErrorMessage.Should().BeNull();
        delivery.ResponseCode.Should().BeNull();
        delivery.CompletionTime.Should().BeNull();
        delivery
            .NextRetryAt.Should()
            .BeOnOrAfter(before.AddMinutes(10))
            .And.BeOnOrBefore(DateTime.UtcNow.AddMinutes(10));
        EnqueuedDeliveryId().Should().Be(delivery.Id);
    }

    private Guid EnqueuedDeliveryId()
    {
        _enqueuedArgs.Should().NotBeNull();
        return JsonSerializer
            .SerializeToElement(_enqueuedArgs)
            .GetProperty(nameof(DeliverWebhookTask.Args.DeliveryId))
            .GetGuid();
    }
}
