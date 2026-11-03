namespace HealthyFac.Spa.Web.StaticAssets;

/// <summary>
/// Tells a static file request from a request that the index or the runtime configuration endpoint answers.
/// </summary>
public static class StaticAssetRequests
{
    /// <summary>
    /// Checks whether a path names a file. The index and the runtime configuration are files by name, but the host
    /// serves them through endpoints, so they count as no static asset.
    /// </summary>
    /// <param name="path">The path of the request.</param>
    /// <returns><see langword="true"/> when the path has a file extension and no endpoint serves it.</returns>
    public static bool IsStaticAsset(PathString path) =>
        Path.HasExtension(path.Value)
        && !path.Equals($"/{SpaConstants.IndexFileName}", StringComparison.OrdinalIgnoreCase)
        && !path.Equals(SpaConstants.RuntimeConfigRoute, StringComparison.OrdinalIgnoreCase);
}
