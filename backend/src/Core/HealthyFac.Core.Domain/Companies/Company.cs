using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Companies;

/// <summary>
/// Represents a tenant. Every business row belongs to exactly one company.
/// </summary>
public class Company : IEntity, IAuditable, ISoftDeletable
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public required string Name { get; init; }

    /// <summary>
    /// The IANA time zone ID, for example <c>Europe/Kyiv</c>. Every planned local time of the company and every
    /// displayed time in the web app use it.
    /// </summary>
    public required string TimeZoneId { get; init; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
}
