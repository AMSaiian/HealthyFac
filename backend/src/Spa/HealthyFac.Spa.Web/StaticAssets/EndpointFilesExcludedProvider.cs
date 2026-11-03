using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace HealthyFac.Spa.Web.StaticAssets;

/// <summary>
/// Hides the files that an endpoint serves, so the static file middleware never answers for them. The index file
/// needs a nonce, and the runtime configuration comes from the host settings, never from a file.
/// </summary>
public sealed class EndpointFilesExcludedProvider(IFileProvider inner) : IFileProvider
{
    private static readonly string[] ExcludedSubpaths =
        [$"/{SpaConstants.IndexFileName}", SpaConstants.RuntimeConfigRoute];

    private readonly IFileProvider _inner = inner;

    public IFileInfo GetFileInfo(string subpath) =>
        IsExcluded(subpath) ? new NotFoundFileInfo(subpath) : _inner.GetFileInfo(subpath);

    public IDirectoryContents GetDirectoryContents(string subpath) => _inner.GetDirectoryContents(subpath);

    public IChangeToken Watch(string filter) => _inner.Watch(filter);

    private static bool IsExcluded(string subpath) =>
        ExcludedSubpaths.Contains(subpath, StringComparer.OrdinalIgnoreCase);
}
