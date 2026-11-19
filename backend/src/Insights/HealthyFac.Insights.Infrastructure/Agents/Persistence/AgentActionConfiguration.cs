using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Insights.Infrastructure.Agents.Persistence;

/// <summary>
/// Configures entity framework mapping for the buffered action entity. A unique index on the run and the call ID makes
/// a repeated tool call add no second action. The payload is a <c>jsonb</c> column.
/// </summary>
public class AgentActionConfiguration : IEntityTypeConfiguration<AgentAction>
{
    private const int CallIdMaxLength = 100;
    private const string JsonColumnType = "jsonb";

    public void Configure(EntityTypeBuilder<AgentAction> builder)
    {
        builder.ConfigureEntity();

        builder.Property(action => action.CallId).HasMaxLength(CallIdMaxLength).IsRequired();
        builder.Property(action => action.Payload).HasColumnType(JsonColumnType).IsRequired();
        builder.Property(action => action.Kind).IsRequired();
        builder.Property(action => action.State).IsRequired();

        builder.HasOne<AgentRun>()
            .WithMany()
            .HasForeignKey(action => action.RunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(action => new { action.RunId, action.CallId })
            .HasDatabaseName(AgentIndexNames.ActionCall)
            .IsUnique();

        builder.HasEnumerationCheck(action => action.Kind)
            .HasEnumerationCheck(action => action.State)
            .HasEnumerationCheck(action => action.FollowUp)
            .HasNotBlankCheck(action => action.CallId);
    }
}
