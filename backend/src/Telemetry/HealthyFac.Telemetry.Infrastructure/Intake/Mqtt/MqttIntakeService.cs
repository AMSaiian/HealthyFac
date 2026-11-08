using System.Buffers;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.Intake;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// Consumes the machine messages from Mosquitto and runs the intake use case for each one. The service subscribes to
/// a shared subscription with QoS 1 and acknowledges a message only after its transaction commits.
/// </summary>
/// <remarks>
/// <para>
/// <b>Consumers</b>: the service runs <see cref="MqttIntakeOptions.ConsumerCount"/> consumers. Each has its own client
/// with the ID <c>{ClientId}-{index}</c> and its own session loop, and the shared subscription gives each message to
/// one of them. Each instance needs a stable, unique <see cref="MqttIntakeOptions.ClientId"/> (for example a
/// StatefulSet pod name or a Compose replica name), because the broker keeps the unacknowledged messages by client ID.
/// Each consumer holds up to 1 database connection during a save, so keep the consumer count below the size of the
/// connection pool. Messages are not processed in order: the data is keyed by <c>sampled_at</c>.
/// </para>
/// <para>
/// <b>Finish</b> (rule <b>Cancellation Tokens</b>): the commit of the use case is the durable step that a rollback
/// cannot undo, so the acknowledge runs with <see cref="CancellationToken.None"/> right after it.
/// </para>
/// <para>
/// <b>Resume</b>: when the use case fails with a transient failure (<see cref="IntakeFailures.IsTransient"/>), the
/// service does not acknowledge, drops the connection, and connects again after a delay. The broker then delivers the
/// message again, and the unique index of the received messages drops a repeat of a message that committed before the
/// failure. Any other failure is the same on every delivery, so the service stores the raw bytes as a
/// <c>processingFailed</c> message in a new scope, logs one error with the exception, and acknowledges (a failure of
/// that save is transient). A message that the validator rejects is permanent too: the service acknowledges it and
/// logs a warning without the payload. Only the use case and the save of the failed message are classified: the
/// acknowledge runs outside them, and a failure of the acknowledge (a dropped connection) ends the session, logs a
/// warning, and stores nothing, so the broker delivers the message again and the unique index drops a repeat. A
/// cancellation (host shutdown) is not a failure.
/// </para>
/// <para>
/// <b>Stop</b>: when the host stops, each consumer closes its in-flight tracker. A message that arrives after the stop
/// signal is neither handled nor acknowledged, so the kept session delivers it again after the restart. The consumer
/// waits up to <see cref="MqttIntakeOptions.DrainTimeoutSeconds"/> for the messages that it already started, and then
/// disconnects with the normal session expiry. A message that started before the stop uses a processing token that
/// ends only at that timeout, never the stop token. <b>Finish</b> (rule <b>Safe Points</b>): the commit and the
/// acknowledge of such a message run to the end, because the broker has assigned it and a cancel would only deliver it
/// again later. At the timeout the handlers stop and the broker keeps the unacknowledged messages (<b>Resume</b>).
/// The consumer never unsubscribes and never deletes its session on a stop, so the broker queues the messages that
/// arrive while the instance is down, for up to <see cref="MqttIntakeOptions.SessionExpirySeconds"/>.
/// </para>
/// <para>
/// <b>Orphan sessions</b>: at start, the service empties the sessions of its own client IDs above
/// <see cref="MqttIntakeOptions.ConsumerCount"/> (a lower consumer count leaves them). For each one that exists, it
/// handles the queued messages, unsubscribes, waits until nothing arrived for
/// <see cref="MqttIntakeOptions.DrainIdleSeconds"/>, disconnects, and deletes the session by a clean-start connection
/// with a session expiry of 0 (MQTTnet cannot send an expiry of 0 on a disconnect). It keeps the normal session
/// expiry while it works, so a failure or a stop leaves the session and its messages for the next start. The check
/// cannot see the sessions of an instance that no longer runs: they keep their share until the expiry.
/// </para>
/// </remarks>
/// <param name="options">The consumer settings.</param>
/// <param name="scopeFactory">Creates the scope of the <c>telemetry:intake</c> actor for each message.</param>
/// <param name="timeProvider">Provides the moment of receipt.</param>
/// <param name="state">Receives the connection state for the health check and the gauges.</param>
/// <param name="metrics">Records the results, the durations, and the lag of the messages.</param>
/// <param name="logger">Logs the connection and the failures, never a payload.</param>
public sealed class MqttIntakeService(
    IOptions<MqttIntakeOptions> options,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    MqttIntakeState state,
    IntakeMetrics metrics,
    ILogger<MqttIntakeService> logger
) : BackgroundService
{
    private readonly MqttIntakeOptions _options = options.Value;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly MqttIntakeState _state = state;
    private readonly IntakeMetrics _metrics = metrics;
    private readonly ILogger<MqttIntakeService> _logger = logger;
    private readonly MqttClientFactory _clientFactory = new();
    private readonly TaskCompletionSource _orphanCheckCompleted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    
    /// <summary>
    /// Completes when the check of the orphan sessions at start has ended, with or without a failure. A test waits for
    /// it before it reads the sessions.
    /// </summary>
    internal Task OrphanCheckCompleted => _orphanCheckCompleted.Task;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsEnabled)
        {
            return;
        }

        var consumers = Enumerable.Range(start: 0, _options.ConsumerCount)
            .Select(index => RunConsumerAsync(index, cancellationToken));

        await Task.WhenAll(consumers.Append(EmptyOrphansAsync(cancellationToken)));
    }

    private async Task RunConsumerAsync(int index, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(index, cancellationToken);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception, message: "The MQTT session {ConsumerIndex} ended with an error.", index);
            }

            await DelayAsync(cancellationToken);
        }
    }

    private async Task RunSessionAsync(int index, CancellationToken cancellationToken)
    {
        await using var session = CreateSession(index);

        await session.ConnectAsync(BuildClientOptions(index), cancellationToken);
        await session.Client.SubscribeAsync(
            new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(SharedFilter, MqttQualityOfServiceLevel.AtLeastOnce)
                .Build(),
            cancellationToken);

        _state.MarkConnected();
        _logger.LogInformation(message: "The MQTT consumer {ConsumerIndex} is connected and subscribed.", index);

        try
        {
            await session.Ended.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await FinishAsync(index, session);
        }
        finally
        {
            _state.MarkDisconnected();
        }
    }

    private ConsumerSession CreateSession(int index) =>
        new(_clientFactory.CreateMqttClient(), _timeProvider, index, HandleAsync);

    private async Task HandleAsync(MqttApplicationMessageReceivedEventArgs arguments, ConsumerSession session)
    {
        arguments.AutoAcknowledge = false;

        if (!session.InFlight.TryBegin())
        {
            return;
        }

        try
        {
            _state.MarkMessageStarted();

            var startedAt = _timeProvider.GetTimestamp();
            var receivedAt = _timeProvider.GetUtcNow().UtcDateTime;
            using var activity = IntakeActivity.Start(
                arguments.ApplicationMessage, _options.SharedGroup, session.Index);

            var outcome = await TrySettleAsync(arguments, receivedAt, session);

            if (outcome.IsSettled)
            {
                await AcknowledgeAsync(arguments, session);
            }

            IntakeActivity.Complete(activity, outcome);
            RecordOutcome(outcome, receivedAt, _timeProvider.GetElapsedTime(startedAt));
        }
        catch (OperationCanceledException) when (session.ProcessingToken.IsCancellationRequested)
        {
        }
        finally
        {
            _state.MarkMessageEnded();
            session.InFlight.End();
        }
    }

    private void RecordOutcome(IntakeOutcome outcome, DateTime receivedAt, TimeSpan elapsed)
    {
        _metrics.RecordMessage(outcome, elapsed);

        if (outcome.IsStored && outcome.SampledAt is { } sampledAt)
        {
            _metrics.RecordLag(receivedAt - sampledAt);
        }
    }

    /// <summary>
    /// Waits for the messages that a stopping consumer already started. At the timeout the handlers stop, and the
    /// caller disconnects with the normal session expiry, so the broker keeps the unacknowledged messages.
    /// </summary>
    private async Task FinishAsync(int index, ConsumerSession session)
    {
        session.InFlight.Close();

        using var timeout = session.CreateTimeout(
            TimeSpan.FromSeconds(_options.DrainTimeoutSeconds),
            CancellationToken.None);

        try
        {
            await WaitUntilAsync(() => session.InFlight.IsEmpty, timeout.Token);
            _logger.LogInformation(message: "The MQTT consumer {ConsumerIndex} finished its messages.", index);
        }
        catch (OperationCanceledException) when (timeout.IsTimedOut)
        {
            _metrics.RecordDrainTimeout();
            _logger.LogWarning(
                message: "The MQTT consumer {ConsumerIndex} did not finish its messages in time. The session stays.",
                index);
        }
    }

    private async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        while (!condition())
        {
            await Task.Delay(
                TimeSpan.FromMilliseconds(MqttIntakeConstants.DrainPollMilliseconds),
                _timeProvider,
                cancellationToken);
        }
    }

    private async Task EmptyOrphansAsync(CancellationToken cancellationToken)
    {
        try
        {
            for (var index = _options.ConsumerCount;
                 index < MqttIntakeConstants.Limits.MaxConsumerCount && !cancellationToken.IsCancellationRequested;
                 index++)
            {
                await TryEmptyOrphanAsync(index, cancellationToken);
            }
        }
        finally
        {
            _orphanCheckCompleted.TrySetResult();
        }
    }

    private async Task TryEmptyOrphanAsync(int index, CancellationToken cancellationToken)
    {
        try
        {
            await EmptyOrphanAsync(index, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                message: "The MQTT intake could not empty the orphan session {ConsumerIndex}. It stays.",
                index);
        }
    }

    /// <summary>
    /// Empties the session of one client ID above the consumer count. The connection keeps the normal session expiry,
    /// so a failure, a timeout, or a stop leaves the session and its messages for the next start.
    /// </summary>
    private async Task EmptyOrphanAsync(int index, CancellationToken cancellationToken)
    {
        await using var session = CreateSession(index);

        var connectResult = await session.ConnectAsync(BuildClientOptions(index), cancellationToken);

        if (connectResult.IsSessionPresent)
        {
            await EmptySessionAsync(session, cancellationToken);
            EnsureNotInterrupted(session, index);
        }

        await session.Client.DisconnectAsync(cancellationToken: cancellationToken);
        await DeleteSessionAsync(index, cancellationToken);

        if (connectResult.IsSessionPresent)
        {
            _metrics.RecordOrphanEmptied(session.MessageCount);
            _logger.LogInformation(
                message: "The MQTT intake emptied the orphan session {ConsumerIndex} ({MessageCount} messages).",
                index,
                session.MessageCount);
        }
    }

    private async Task EmptySessionAsync(ConsumerSession session, CancellationToken cancellationToken)
    {
        using var timeout = session.CreateTimeout(
            TimeSpan.FromSeconds(_options.DrainTimeoutSeconds),
            cancellationToken);

        await session.Client.UnsubscribeAsync(
            new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(SharedFilter).Build(),
            timeout.Token);
        session.InFlight.Touch();
        await WaitUntilAsync(
            () => session.InFlight.IsIdleFor(TimeSpan.FromSeconds(_options.DrainIdleSeconds)),
            timeout.Token);
    }

    private async Task DeleteSessionAsync(int index, CancellationToken cancellationToken)
    {
        await using var cleaning = CreateSession(index);

        await cleaning.ConnectAsync(
            BuildClientOptions(index, isCleanStart: true, sessionExpirySeconds: 0),
            cancellationToken);
        await cleaning.Client.DisconnectAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Throws when a handler ended the session (a message stayed unacknowledged) or the connection dropped, so the
    /// caller never deletes a session that still holds a message.
    /// </summary>
    private static void EnsureNotInterrupted(ConsumerSession session, int index)
    {
        if (session.Ended.IsCompleted)
        {
            throw new InvalidOperationException(
                string.Format(MqttIntakeConstants.Messages.OrphanSessionInterrupted, index));
        }
    }

    /// <summary>
    /// Runs the use case and classifies only its failures. The acknowledge is not part of it, so a broken connection
    /// at the acknowledge is never stored as a processing failure.
    /// </summary>
    /// <returns>The result of the message. The caller may acknowledge it unless the result is
    /// <c>redelivery</c>: then the session ended and the broker must deliver it again.</returns>
    private async Task<IntakeOutcome> TrySettleAsync(
        MqttApplicationMessageReceivedEventArgs arguments,
        DateTime receivedAt,
        ConsumerSession session
    )
    {
        try
        {
            var response = await ReceiveAsync(arguments, receivedAt, session.ProcessingToken);

            return IntakeOutcome.From(response);
        }
        catch (AppValidationException exception)
        {
            _logger.LogWarning(
                message: "The MQTT intake dropped a message that failed validation ({ErrorCount} errors).",
                exception.Errors.Count());

            return new IntakeOutcome { Result = ObservabilityConstants.Intake.Results.Invalid };
        }
        catch (Exception exception) when (!session.ProcessingToken.IsCancellationRequested)
        {
            return await TryStoreFailureAsync(arguments, exception, session);
        }
    }

    private async Task<IntakeOutcome> TryStoreFailureAsync(
        MqttApplicationMessageReceivedEventArgs arguments,
        Exception exception,
        ConsumerSession session
    )
    {
        if (IntakeFailures.IsTransient(exception))
        {
            _logger.LogError(
                exception,
                message: "The MQTT intake hit a transient failure. The message stays unacknowledged for redelivery.");
            session.End();

            return new IntakeOutcome { Result = ObservabilityConstants.Intake.Results.Redelivery };
        }

        try
        {
            await StoreFailureAsync(arguments, session.ProcessingToken);
        }
        catch (Exception storeException) when (!session.ProcessingToken.IsCancellationRequested)
        {
            _logger.LogError(
                storeException,
                message: "The MQTT intake could not store a failed message. It stays unacknowledged for redelivery.");
            session.End();

            return new IntakeOutcome { Result = ObservabilityConstants.Intake.Results.Redelivery };
        }

        _logger.LogError(
            exception,
            message: "The MQTT intake could not process a message. It stored it as processingFailed.");

        return new IntakeOutcome { Result = ObservabilityConstants.Intake.Results.Failed };
    }

    private async Task AcknowledgeAsync(MqttApplicationMessageReceivedEventArgs arguments, ConsumerSession session)
    {
        try
        {
            await arguments.AcknowledgeAsync(CancellationToken.None);
        }
        catch (Exception exception) when (!session.ProcessingToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "The MQTT intake could not acknowledge a message. "
                + "The session ends and the broker delivers it again.");
            session.End();
        }
    }

    private async Task StoreFailureAsync(
        MqttApplicationMessageReceivedEventArgs arguments,
        CancellationToken cancellationToken
    )
    {
        using var scope = _scopeFactory.CreateActorScope(TelemetryServiceActors.Intake);
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<StoreFailedMachineMessageRequest>>();

        await handler.HandleAsync(
            new StoreFailedMachineMessageRequest
            {
                Topic = arguments.ApplicationMessage.Topic,
                Payload = arguments.ApplicationMessage.Payload.ToArray(),
                ReceivedAt = _timeProvider.GetUtcNow().UtcDateTime,
            },
            cancellationToken);
    }

    private async Task<ReceiveMachineMessageResponse> ReceiveAsync(
        MqttApplicationMessageReceivedEventArgs arguments,
        DateTime receivedAt,
        CancellationToken cancellationToken
    )
    {
        using var scope = _scopeFactory.CreateActorScope(TelemetryServiceActors.Intake);
        var handler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<ReceiveMachineMessageRequest, ReceiveMachineMessageResponse>>();

        return await handler.HandleAsync(
            new ReceiveMachineMessageRequest
            {
                Topic = arguments.ApplicationMessage.Topic,
                Payload = arguments.ApplicationMessage.Payload.ToArray(),
                ReceivedAt = receivedAt,
            },
            cancellationToken);
    }

    private string SharedFilter => string.Format(MqttIntakeConstants.SharedSubscriptionFormat, _options.SharedGroup);

    private MqttClientOptions BuildClientOptions(int index) =>
        BuildClientOptions(index, isCleanStart: false, _options.SessionExpirySeconds);

    private MqttClientOptions BuildClientOptions(int index, bool isCleanStart, int sessionExpirySeconds) =>
        new MqttClientOptionsBuilder()
            .WithTcpServer(_options.Host, _options.Port)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .WithClientId(string.Format(MqttIntakeConstants.ConsumerClientIdFormat, _options.ClientId, index))
            .WithCredentials(_options.Username, _options.Password)
            .WithCleanStart(isCleanStart)
            .WithSessionExpiryInterval((uint)sessionExpirySeconds)
            .Build();

    private async Task DelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), _timeProvider, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
