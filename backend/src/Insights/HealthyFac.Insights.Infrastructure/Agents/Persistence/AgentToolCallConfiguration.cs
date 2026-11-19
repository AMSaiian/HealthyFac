using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Insights.Infrastructure.Agents.Persistence;

/// <summary>
/// Configures entity framework mapping for the tool call entity of the AI log. The arguments and the result are
/// <c>jsonb</c> columns. A unique index keeps one row for each position of a run.
/// </summary>
public class AgentToolCallConfiguration : IEntityTypeConfiguration<AgentToolCall>
{
    private const int CallIdMaxLength = 100;
    private const int ToolNameMaxLength = 100;
    private const string JsonColumnType = "jsonb";

    public void Configure(EntityTypeBuilder<AgentToolCall> builder)
    {
        builder.ConfigureEntity();

        builder.Property(call => call.CallId).HasMaxLength(CallIdMaxLength).IsRequired();
        builder.Property(call => call.ToolName).HasMaxLength(ToolNameMaxLength).IsRequired();
        builder.Property(call => call.Arguments).HasColumnType(JsonColumnType).IsRequired();
        builder.Property(call => call.Result).HasColumnType(JsonColumnType).IsRequired();
        builder.Property(call => call.Outcome).IsRequired();

        builder.HasOne<AgentRun>()
            .WithMany()
            .HasForeignKey(call => call.RunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(call => new { call.RunId, call.Sequence })
            .HasDatabaseName(AgentIndexNames.ToolCallSequence)
            .IsUnique();

        builder.HasEnumerationCheck(call => call.Outcome)
            .HasNotBlankCheck(call => call.CallId)
            .HasNotBlankCheck(call => call.ToolName)
            .HasRangeCheck(call => call.Sequence, min: 1, int.MaxValue)
            .HasRangeCheck(call => call.DurationMilliseconds, min: 0, int.MaxValue);
    }
}
