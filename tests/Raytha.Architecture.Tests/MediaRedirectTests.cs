using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Web.Areas.Admin.Endpoints;

namespace Raytha.Architecture.Tests;

public class MediaRedirectTests
{
    [Test]
    public async Task Redirecting_by_object_key_to_local_storage_does_not_name_a_host()
    {
        var storage = new Mock<IFileStorageProvider>();
        storage
            .Setup(s => s.GetDownloadUrlAsync("abc.png", It.IsAny<DateTime>(), It.IsAny<bool>()))
            .ReturnsAsync("http://localhost:5200/_static-files/abc.png");

        var result = await MediaItemsEndpoints.RedirectToFileUrlByObjectKey("abc.png", storage.Object);

        result.Should().BeOfType<RedirectHttpResult>().Which.Url.Should().Be("/_static-files/abc.png");
    }

    [Test]
    public async Task Redirecting_by_object_key_to_a_cloud_url_keeps_it_absolute()
    {
        const string presigned = "https://bucket.s3.amazonaws.com/abc.png?X-Amz-Signature=1";
        var storage = new Mock<IFileStorageProvider>();
        storage
            .Setup(s => s.GetDownloadUrlAsync("abc.png", It.IsAny<DateTime>(), It.IsAny<bool>()))
            .ReturnsAsync(presigned);

        var result = await MediaItemsEndpoints.RedirectToFileUrlByObjectKey("abc.png", storage.Object);

        result.Should().BeOfType<RedirectHttpResult>().Which.Url.Should().Be(presigned);
    }
}
