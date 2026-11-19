using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Domain.Evaluation.Rules;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.Evaluation.Persistence;

/// <summary>
/// Configures entity framework mapping for the rule streak entity. A null subject means a rule of the machine, and
/// the unique index treats two nulls as equal, so a machine has one streak for each rule without a subject. The
/// subject is a metric or a status definition, so it has no foreign key.
/// </summary>
public class RuleStreakConfiguration : IEntityTypeConfiguration<RuleStreak>
{
    public void Configure(EntityTypeBuilder<RuleStreak> builder)
    {
        builder.ConfigureEntity();

        builder.HasOne<MachineRegistration>()
            .WithMany()
            .HasForeignKey(streak => streak.MachineRegistrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(streak => new { streak.MachineRegistrationId, streak.Rule, streak.SubjectId })
            .HasDatabaseName(EvaluationIndexNames.MachineRuleSubject)
            .IsUnique()
            .AreNullsDistinct(nullsDistinct: false);

        builder.HasEnumerationCheck(streak => streak.Rule);
    }
}
