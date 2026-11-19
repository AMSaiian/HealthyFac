using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// Chooses the answer for a template version that a load did not find.
/// </summary>
internal static class ShiftVersionRejections
{
    /// <summary>
    /// Gives the exception for a missing version. A version that another change deleted is a conflict (the client
    /// read it before), and an ID that never existed is "not found".
    /// </summary>
    /// <param name="dbContext">Looks for the deleted row.</param>
    /// <param name="versionId">The ID that the request named.</param>
    /// <param name="cancellationToken">Stops the query.</param>
    /// <returns><see cref="ConflictException"/> for a deleted version, else <see cref="NotFoundException"/>.</returns>
    public static async Task<Exception> MissingAsync(
        ICoreDbContext dbContext,
        Guid versionId,
        CancellationToken cancellationToken
    )
    {
        var wasDeleted = await dbContext.ShiftTemplateVersions
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .AnyAsync(version => version.Id == versionId && version.DeletedAt != null, cancellationToken);

        return wasDeleted
            ? new ConflictException(ShiftErrors.Rejections.VersionChanged)
            : new NotFoundException(ShiftErrors.Rejections.VersionNotFound);
    }
}
