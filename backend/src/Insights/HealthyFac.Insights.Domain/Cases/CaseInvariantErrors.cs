using System.Runtime.CompilerServices;

namespace HealthyFac.Insights.Domain.Cases;

/// <summary>
/// Defines the exceptions that a case throws when a caller breaks the lifecycle. The Application layer checks the
/// state first, so these exceptions mark a programming error.
/// </summary>
public static class CaseInvariantErrors
{
    public static InvalidOperationException IllegalTransition(
        Guid caseId,
        CaseState from,
        [CallerMemberName] string action = ""
    ) =>
        new($"Case '{caseId}' is {from.Value}, so it cannot {action}.");
}
