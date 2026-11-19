using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.SignalEpisodes.Persistence;

/// <summary>
/// Configures entity framework mapping for the signal episode entity. A partial unique index allows one open episode
/// for each machine, rule, and subject. A null subject counts as equal (<c>AreNullsDistinct(false)</c>), so the rules
/// of a machine without a subject also keep one open episode. A resolved episode never blocks a new one. The snapshot
/// is one <c>jsonb</c> column.
/// </summary>
public class SignalEpisodeConfiguration : IEntityTypeConfiguration<SignalEpisode>
{
    private const string OpenFilter = "state = 'open'";

    public void Configure(EntityTypeBuilder<SignalEpisode> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureVersioned();

        builder.HasOne<MachineRegistration>()
            .WithMany()
            .HasForeignKey(episode => episode.MachineRegistrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(episode => new
            {
                episode.MachineRegistrationId,
                episode.Rule,
                episode.MetricDefinitionId,
                episode.StatusDefinitionId,
            })
            .HasDatabaseName(SignalEpisodeIndexNames.OpenSubject)
            .IsUnique()
            .AreNullsDistinct(nullsDistinct: false)
            .HasFilter(OpenFilter);

        builder.HasIndex(episode => new { episode.MachineRegistrationId, episode.OpenedAt })
            .HasDatabaseName(SignalEpisodeIndexNames.MachineOpenedAt);

        builder.ComplexProperty(episode => episode.Snapshot, snapshot => snapshot.ToJson(jsonColumnName: "snapshot"));

        builder.HasEnumerationCheck(episode => episode.Rule)
            .HasEnumerationCheck(episode => episode.Severity)
            .HasEnumerationCheck(episode => episode.State)
            .HasNotEmptyGuidCheck(episode => episode.CompanyId);
    }
}
