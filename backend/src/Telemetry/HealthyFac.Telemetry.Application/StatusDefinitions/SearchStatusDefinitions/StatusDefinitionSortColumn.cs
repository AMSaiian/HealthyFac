using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Application.StatusDefinitions.SearchStatusDefinitions;

/// <summary>
/// The sortable columns of the status definition grid.
/// </summary>
public sealed class StatusDefinitionSortColumn : Enumeration<StatusDefinitionSortColumn>
{
    public static readonly StatusDefinitionSortColumn Code = new(value: "code");
    public static readonly StatusDefinitionSortColumn Severity = new(value: "severity");
    public static readonly StatusDefinitionSortColumn CreatedAt = new(value: "createdAt");
    public static readonly StatusDefinitionSortColumn UpdatedAt = new(value: "updatedAt");

    private StatusDefinitionSortColumn(string value)
        : base(value)
    {
    }
}
