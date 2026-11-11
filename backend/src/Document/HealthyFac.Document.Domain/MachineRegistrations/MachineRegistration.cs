using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Document.Domain.MachineRegistrations;

/// <summary>
/// The Document copy of a Core machine: the machine ID, the company, and the name. The name shows in the company file
/// grid. A removal keeps the row (soft delete).
/// </summary>
public class MachineRegistration : IEntity, ISoftDeletable
{
    /// <summary>
    /// Creates the registration of a machine.
    /// </summary>
    /// <param name="id">The ID of the machine in Core. It is the ID of the registration.</param>
    /// <param name="companyId">The company of the machine.</param>
    /// <param name="name">The name of the machine.</param>
    public MachineRegistration(Guid id, Guid companyId, string name)
    {
        Id = id;
        CompanyId = companyId;
        Name = name;
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; }

    /// <summary>The time (UTC) of the last rename, or <see langword="null"/> when the name never changed.</summary>
    public DateTime? NameChangedAt { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Changes the name. A rename that is not newer than the last one changes nothing, because messages can arrive in
    /// another order than Core sent them.
    /// </summary>
    /// <param name="name">The new name.</param>
    /// <param name="renamedAt">The time of the rename in Core (UTC).</param>
    public void Rename(string name, DateTime renamedAt)
    {
        if (NameChangedAt is not null && renamedAt <= NameChangedAt)
        {
            return;
        }

        Name = name;
        NameChangedAt = renamedAt;
    }
}
