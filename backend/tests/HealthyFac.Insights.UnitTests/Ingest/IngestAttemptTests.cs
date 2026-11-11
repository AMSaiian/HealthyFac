using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.UnitTests.Ingest.Data;

namespace HealthyFac.Insights.UnitTests.Ingest;

public sealed class IngestAttemptTests
{
    [Fact]
    public void Claim_OnQueuedAttempt_StartsTheRun()
    {
        var attempt = IngestAttemptTestData.Create(IngestState.Queued);

        attempt.Claim(IngestAttemptTestData.Now, IngestAttemptTestData.LeaseUntil);

        attempt.State.Should().Be(IngestState.InProcess);
        attempt.StartedAt.Should().Be(IngestAttemptTestData.Now);
        attempt.LeaseUntil.Should().Be(IngestAttemptTestData.LeaseUntil);
    }

    [Theory]
    [MemberData(
        nameof(IngestAttemptTestData.StatesThatAreNotQueued),
        MemberType = typeof(IngestAttemptTestData))]
    public void Claim_OnAttemptThatIsNotQueued_Throws(IngestState state)
    {
        var attempt = IngestAttemptTestData.Create(state);

        var act = () => attempt.Claim(IngestAttemptTestData.Now, IngestAttemptTestData.LeaseUntil);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_OnAttemptInProcess_StoresTheResultAndEndsTheLease()
    {
        var attempt = IngestAttemptTestData.Create(IngestState.InProcess);

        attempt.Complete(IngestAttemptTestData.Now, pageCount: 2, chunkCount: 3, draftCount: 1, warning: "w");

        attempt.State.Should().Be(IngestState.Completed);
        attempt.FinishedAt.Should().Be(IngestAttemptTestData.Now);
        attempt.LeaseUntil.Should().BeNull();
        (attempt.PageCount, attempt.ChunkCount, attempt.ProposedDraftCount).Should().Be((2, 3, 1));
        attempt.Warning.Should().Be(expected: "w");
    }

    [Theory]
    [MemberData(
        nameof(IngestAttemptTestData.StatesThatAreNotInProcess),
        MemberType = typeof(IngestAttemptTestData))]
    public void Complete_OnAttemptThatIsNotInProcess_Throws(IngestState state)
    {
        var attempt = IngestAttemptTestData.Create(state);

        var act = () => attempt.Complete(
            IngestAttemptTestData.Now, pageCount: 1, chunkCount: 1, draftCount: 0, warning: null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Fail_OnAttemptInProcess_StoresTheErrorCode()
    {
        var attempt = IngestAttemptTestData.Create(IngestState.InProcess);

        attempt.Fail(IngestAttemptTestData.Now, IngestErrorCode.ConversionTimeout);

        attempt.State.Should().Be(IngestState.Failed);
        attempt.ErrorCode.Should().Be(IngestErrorCode.ConversionTimeout);
        attempt.LeaseUntil.Should().BeNull();
    }

    [Theory]
    [MemberData(
        nameof(IngestAttemptTestData.StatesThatAreNotInProcess),
        MemberType = typeof(IngestAttemptTestData))]
    public void Fail_OnAttemptThatIsNotInProcess_Throws(IngestState state)
    {
        var attempt = IngestAttemptTestData.Create(state);

        var act = () => attempt.Fail(IngestAttemptTestData.Now, IngestErrorCode.NoText);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(IngestAttemptTestData.InterruptibleStates), MemberType = typeof(IngestAttemptTestData))]
    public void Interrupt_OnActiveAttempt_StoresTheReason(IngestState state)
    {
        var attempt = IngestAttemptTestData.Create(state);

        attempt.Interrupt(IngestAttemptTestData.Now, IngestInterruptReason.LeaseExpired);

        attempt.State.Should().Be(IngestState.Interrupted);
        attempt.InterruptReason.Should().Be(IngestInterruptReason.LeaseExpired);
        attempt.IsFinal.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(IngestAttemptTestData.FinalStates), MemberType = typeof(IngestAttemptTestData))]
    public void Interrupt_OnFinalAttempt_Throws(IngestState state)
    {
        var attempt = IngestAttemptTestData.Create(state);

        var act = () => attempt.Interrupt(IngestAttemptTestData.Now, IngestInterruptReason.Cancelled);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(IngestAttemptTestData.StatesThatAFileDeleteStops), MemberType = typeof(IngestAttemptTestData))]
    public void InterruptForFileDelete_OnQueuedRunningOrCompletedAttempt_StoresTheReasonAndKeepsTheCounts(
        IngestState state
    )
    {
        var attempt = IngestAttemptTestData.Create(state);
        var counts = (attempt.PageCount, attempt.ChunkCount, attempt.ProposedDraftCount);

        attempt.InterruptForFileDelete(IngestAttemptTestData.Now);

        attempt.State.Should().Be(IngestState.Interrupted);
        attempt.InterruptReason.Should().Be(IngestInterruptReason.FileDeleted);
        attempt.FinishedAt.Should().Be(IngestAttemptTestData.Now);
        attempt.LeaseUntil.Should().BeNull();
        (attempt.PageCount, attempt.ChunkCount, attempt.ProposedDraftCount).Should().Be(counts);
    }

    [Theory]
    [MemberData(nameof(IngestAttemptTestData.StatesThatAFileDeleteLeaves), MemberType = typeof(IngestAttemptTestData))]
    public void InterruptForFileDelete_OnFailedOrInterruptedAttempt_Throws(IngestState state)
    {
        var attempt = IngestAttemptTestData.Create(state);

        var act = () => attempt.InterruptForFileDelete(IngestAttemptTestData.Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ChangeMachine_WithNewerTime_StoresTheMachine()
    {
        var attempt = IngestAttemptTestData.Create(IngestState.Queued);

        attempt.ChangeMachine(IngestAttemptTestData.MachineId, IngestAttemptTestData.Now);

        attempt.MachineId.Should().Be(IngestAttemptTestData.MachineId);
        attempt.MachineChangedAt.Should().Be(IngestAttemptTestData.Now);
    }

    [Fact]
    public void ChangeMachine_WithOlderTime_ChangesNothing()
    {
        var attempt = IngestAttemptTestData.Create(IngestState.Queued);
        attempt.ChangeMachine(IngestAttemptTestData.MachineId, IngestAttemptTestData.Now);

        attempt.ChangeMachine(machineId: null, IngestAttemptTestData.QueuedAt);

        attempt.MachineId.Should().Be(IngestAttemptTestData.MachineId);
    }
}
