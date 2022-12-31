namespace Raytha.Web.Areas.Admin.Views.MediaItems;

public class MediaItemPresignRequest_ViewModel
{
    public string filename { get; set; }
    public string contentType { get; set; }
    public string extension { get; set; }
}

public class MediaItemCreateAfterUpload_ViewModel
{
    public string id { get; set; }
    public string filename { get; set; }
    public string contentType { get; set; }
    public string extension { get; set; }
    public string objectKey { get; set; }
    public long length { get; set; }
    public string contentDisposition { get; set; }
}