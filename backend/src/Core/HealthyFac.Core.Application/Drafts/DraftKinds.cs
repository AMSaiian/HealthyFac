namespace HealthyFac.Core.Application.Drafts;

/// <summary>
/// Defines the kinds of draft that Core shows. The camelCase values reach the API. Core holds only machine drafts: the
/// metric and status kinds live in Telemetry, and the Gateway splits a kind filter between the two services.
/// </summary>
public static class DraftKinds
{
    public const string Machine = "machine";

    public static readonly string[] All = [Machine];
}
