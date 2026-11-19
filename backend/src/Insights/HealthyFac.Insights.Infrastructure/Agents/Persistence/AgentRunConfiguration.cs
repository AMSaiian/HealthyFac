using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Insights.Infrastructure.Agents.Persistence;

/// <summary>
/// Configures entity framework mapping for the agent run entity, the row of the AI log. The index on the state and the
/// due time serves the worker claim.
/// </summary>
public class AgentRunConfiguration : IEntityTypeConfiguration<AgentRun>
{
    private const int ModelMaxLength = 100;
    private const int PromptVersionMaxLength = 50;
    private const int PromptHashMaxLength = 64;

    public void Configure(EntityTypeBuilder<AgentRun> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureVersioned();

        builder.Property(run => run.Role).IsRequired();
        builder.Property(run => run.State).IsRequired();
        builder.Property(run => run.Model).HasMaxLength(ModelMaxLength);
        builder.Property(run => run.PromptVersion).HasMaxLength(PromptVersionMaxLength);
        builder.Property(run => run.PromptHash).HasMaxLength(PromptHashMaxLength);
        builder.Property(run => run.VerdictReason).HasMaxLength(AgentRun.VerdictReasonMaxLength);

        builder.HasOne<MaintenanceCase>()
            .WithMany()
            .HasForeignKey(run => run.CaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<IngestAttempt>()
            .WithMany()
            .HasForeignKey(run => run.IngestAttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(run => new { run.State, run.DueAt })
            .HasDatabaseName(AgentIndexNames.RunStateDueAt);

        builder.HasIndex(run => run.CaseId)
            .HasDatabaseName(AgentIndexNames.RunCase);

        builder.HasIndex(run => run.IngestAttemptId)
            .HasDatabaseName(AgentIndexNames.RunIngestAttempt);

        builder.HasEnumerationCheck(run => run.Role)
            .HasEnumerationCheck(run => run.Verdict)
            .HasEnumerationCheck(run => run.ErrorCode)
            .HasEnumerationCheck(run => run.SkipReason)
            .HasEnumerationCheck(run => run.State)
            .HasRangeCheck(run => run.AttemptNumber, min: 0, int.MaxValue)
            .HasRangeCheck(run => run.InputTokens, min: 0, int.MaxValue)
            .HasRangeCheck(run => run.OutputTokens, min: 0, int.MaxValue);
    }
}
