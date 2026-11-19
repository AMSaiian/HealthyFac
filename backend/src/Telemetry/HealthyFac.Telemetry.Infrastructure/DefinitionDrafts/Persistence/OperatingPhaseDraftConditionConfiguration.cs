using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.DefinitionDrafts.Persistence;

/// <summary>
/// Configures the table <c>operating_phase_draft_conditions</c>. A condition names exactly one source: a definition or
/// a draft of a definition. The sources have no foreign key, because a draft is a soft-deleted row of another table
/// and a definition can arrive later than the proposal.
/// </summary>
public class OperatingPhaseDraftConditionConfiguration : IEntityTypeConfiguration<OperatingPhaseDraftCondition>
{
    public const string TableName = "operating_phase_draft_conditions";

    public void Configure(EntityTypeBuilder<OperatingPhaseDraftCondition> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.ToTable(
            TableName,
            table => table.HasCheckConstraint(
                DefinitionDraftCheckNames.PhaseConditionSource,
                DefinitionDraftCheckNames.PhaseConditionSourceSql));

        builder.Property(condition => condition.States)
            .HasField(fieldName: "_states")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
