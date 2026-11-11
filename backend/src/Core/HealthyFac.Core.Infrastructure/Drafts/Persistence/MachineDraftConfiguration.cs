using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.Drafts.Persistence;

/// <summary>
/// Configures entity framework mapping for the machine draft entity. The row version (<c>xmin</c>) makes two parallel
/// applies conflict.
/// </summary>
public class MachineDraftConfiguration : IEntityTypeConfiguration<MachineDraft>
{
    public void Configure(EntityTypeBuilder<MachineDraft> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(draft => draft.Name)
            .HasMaxLength(MachineValidationConstants.NameMaxLength);

        builder.Property(draft => draft.SerialNumber)
            .HasMaxLength(MachineValidationConstants.SerialNumberMaxLength);

        builder.Property(draft => draft.State)
            .IsRequired();

        builder.ConfigureVersioned();

        builder.HasOne<Machine>()
            .WithMany()
            .HasForeignKey(draft => draft.TargetMachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Machine>()
            .WithMany()
            .HasForeignKey(draft => draft.AppliedMachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(draft => new { draft.CompanyId, draft.AttemptId })
            .HasDatabaseName(DraftIndexNames.MachineDraftAttempt);

        builder.HasIndex(draft => new { draft.TargetMachineId, draft.State })
            .HasDatabaseName(DraftIndexNames.MachineDraftTarget)
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasEnumerationCheck(draft => draft.State)
            .HasNotBlankCheck(draft => draft.Name)
            .HasLengthCheck(draft => draft.Name, minLength: 1, MachineValidationConstants.NameMaxLength)
            .HasNotBlankCheck(draft => draft.SerialNumber)
            .HasLengthCheck(
                draft => draft.SerialNumber, minLength: 1, MachineValidationConstants.SerialNumberMaxLength)
            .HasLengthCheck(draft => draft.Description, minLength: 0, RichTextConstants.HtmlMaxLength)
            .HasNotEmptyGuidCheck(draft => draft.CompanyId);
    }
}
