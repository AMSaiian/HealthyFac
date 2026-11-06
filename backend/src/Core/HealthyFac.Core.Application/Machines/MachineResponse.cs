using System.Linq.Expressions;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.QueryExpressions;

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
    public static readonly Expression<Func<Machine, MachineResponse>> Projection = BuildProjection();

    /// <summary>
    /// Builds <see cref="Projection"/> from a template lambda. It puts <see cref="MachineCurrentState.Selector"/> in
    /// place of the state parameter of the template, so the projection and the grid filter share one definition of
    /// the current state, and EF Core translates the result without LINQKit.
    /// </summary>
    /// <returns>The projection with the current state selector embedded.</returns>
    private static Expression<Func<Machine, MachineResponse>> BuildProjection()
    {
        Expression<Func<Machine, MachineState, MachineResponse>> template = (machine, state) => new MachineResponse
        {
            Id = machine.Id,
            Name = machine.Name,
            SerialNumber = machine.SerialNumber,
            DepartmentId = machine.DepartmentId,
            State = state,
            Description = machine.Description,
            CreatedAt = machine.CreatedAt,
            UpdatedAt = machine.UpdatedAt,
        };

        var machineParameter = template.Parameters[index: 0];
        var selectorBody = new ParameterReplacer(MachineCurrentState.Selector.Parameters[index: 0], machineParameter)
            .Visit(MachineCurrentState.Selector.Body);
        var body = new ParameterReplacer(template.Parameters[index: 1], selectorBody).Visit(template.Body);

        return Expression.Lambda<Func<Machine, MachineResponse>>(body, machineParameter);
    }
}
