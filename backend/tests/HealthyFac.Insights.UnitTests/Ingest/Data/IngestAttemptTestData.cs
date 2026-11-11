using HealthyFac.Insights.Domain.Ingest;

namespace HealthyFac.Insights.UnitTests.Ingest.Data;

public static class IngestAttemptTestData
{
    public static readonly DateTime QueuedAt =
        new(year: 2026, month: 11, day: 9, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateTime Now = QueuedAt.AddMinutes(value: 5);
    public static readonly DateTime LeaseUntil = Now.AddMinutes(value: 17);

    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a01");

    public static IngestAttempt Create(IngestState state)
    {
        var attempt = new IngestAttempt(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            machineId: null,
            QueuedAt,
            fileName: "manual",
            extension: "pdf",
            sizeBytes: 10,
            QueuedAt,
            traceParent: null);

        if (state == IngestState.Queued)
        {
            return attempt;
        }

        attempt.Claim(Now, LeaseUntil);

        if (state == IngestState.Completed)
        {
            attempt.Complete(Now, pageCount: 2, chunkCount: 3, draftCount: 1, warning: null);
        }
        else if (state == IngestState.Failed)
        {
            attempt.Fail(Now, IngestErrorCode.NoText);
        }
        else if (state == IngestState.Interrupted)
        {
            attempt.Interrupt(Now, IngestInterruptReason.Cancelled);
        }

        return attempt;
    }

    public static TheoryData<IngestState> InterruptibleStates => new()
    {
        IngestState.Queued,
        IngestState.InProcess,
    };

    public static TheoryData<IngestState> StatesThatAFileDeleteStops => new()
    {
        IngestState.Queued,
        IngestState.InProcess,
        IngestState.Completed,
    };

    public static TheoryData<IngestState> StatesThatAFileDeleteLeaves => new()
    {
        IngestState.Failed,
        IngestState.Interrupted,
    };

    public static TheoryData<IngestState> FinalStates => new()
    {
        IngestState.Completed,
        IngestState.Failed,
        IngestState.Interrupted,
    };

    public static TheoryData<IngestState> StatesThatAreNotInProcess => new()
    {
        IngestState.Queued,
        IngestState.Completed,
        IngestState.Failed,
        IngestState.Interrupted,
    };

    public static TheoryData<IngestState> StatesThatAreNotQueued => new()
    {
        IngestState.InProcess,
        IngestState.Completed,
        IngestState.Failed,
        IngestState.Interrupted,
    };
}
