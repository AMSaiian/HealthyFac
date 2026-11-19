using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.UnitTests.Cases.Data;

namespace HealthyFac.Insights.UnitTests.Cases;

public sealed class MaintenanceCaseTests
{
    [Theory]
    [MemberData(nameof(CaseTestData.CoverageAfterClose), MemberType = typeof(CaseTestData))]
    public void Close_WithASignal_DismissesOnlyTheUncoveredCoverage(
        string caseName,
        SignalCoverage before,
        SignalCoverage after
    )
    {
        var maintenanceCase = CaseTestData.CreateCase();
        var signal = CaseTestData.CreateSignal(coverage: before);
        maintenanceCase.Add(signal);

        maintenanceCase.Close(reason: "done", CaseTestData.Now);

        signal.Coverage.Should().Be(after, caseName);
        maintenanceCase.State.Should().Be(CaseState.Closed);
        maintenanceCase.CloseReason.Should().Be(expected: "done");
        maintenanceCase.ClosedAt.Should().Be(CaseTestData.Now);
    }

    [Fact]
    public void Close_OnAClosedCase_Throws()
    {
        var maintenanceCase = CaseTestData.CreateCase();
        maintenanceCase.Close(reason: "done", CaseTestData.Now);

        var act = () => maintenanceCase.Close(reason: "again", CaseTestData.Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ResolveIfDone_WithAnOpenSignal_KeepsTheCaseOpen()
    {
        var maintenanceCase = CaseTestData.CreateCase();
        maintenanceCase.Add(CaseTestData.CreateSignal(SignalState.Open));
        maintenanceCase.Add(CaseTestData.CreateSignal(SignalState.Resolved));

        var isResolved = maintenanceCase.ResolveIfDone(CaseTestData.Now);

        isResolved.Should().BeFalse();
        maintenanceCase.State.Should().Be(CaseState.Open);
    }

    [Fact]
    public void ResolveIfDone_WithNoOpenSignal_ResolvesTheCase()
    {
        var maintenanceCase = CaseTestData.CreateCase();
        maintenanceCase.Add(CaseTestData.CreateSignal(SignalState.Resolved));

        var isResolved = maintenanceCase.ResolveIfDone(CaseTestData.Now);

        isResolved.Should().BeTrue();
        maintenanceCase.State.Should().Be(CaseState.Resolved);
    }

    [Fact]
    public void ResolveIfDone_OnAClosedCase_KeepsItClosed()
    {
        var maintenanceCase = CaseTestData.CreateCase();
        maintenanceCase.Close(reason: "done", CaseTestData.Now);

        var isResolved = maintenanceCase.ResolveIfDone(CaseTestData.Now);

        isResolved.Should().BeFalse();
        maintenanceCase.State.Should().Be(CaseState.Closed);
    }
}
