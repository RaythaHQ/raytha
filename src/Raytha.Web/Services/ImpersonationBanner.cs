using System;
using System.Net;
using System.Security.Claims;
using Raytha.Application.Common.Security;

namespace Raytha.Web.Services;

/// <summary>
/// The fixed bar shown on server-rendered pages while an admin is signed in as someone else.
/// Self-contained markup, so it works on any site builder's template without a stylesheet.
/// </summary>
public static class ImpersonationBanner
{
    /// <summary>Null unless <paramref name="user"/> is an impersonation session.</summary>
    public static string? For(ClaimsPrincipal user, string pathBase)
    {
        var impersonatorName = user.FindFirstValue(RaythaClaimTypes.ImpersonatorName);
        if (user.FindFirstValue(RaythaClaimTypes.ImpersonatorId) is null || impersonatorName is null)
            return null;

        var name =
            $"{user.FindFirstValue(ClaimTypes.GivenName)} {user.FindFirstValue(ClaimTypes.Surname)}".Trim();
        var email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var stopUrl = $"{pathBase}/raytha/api/auth/impersonation/stop";

        return $$"""
            <div aria-hidden="true" style="height:64px"></div>
            <div id="raytha-impersonation" role="status" style="position:fixed;left:0;right:0;bottom:0;z-index:2147483647;display:flex;flex-wrap:wrap;align-items:center;justify-content:center;gap:8px 16px;padding:12px 16px;background:#fffbeb;color:#422006;border-top:1px solid #f59e0b;box-shadow:0 -4px 12px rgba(0,0,0,.08);font:14px/1.4 system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;text-align:left">
            <span>Viewing the site as <strong>{{Encode(name)}}</strong> ({{Encode(email)}}). Signed in by {{Encode(impersonatorName)}}.</span>
            <button type="button" data-stop-url="{{Encode(stopUrl)}}" style="appearance:none;cursor:pointer;padding:6px 12px;border:1px solid #d97706;border-radius:6px;background:#fff;color:#422006;font:inherit;font-weight:600">End impersonation</button>
            <span data-error hidden style="color:#991b1b"></span>
            </div>
            <script>
            (function () {
              var banner = document.getElementById("raytha-impersonation");
              var button = banner.querySelector("button");
              var error = banner.querySelector("[data-error]");
              button.addEventListener("click", function () {
                button.disabled = true;
                error.hidden = true;
                fetch(button.getAttribute("data-stop-url"), {
                  method: "POST",
                  headers: { "Content-Type": "application/json", "Accept": "application/json" },
                  body: "{}",
                  credentials: "same-origin"
                })
                  .then(function (response) {
                    if (!response.ok) throw new Error(String(response.status));
                    return response.json();
                  })
                  .then(function (payload) {
                    window.location.assign(payload.redirectUrl);
                  })
                  .catch(function () {
                    button.disabled = false;
                    error.textContent = "Could not end impersonation. Try again, or sign out.";
                    error.hidden = false;
                  });
              });
            })();
            </script>
            """;
    }

    /// <summary>Places <paramref name="banner"/> before the last <c>&lt;/body&gt;</c>, or at the end.</summary>
    public static string Insert(string html, string banner)
    {
        var index = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        return index < 0 ? html + banner : html.Insert(index, banner);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
