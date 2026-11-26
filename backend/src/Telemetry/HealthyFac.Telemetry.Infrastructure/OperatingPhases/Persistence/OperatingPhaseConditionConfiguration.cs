using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.OperatingPhases.Persistence;

/// <summary>
/// Configures the table <c>operating_phase_conditions</c>, the conditions of a phase. A condition reads exactly one
/// source. The foreign keys to the definitions restrict a delete, because a use case answers a used definition with a
/// business error first.
/// </summary>
public class OperatingPhaseConditionConfiguration : IEntityTypeConfiguration<OperatingPhaseCondition>
{
    public const string TableName = "operating_phase_conditions";

    public const string PhaseIdName = "PhaseId";

    public void Configure(EntityTypeBuilder<OperatingPhaseCondition> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.ToTable(
            TableName,
            table =>
            {
                table.HasCheckConstraint(
                    OperatingPhaseCheckNames.ConditionExactlyOneSource,
                    OperatingPhaseCheckNames.ConditionExactlyOneSourceSql);

                table.HasCheckConstraint(
                    OperatingPhaseCheckNames.ConditionOperatorAllowed,
                    CheckConstraintSql.AllowedValues(
                        nameof(OperatingPhaseCondition.Operator).ToSnakeCase(),
                        PhaseConditionOperator.All.Select(member => member.Value)));
            });

        builder.Property(condition => condition.Operator)
            .IsRequired();

        builder.Property(condition => condition.States)
            .HasField(fieldName: "_states")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<MetricDefinition>()
            .WithMany()
            .HasForeignKey(condition => condition.MetricDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StatusDefinition>()
            .WithMany()
            .HasForeignKey(condition => condition.StatusDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
