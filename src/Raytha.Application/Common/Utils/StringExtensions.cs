using Raytha.Domain.ValueObjects;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Raytha.Application.Common.Utils;

public static class StringExtensions
{
    public static string ToDeveloperName(this string source)
    {
        if (string.IsNullOrEmpty(source))
            return string.Empty;
        source = source.ToLower().Trim();
        source = Regex.Replace(source, "[^a-zA-Z0-9]", "_");
        return source;
    }

    public static bool IsValidDeveloperName(string source)
    {
        return !string.IsNullOrEmpty(source.ToDeveloperName()) && !IsProtectedRoutePath(source.ToDeveloperName());
    }

    public static bool IsValidUriFormat(this string link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return false;
        }

        Uri outUri;
        return Uri.TryCreate(link, UriKind.Absolute, out outUri)
            && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps);
    }

    public static string IfNullOrEmpty(this string value, string swap)
    {
        if (string.IsNullOrEmpty(value))
            return swap;
        return value;
    }

    public static string[] SplitIntoSeparateEmailAddresses(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new string[0];

        char[] delimiters = new[] { ',', ';', ' ' };
        var emailArray = value.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        return emailArray;
    }

    public static bool IsValidEmailAddress(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        var trimmedEmail = value.Trim();

        if (trimmedEmail.EndsWith("."))
        {
            return false;
        }
        try
        {
            var addr = new System.Net.Mail.MailAddress(value);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }

    public static string YesOrNo(this bool value)
    {
        return value ? "Yes" : "No";
    }

    public static bool IsProtectedRoutePath(this string value)
    {
        value = value.ToUrlSlug().ToLower();
        bool isRaythaPath = value == "raytha" || value.StartsWith("raytha/") || ProtectedRoutePaths().Any(p => value.StartsWith(p));
        return isRaythaPath;
    }

    public static IEnumerable<string> ProtectedRoutePaths()
    {
        yield return "account/login";
        yield return "account/logout";
        yield return "account/me";
        yield return "account/create";
    }

    static readonly Regex WordDelimiters = new Regex(@"[\s—–]", RegexOptions.Compiled);

    // characters that are not valid
    static readonly Regex InvalidChars = new Regex(@"[^_/a-zA-Z0-9\-]", RegexOptions.Compiled);

    // multiple hyphens
    static readonly Regex MultipleHyphens = new Regex(@"-{2,}", RegexOptions.Compiled);

    public static string ToUrlSlug(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // remove diacritics (accents)
        value = RemoveDiacritics(value);

        // ensure all word delimiters are hyphens
        value = WordDelimiters.Replace(value, "-");

        // strip out invalid characters
        value = InvalidChars.Replace(value, "");

        // replace multiple hyphens (-) with a single hyphen
        value = MultipleHyphens.Replace(value, "-");

        // trim hyphens (-) from ends
        return value.Trim().Trim('-', '/');
    }

    /// See: http://www.siao2.com/2007/05/14/2629747.aspx
    private static string RemoveDiacritics(string stIn)
    {
        string stFormD = stIn.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();

        for (int ich = 0; ich < stFormD.Length; ich++)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(stFormD[ich]);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(stFormD[ich]);
            }
        }

        return (sb.ToString().Normalize(NormalizationForm.FormC));
    }

    public static string? Truncate(this string? value, int maxLength, string truncationSuffix = "…")
    {
        return value?.Length > maxLength
            ? value.Substring(0, maxLength) + truncationSuffix
            : value;
    }

    public static string StripHtml(this string input)
    {
        if (input == null)
            return string.Empty;

        // Will this simple expression replace all tags???
        var tagsExpression = new Regex(@"</?.+?>");
        return tagsExpression.Replace(input, " ");
    }

    public static (string column, SortOrder sortOrder) SplitIntoColumnAndSortOrder(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return (null, null);
        
        var (column, directionString) = input.Split(' ', 2) switch
        {
            var arr when arr.Length == 2 => (arr[0], arr[1]),
            var arr when arr.Length == 1 => (arr[0], string.Empty),
            _ => (string.Empty, string.Empty)
        };
        var direction = SortOrder.From(directionString);
        return (column, direction);
    }
}
