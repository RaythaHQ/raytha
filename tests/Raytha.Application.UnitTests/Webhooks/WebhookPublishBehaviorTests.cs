using System.Text.Json.Nodes;
using CSharpVitamins;
using FluentAssertions;
using Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Raytha.Application.Common.Behaviors;
using Raytha.Application.Common.Models;
using Raytha.Application.FeatureFlags;
using Raytha.Application.Users.Commands;
using Raytha.Application.Webhooks;
using Raytha.Application.Webhooks.Commands;

namespace Raytha.Application.UnitTests.Webhooks;

public class WebhookPublishBehaviorTests
{
    private Mock<IWebhookEventPublisher> _publisher = null!;
    private Mock<IFeatureFlagService> _featureFlags = null!;

    [SetUp]
    public void Setup()
    {
        _publisher = new Mock<IWebhookEventPublisher>();
        _featureFlags = new Mock<IFeatureFlagService>();
        _featureFlags
            .Setup(f => f.IsEnabledAsync(RaythaFeatureFlags.Webhooks, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    [Test]
    public void Sanitize_StripsCredentialLikeProperties()
    {
        var node = WebhookPublishBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>.Sanitize(
            new
            {
                EmailAddress = "a@b.com",
                Password = "hunter2",
                Nested = new { ApiKey = "k", Name = "n" },
                Items = new[] { new { Token = "t", Label = "l" } },
            }
        );

        var json = node!.ToJsonString();
        json.Should().Contain("a@b.com").And.Contain("\"name\":\"n\"").And.Contain("\"label\":\"l\"");
        json.Should().NotContain("hunter2").And.NotContain("\"apiKey\"").And.NotContain("\"token\"");
    }

    [Test]
    public async Task Handle_PublishesEvent_WhenAnnotatedCommandSucceeds()
    {
        var behavior = new WebhookPublishBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>(
            _publisher.Object,
            _featureFlags.Object,
            NullLogger<WebhookPublishBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>>.Instance
        );
        var id = ShortGuid.NewGuid();

        var response = await behavior.Handle(
            new CreateUser.Command { EmailAddress = "a@b.com", FirstName = "Ada" },
            (_, _) => ValueTask.FromResult(new CommandResponseDto<ShortGuid>(id)),
            CancellationToken.None
        );

        response.Result.Should().Be(id);
        _publisher.Verify(
            p =>
                p.PublishAsync(
                    "user.created",
                    It.Is<object>(o => o.ToString()!.Contains("a@b.com") && o.ToString()!.Contains(id.ToString())),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_DoesNotPublish_WhenCommandFailsValidation()
    {
        var behavior = new WebhookPublishBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>(
            _publisher.Object,
            _featureFlags.Object,
            NullLogger<WebhookPublishBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>>.Instance
        );

        await behavior.Handle(
            new CreateUser.Command(),
            (_, _) => ValueTask.FromResult(new CommandResponseDto<ShortGuid>("EmailAddress", "required")),
            CancellationToken.None
        );

        _publisher.Verify(
            p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_DoesNotPublish_WhenWebhooksFlagIsOff()
    {
        _featureFlags
            .Setup(f => f.IsEnabledAsync(RaythaFeatureFlags.Webhooks, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var behavior = new WebhookPublishBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>(
            _publisher.Object,
            _featureFlags.Object,
            NullLogger<WebhookPublishBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>>.Instance
        );

        await behavior.Handle(
            new CreateUser.Command(),
            (_, _) => ValueTask.FromResult(new CommandResponseDto<ShortGuid>(ShortGuid.NewGuid())),
            CancellationToken.None
        );

        _publisher.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_IgnoresUnannotatedCommands()
    {
        var behavior = new WebhookPublishBehavior<TestWebhook.Command, CommandResponseDto<ShortGuid>>(
            _publisher.Object,
            _featureFlags.Object,
            NullLogger<WebhookPublishBehavior<TestWebhook.Command, CommandResponseDto<ShortGuid>>>.Instance
        );

        await behavior.Handle(
            new TestWebhook.Command(),
            (_, _) => ValueTask.FromResult(new CommandResponseDto<ShortGuid>(ShortGuid.NewGuid())),
            CancellationToken.None
        );

        _publisher.VerifyNoOtherCalls();
        _featureFlags.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_SwallowsPublisherFailures()
    {
        _publisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var behavior = new WebhookPublishBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>(
            _publisher.Object,
            _featureFlags.Object,
            NullLogger<WebhookPublishBehavior<CreateUser.Command, CommandResponseDto<ShortGuid>>>.Instance
        );
        var id = ShortGuid.NewGuid();

        var response = await behavior.Handle(
            new CreateUser.Command(),
            (_, _) => ValueTask.FromResult(new CommandResponseDto<ShortGuid>(id)),
            CancellationToken.None
        );

        response.Success.Should().BeTrue();
        response.Result.Should().Be(id);
    }
}
