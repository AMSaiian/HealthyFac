using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.StatusDefinitions.Persistence;

/// <summary>
/// Configures entity framework mapping for the status definition entity. The unique index on the machine and the
/// code is the only guard for the uniqueness of a code, because the use cases do not query for it.
/// </summary>
public class StatusDefinitionConfiguration : IEntityTypeConfiguration<StatusDefinition>
{
    public void Configure(EntityTypeBuilder<StatusDefinition> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(status => status.Code)
            .HasMaxLength(StatusDefinitionValidationConstants.CodeMaxLength)
            .IsRequired();

        builder.Property(status => status.Severity)
            .IsRequired();

        builder.HasIndex(status => new { status.MachineRegistrationId, status.Code })
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasOne<MachineRegistration>()
            .WithMany()
            .HasForeignKey(status => status.MachineRegistrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasEnumerationCheck(status => status.Severity)
            .HasNotBlankCheck(status => status.Code)
            .HasLengthCheck(
                status => status.Description,
                minLength: 0,
                RichTextConstants.HtmlMaxLength
            )
            .HasNotEmptyGuidCheck(status => status.MachineRegistrationId);
    }
}
