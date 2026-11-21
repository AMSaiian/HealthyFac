using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.Application.Realtime;

/// <summary>
/// Tells the hub sender whether a push goes to one user or to a group.
/// </summary>
public sealed class RealtimePushTarget : Enumeration<RealtimePushTarget>
{
    public static readonly RealtimePushTarget User = new(value: "user");
    public static readonly RealtimePushTarget Group = new(value: "group");

    private RealtimePushTarget(string value)
        : base(value)
    {
    }
}
