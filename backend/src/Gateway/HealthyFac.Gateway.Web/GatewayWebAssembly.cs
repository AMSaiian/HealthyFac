using System.Reflection;

namespace HealthyFac.Gateway.Web;

/// <summary>
/// Labels the assembly of the Gateway for a scan, so a move or a rename of another type never changes the scan.
/// </summary>
public static class GatewayWebAssembly
{
    public static readonly Assembly Instance = typeof(GatewayWebAssembly).Assembly;
}
