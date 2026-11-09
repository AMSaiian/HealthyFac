using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Document.Domain.MachineRegistrations;

/// <summary>
/// The Document copy of a Core machine: the machine ID and the company. The values never change, so
/// one registration per machine is enough. A removal keeps the row (soft delete).
/// </summary>
public class MachineRegistration : IEntity, ISoftDeletable
{
    /// <summary>
    /// Creates the registration of a machine.
    /// </summary>
    /// <param name="id">The ID of the machine in Core. It is the ID of the registration.</param>
    /// <param name="companyId">The company of the machine.</param>
    public MachineRegistration(Guid id, Guid companyId)
    {
        Id = id;
        CompanyId = companyId;
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
}
