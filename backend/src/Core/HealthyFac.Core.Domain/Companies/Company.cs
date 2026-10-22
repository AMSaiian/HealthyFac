using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Companies;

/// <summary>
/// Represents a tenant. Every business row belongs to exactly one company.
/// </summary>
public class Company : IEntity, IAuditable
{
    public Guid Id { get; private set; } = Guid.Empty;
    public required string Name { get; init; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
}
