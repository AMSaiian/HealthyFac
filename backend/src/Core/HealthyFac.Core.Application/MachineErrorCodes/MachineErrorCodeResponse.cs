using System.Linq.Expressions;
using HealthyFac.Core.Domain.MachineErrorCodes;

namespace HealthyFac.Core.Application.MachineErrorCodes;

/// <summary>
/// The error code as the API shows it: the result of a get and a row of the error code grid.
/// </summary>
public record MachineErrorCodeResponse
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public string? Description { get; init; }

    public required DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Maps an error code to its response inside a query, so the database reads only these columns. The get use
    /// case and the grid share it.
    /// </summary>
    public static readonly Expression<Func<MachineErrorCode, MachineErrorCodeResponse>> Projection =
        errorCode => new MachineErrorCodeResponse
        {
            Id = errorCode.Id,
            Code = errorCode.Code,
            Description = errorCode.Description,
            CreatedAt = errorCode.CreatedAt,
            UpdatedAt = errorCode.UpdatedAt,
        };
}
