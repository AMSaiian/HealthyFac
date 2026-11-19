using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Shared.Infrastructure.IdEncryption;
using HealthyFac.Telemetry.Messaging.Signals;

namespace HealthyFac.Insights.IntegrationTests.Cases.Data;

public static class CaseTestData
{
    public const string CallId = "call-1";
    public const string EmptyJson = "{}";
    public const string IdEncryptionKey = "dGVzdC1pZC1lbmNyeXB0aW9uLWtleS0zMmJ5dGVzISE=";

    private static readonly AesKwIdEncryptor Encryptor = new(Convert.FromBase64String(IdEncryptionKey));

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");
    public static readonly Guid OtherMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000102");

    public static readonly DateTime Now = new(
        year: 2026, month: 11, day: 9, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

    public const int DebounceSeconds = 30;
    public const int RaceParticipantCount = 2;
    public const int InboxWaitSteps = 200;
    public const int InboxWaitStepMilliseconds = 100;

    public static readonly Guid FirstEpisodeId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000501");
    public static readonly Guid SecondEpisodeId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000502");
    public static readonly Guid ThirdEpisodeId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000503");

    public static SignalEpisodeChanged Message(
        Guid episodeId,
        int sequence,
        SignalEpisodeStateValue? state = null,
        SignalSeverityValue? severity = null,
        Guid? machineId = null
    ) =>
        new()
        {
            EpisodeId = episodeId,
            MachineId = machineId ?? MachineId,
            CompanyId = CompanyId,
            Rule = SignalRuleValue.Threshold,
            Severity = severity ?? SignalSeverityValue.Warning,
            State = state ?? SignalEpisodeStateValue.Open,
            MetricKey = "spindle.temperature",
            Value = 91,
            Limit = 90,
            OpenedAt = Now,
            ResolvedAt = state == SignalEpisodeStateValue.Resolved ? Now.AddMinutes(value: 1) : null,
            Sequence = sequence,
            ChangedAt = Now.AddSeconds(sequence),
        };

    public static MaintenanceCase CreateOpenCase(Guid? machineId = null) =>
        new(CompanyId, machineId ?? MachineId, Now);

    public static MaintenanceCase CreateResolvedCase(Guid? machineId = null)
    {
        var maintenanceCase = CreateOpenCase(machineId);
        maintenanceCase.ResolveIfDone(Now);

        return maintenanceCase;
    }

    public static AgentRun CreateRun(Guid? caseId = null) =>
        new(AgentRole.Triage, caseId, ingestAttemptId: null, Now);

    public static AgentAction CreateAction(Guid runId, string callId = CallId) =>
        new(runId, callId, AgentActionKind.CloseCase, EmptyJson);

    public static AgentToolCall CreateToolCall(Guid runId, int sequence) =>
        new(
            runId,
            sequence,
            CallId,
            toolName: "get_open_work_orders",
            EmptyJson,
            ToolCallOutcome.Succeeded,
            EmptyJson,
            durationMilliseconds: 5);

    /// <summary>
    /// Encrypts an ID the way the model sees it.
    /// </summary>
    /// <param name="id">The raw ID.</param>
    /// <returns>The encrypted token.</returns>
    public static string Encrypt(Guid id) => Encryptor.Encrypt(id);
}
