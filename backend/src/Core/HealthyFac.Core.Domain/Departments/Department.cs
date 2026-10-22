using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Departments;

/// <summary>
/// Represents a department of a company. Machines and employees belong to departments, and the department
/// time zone is the reference for every planned local time in it.
/// </summary>
public class Department : IEntity, IAuditable
{
    public Guid Id { get; private set; } = Guid.Empty;
    public required Guid CompanyId { get; init; }
    public required string Name { get; init; }

    /// <summary>The IANA time zone ID, for example <c>Europe/Kyiv</c>.</summary>
    public required string TimeZoneId { get; init; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
}
