using FluentAssertions;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.UnitTests.Common.Utils;

public class PublicAssetUrlTests
{
    [Test]
    public void A_local_static_file_url_becomes_root_relative()
    {
        PublicAssetUrl
            .PreferRootRelative("http://localhost:5200/_static-files/key_name.css")
            .Should()
            .Be("/_static-files/key_name.css");
    }

    [Test]
    public void A_path_base_stays_on_the_root_relative_url()
    {
        PublicAssetUrl
            .PreferRootRelative("https://cms.example.com/site/_static-files/key.css")
            .Should()
            .Be("/site/_static-files/key.css");
    }

    [Test]
    public void A_presigned_cloud_url_stays_absolute()
    {
        const string url = "https://bucket.s3.amazonaws.com/key.css?X-Amz-Signature=abc";

        PublicAssetUrl.PreferRootRelative(url).Should().Be(url);
    }
}
