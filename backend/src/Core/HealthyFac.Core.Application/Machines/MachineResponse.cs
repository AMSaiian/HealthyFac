using System.Linq.Expressions;
using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// The machine as the API shows it: the result of a get and a row of the machine grid.
/// </summary>
public record MachineResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string SerialNumber { get; init; }

    public required Guid DepartmentId { get; init; }

    public required MachineState State { get; init; }

    public string? Description { get; init; }

    public required DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Maps a machine to its response inside a query, so the database reads only these columns. The get use case
    /// and the grid share it.
    /// </summary>
    public static readonly Expression<Func<Machine, MachineResponse>> Projection = machine => new MachineResponse
    {
        Id = machine.Id,
        Name = machine.Name,
        SerialNumber = machine.SerialNumber,
        DepartmentId = machine.DepartmentId,
        State = machine.State,
        Description = machine.Description,
        CreatedAt = machine.CreatedAt,
        UpdatedAt = machine.UpdatedAt,
    };
}
