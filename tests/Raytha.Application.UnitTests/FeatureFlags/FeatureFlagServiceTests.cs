using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Raytha.Application.FeatureFlags;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.FeatureFlags;

public class FeatureFlagServiceTests
{
    private Mock<IFeatureFlagStore> _store = null!;
    private FeatureFlagService _service = null!;

    [SetUp]
    public void Setup()
    {
        _store = new Mock<IFeatureFlagStore>();
        _service = new FeatureFlagService(_store.Object, NullLogger<FeatureFlagService>.Instance);
    }

    private void StoreReturns(params (string Key, bool Enabled)[] overrides)
    {
        _store
            .Setup(s => s.GetAllAsync(FeatureFlag.GlobalScope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(overrides.ToDictionary(o => o.Key, o => o.Enabled, StringComparer.OrdinalIgnoreCase));
    }

    [Test]
    public async Task IsEnabled_UnknownKey_FailsClosed_WithoutTouchingStore()
    {
        var result = await _service.IsEnabledAsync("not_a_flag");

        result.Should().BeFalse();
        _store.VerifyNoOtherCalls();
    }

    [Test]
    public async Task IsEnabled_UsesCodeDefault_WhenNoOverrideExists()
    {
        StoreReturns();

        (await _service.IsEnabledAsync(RaythaFeatureFlags.Webhooks)).Should().BeTrue();
        (await _service.IsEnabledAsync(RaythaFeatureFlags.AdminSpa)).Should().BeFalse();
    }

    [Test]
    public async Task IsEnabled_OverrideWins_OverDefault()
    {
        StoreReturns((RaythaFeatureFlags.Webhooks, false), (RaythaFeatureFlags.AdminSpa, true));

        (await _service.IsEnabledAsync(RaythaFeatureFlags.Webhooks)).Should().BeFalse();
        (await _service.IsEnabledAsync(RaythaFeatureFlags.AdminSpa)).Should().BeTrue();
    }

    [Test]
    public async Task IsEnabled_FailsClosed_WhenStoreThrows()
    {
        _store
            .Setup(s => s.GetAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        (await _service.IsEnabledAsync(RaythaFeatureFlags.Webhooks)).Should().BeFalse();
    }

    [Test]
    public async Task List_ReportsSourceForEachDefinition()
    {
        StoreReturns((RaythaFeatureFlags.AdminSpa, true));

        var list = await _service.ListAsync();

        list.Should().HaveCount(RaythaFeatureFlags.All.Count);
        list.Single(f => f.Definition.Key == RaythaFeatureFlags.AdminSpa)
            .Should()
            .BeEquivalentTo(new { IsEnabled = true, Source = "override" });
        list.Single(f => f.Definition.Key == RaythaFeatureFlags.Webhooks)
            .Should()
            .BeEquivalentTo(new { IsEnabled = true, Source = "default" });
    }

    [Test]
    public async Task Set_UnknownKey_Throws()
    {
        var act = () => _service.SetAsync("nope", true);

        await act.Should().ThrowAsync<ArgumentException>();
        _store.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Set_NormalizesKeyCasing_ToCanonicalDefinition()
    {
        await _service.SetAsync("WEBHOOKS", false);

        _store.Verify(
            s => s.SetAsync(FeatureFlag.GlobalScope, RaythaFeatureFlags.Webhooks, false, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
