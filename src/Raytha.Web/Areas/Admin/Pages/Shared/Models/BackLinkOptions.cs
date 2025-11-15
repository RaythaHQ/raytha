public class BackLinkOptions
{
    public string Page { get; set; }
    public string Area { get; set; }
    public string Handler { get; set; }
    public string Fragment { get; set; }
    public RouteValueDictionary RouteValues { get; set; } = new();
    public string Href { get; set; }

    public string Text { get; set; } = "Back";
    public string Class { get; set; }
    public string IconSvg { get; set; }
}
