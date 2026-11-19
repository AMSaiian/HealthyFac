using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Messaging.Signals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Insights.Infrastructure.Cases.Persistence;

/// <summary>
/// Configures entity framework mapping for the machine signal entity. The signal keeps the ID of the Telemetry
/// episode as its key. A signal belongs to at most one case.
/// </summary>
public class MachineSignalConfiguration : IEntityTypeConfiguration<MachineSignal>
{
    public void Configure(EntityTypeBuilder<MachineSignal> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureVersioned();

        builder.Property(signal => signal.Rule).IsRequired();
        builder.Property(signal => signal.Severity).IsRequired();
        builder.Property(signal => signal.State).IsRequired();
        builder.Property(signal => signal.Coverage).IsRequired();

        builder.Property(signal => signal.MetricKey).HasMaxLength(SignalMessageLimits.KeyMaxLength);
        builder.Property(signal => signal.PhaseCode).HasMaxLength(SignalMessageLimits.KeyMaxLength);
        builder.Property(signal => signal.StatusCode).HasMaxLength(SignalMessageLimits.StatusCodeMaxLength);

        builder.HasOne<MaintenanceCase>()
            .WithMany(maintenanceCase => maintenanceCase.Signals)
            .HasForeignKey(signal => signal.CaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(signal => signal.CaseId)
            .HasDatabaseName(CaseIndexNames.SignalCase);

        builder.HasIndex(signal => new { signal.CompanyId, signal.MachineId, signal.State })
            .HasDatabaseName(CaseIndexNames.SignalMachineState);

        builder.HasEnumerationCheck(signal => signal.Rule)
            .HasEnumerationCheck(signal => signal.Severity)
            .HasEnumerationCheck(signal => signal.State)
            .HasEnumerationCheck(signal => signal.Coverage)
            .HasNotEmptyGuidCheck(signal => signal.CompanyId)
            .HasNotEmptyGuidCheck(signal => signal.MachineId)
            .HasRangeCheck(signal => signal.Sequence, min: 1, int.MaxValue);
    }
}
