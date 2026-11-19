namespace HealthyFac.Telemetry.Infrastructure.SignalEpisodes.Persistence;

/// <summary>
/// Defines the database index names of the signal episode table that code reads, so a name never differs between the
/// mapping and a check.
/// </summary>
public static class SignalEpisodeIndexNames
{
    public const string OpenSubject = "ux_signal_episodes_open_machine_registration_id_rule_subject";
    public const string MachineOpenedAt = "ix_signal_episodes_machine_registration_id_opened_at";
}
