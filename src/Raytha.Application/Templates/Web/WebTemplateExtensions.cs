using System.Text.RegularExpressions;

namespace Raytha.Application.Templates.Web;

public static class WebTemplateExtensions
{
    public const string RENDERBODY_REGEX = @"{%\s*\b(renderbody)\s*%}";

    public static bool HasRenderBodyTag(string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;
        return Regex.IsMatch(s, RENDERBODY_REGEX, RegexOptions.IgnoreCase);
    }

    public static string ContentAssembledFromParents(string currentContent, WebTemplateDto parent)
    {
        if (parent == null)
            return currentContent;
        var updatedContent = Regex.Replace(parent.Content, RENDERBODY_REGEX, currentContent, RegexOptions.IgnoreCase);
        return ContentAssembledFromParents(updatedContent, parent.ParentTemplate);
    }
}
