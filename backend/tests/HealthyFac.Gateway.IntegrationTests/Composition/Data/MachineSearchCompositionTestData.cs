namespace HealthyFac.Gateway.IntegrationTests.Composition.Data;

public static class MachineSearchCompositionTestData
{
    /// <summary>Both values of the filter <c>hasPendingDrafts</c>.</summary>
    public static TheoryData<bool> DraftFilterValues => [true, false];
}
