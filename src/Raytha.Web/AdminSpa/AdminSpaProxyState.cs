namespace Raytha.Web.AdminSpa;

/// <summary>
/// Shared by <see cref="ViteDevServerHostedService"/> and the <c>/raytha</c> pipeline: only proxy to
/// Vite after GET <c>/raytha/</c> actually returns 200. A process listening on the port is not
/// enough — a leftover Vite from another checkout 404s every admin URL.
/// </summary>
public sealed class AdminSpaProxyState
{
    public volatile bool ViteReady;
}
