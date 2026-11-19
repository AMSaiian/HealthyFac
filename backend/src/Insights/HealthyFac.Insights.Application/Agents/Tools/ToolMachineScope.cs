using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Agents.Tools;

/// <summary>
/// The scope check that every tool request runs: the actor works inside the company of the request, and the machine
/// of the request has a case in that company. A machine of another company is "not found", so a tool never reads the
/// data of a machine that the company does not own.
/// </summary>
public static class ToolMachineScope
{
    public const string MachineNotFound = "The machine was not found.";

    /// <summary>
    /// Checks the company of the actor and the machine.
    /// </summary>
    /// <param name="dbContext">Reads the cases.</param>
    /// <param name="currentUser">Reads the actor.</param>
    /// <param name="companyId">The company of the request.</param>
    /// <param name="machineId">The machine of the request.</param>
    /// <param name="cancellationToken">The token that cancels the query.</param>
    /// <returns>A task that completes when the check passes.</returns>
    /// <exception cref="NotFoundException">Thrown when the company has no case for the machine.</exception>
    public static async Task EnsureAsync(
        IInsightsDbContext dbContext,
        ICurrentUserProvider currentUser,
        Guid companyId,
        Guid machineId,
        CancellationToken cancellationToken
    )
    {
        currentUser.GetActor().EnsureCompany(companyId);

        var isMachineOfCompany = await dbContext.MaintenanceCases.AnyAsync(
            candidate => candidate.CompanyId == companyId && candidate.MachineId == machineId,
            cancellationToken);

        if (!isMachineOfCompany)
        {
            throw new NotFoundException(MachineNotFound);
        }
    }
}
