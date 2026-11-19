using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.DefinitionDrafts.Persistence;

/// <summary>
/// Configures the table <c>operating_phase_draft_limits</c>. A limit row names exactly one metric: a definition or a
/// draft of a definition.
/// </summary>
public class OperatingPhaseDraftLimitConfiguration : IEntityTypeConfiguration<OperatingPhaseDraftLimit>
{
    public const string TableName = "operating_phase_draft_limits";

    public void Configure(EntityTypeBuilder<OperatingPhaseDraftLimit> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.ToTable(
            TableName,
            table => table.HasCheckConstraint(
                DefinitionDraftCheckNames.PhaseLimitMetric,
                DefinitionDraftCheckNames.PhaseLimitMetricSql));
    }
}
