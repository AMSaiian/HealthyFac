namespace HealthyFac.Shared.UnitTests.HealthChecks.Data;

public static class HealthEndpointOptionsValidatorTestData
{
    public static TheoryData<int> PortsOutOfRange => new()
    {
        0,
        -1,
        65536,
    };

    public static TheoryData<int> ValidPorts => new()
    {
        1,
        8090,
        65535,
    };
}
