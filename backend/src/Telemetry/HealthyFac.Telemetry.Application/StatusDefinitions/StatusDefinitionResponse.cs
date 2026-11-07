using System.Linq.Expressions;
using HealthyFac.Telemetry.Domain.StatusDefinitions;

namespace HealthyFac.Telemetry.Application.StatusDefinitions;

/// <summary>
/// The status definition as the API shows it: the result of a get and a row of the status definition grid.
/// </summary>
public record StatusDefinitionResponse
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required StatusSeverity Severity { get; init; }

    public string? Description { get; init; }

    public required DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Maps a status definition to its response inside a query, so the database reads only these columns. The get use
    /// case and the grid share it.
    /// </summary>
    public static readonly Expression<Func<StatusDefinition, StatusDefinitionResponse>> Projection =
        status => new StatusDefinitionResponse
        {
            Id = status.Id,
            Code = status.Code,
            Severity = status.Severity,
            Description = status.Description,
            CreatedAt = status.CreatedAt,
            UpdatedAt = status.UpdatedAt,
        };
}
