using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Insights.Infrastructure.Cases.Persistence;

/// <summary>
/// Configures entity framework mapping for the maintenance case entity. A partial unique index allows one open case
/// for each machine, so two signals of one machine that arrive together cannot open two cases.
/// </summary>
public class MaintenanceCaseConfiguration : IEntityTypeConfiguration<MaintenanceCase>
{
    public void Configure(EntityTypeBuilder<MaintenanceCase> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureVersioned();

        builder.Property(maintenanceCase => maintenanceCase.State)
            .IsRequired();

        builder.Property(maintenanceCase => maintenanceCase.CloseReason)
            .HasMaxLength(CaseLimits.CloseReasonMaxLength);

        builder.HasIndex(maintenanceCase => maintenanceCase.MachineId, CaseIndexNames.OpenMachine)
            .IsUnique()
            .HasFilter($"state = '{CaseState.Open.Value}'");

        builder.HasEnumerationCheck(maintenanceCase => maintenanceCase.State)
            .HasNotEmptyGuidCheck(maintenanceCase => maintenanceCase.CompanyId)
            .HasNotEmptyGuidCheck(maintenanceCase => maintenanceCase.MachineId);
    }
}
