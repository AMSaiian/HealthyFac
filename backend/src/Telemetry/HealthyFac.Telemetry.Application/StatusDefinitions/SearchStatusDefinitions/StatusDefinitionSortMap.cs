using System.Collections.Frozen;
using System.Linq.Expressions;
using HealthyFac.Telemetry.Domain.StatusDefinitions;

namespace HealthyFac.Telemetry.Application.StatusDefinitions.SearchStatusDefinitions;

/// <summary>
/// Maps each sort column of the status definition grid to its key expression.
/// </summary>
public static class StatusDefinitionSortMap
{
    public static readonly FrozenDictionary<StatusDefinitionSortColumn, Expression<Func<StatusDefinition, dynamic>>>
        Instance =
            new Dictionary<StatusDefinitionSortColumn, Expression<Func<StatusDefinition, dynamic>>>
            {
                [StatusDefinitionSortColumn.Code] = status => status.Code,
                [StatusDefinitionSortColumn.Severity] = status => status.Severity == StatusSeverity.Error
                    ? 2
                    : status.Severity == StatusSeverity.Warning
                        ? 1
                        : 0,
                [StatusDefinitionSortColumn.CreatedAt] = status => status.CreatedAt,
                [StatusDefinitionSortColumn.UpdatedAt] = status => status.UpdatedAt!,
            }.ToFrozenDictionary();
}
