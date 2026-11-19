using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.OperatingPhases.Persistence;

/// <summary>
/// Configures entity framework mapping for the operating phase entity. Two unique indexes guard the code and the
/// priority of the active phases of a machine: a deleted phase frees both. The conditions and the limit rows are
/// child entities with a cascade: a loaded child is soft deleted with its phase, and a hard delete removes them.
/// </summary>
public class OperatingPhaseConfiguration : IEntityTypeConfiguration<OperatingPhase>
{
    public void Configure(EntityTypeBuilder<OperatingPhase> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.ConfigureVersioned();

        builder.Property(phase => phase.Code)
            .HasMaxLength(OperatingPhaseValidationConstants.CodeMaxLength)
            .IsRequired();

        builder.Property(phase => phase.Name)
            .HasMaxLength(OperatingPhaseValidationConstants.NameMaxLength)
            .IsRequired();

        builder.HasIndex(phase => new { phase.MachineRegistrationId, phase.Code })
            .HasDatabaseName(OperatingPhaseIndexNames.MachineCode)
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasIndex(phase => new { phase.MachineRegistrationId, phase.Priority })
            .HasDatabaseName(OperatingPhaseIndexNames.MachinePriority)
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasOne<MachineRegistration>()
            .WithMany()
            .HasForeignKey(phase => phase.MachineRegistrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(phase => phase.Conditions)
            .WithOne()
            .HasForeignKey(OperatingPhaseConditionConfiguration.PhaseIdName)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(phase => phase.Limits)
            .WithOne()
            .HasForeignKey(OperatingPhaseConditionConfiguration.PhaseIdName)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasNotBlankCheck(phase => phase.Code)
            .HasNotBlankCheck(phase => phase.Name)
            .HasLengthCheck(phase => phase.Description, minLength: 0, RichTextConstants.HtmlMaxLength)
            .HasNotEmptyGuidCheck(phase => phase.MachineRegistrationId);
    }
}
