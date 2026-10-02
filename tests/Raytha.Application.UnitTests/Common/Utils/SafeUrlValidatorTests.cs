using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.Themes.Commands;
using Raytha.Application.Webhooks;

namespace Raytha.Application.UnitTests.Common.Utils;

public class SafeUrlValidatorTests
{
    [TestCase(DeliverWebhookTask.HttpClientName, "http://127.0.0.1:1/")]
    [TestCase(DeliverWebhookTask.HttpClientName, "http://localhost:1/")]
    [TestCase(nameof(BeginImportThemeFromUrl), "http://127.0.0.1:1/theme.json")]
    [TestCase(nameof(BeginImportContentItemsFromCsv), "http://127.0.0.1:1/file.png")]
    public async Task Outbound_clients_refuse_internal_addresses_by_default(string clientName, string url)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<ISecurityConfiguration>(c => c.AllowInternalUrlImports == false));
        services.AddApplicationServices();
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName);

        var act = () => client.GetAsync(url);

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Which.Message.Should()
            .Contain("internal network address");
    }
}
