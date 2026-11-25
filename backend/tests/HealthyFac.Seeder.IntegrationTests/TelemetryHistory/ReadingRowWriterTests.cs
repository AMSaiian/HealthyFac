using HealthyFac.MachineSimulator.Scenarios.Payloads;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;
using HealthyFac.Seeder.TelemetryHistory;
using HealthyFac.Seeder.TelemetryHistory.Persistence;
using HealthyFac.Telemetry.Application.Intake;
using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.TelemetryHistory;

[Collection(SeederCollection.Name)]
public sealed class ReadingRowWriterTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact(Explicit = true)]
    public async Task WriteAsync_WithAHistorySample_StoresTheRowsThatTheIntakeStoresForTheSamePayload()
    {
        var context = await _fixture.RunMachinesSeedAsync(TestContext.Current.CancellationToken);
        var machineId = context.GetId(ReadingRowWriterTestData.LatheKey);
        var data = await HistoryRun.ReadDataAsync(
            _fixture.Hosts.Telemetry, machineId, TestContext.Current.CancellationToken);
        var sample = ReadingRowWriterTestData.CreateLatheSample(data, machineId);
        var rows = new HistoryMessageBuilder(_fixture.Hosts.Telemetry.GetService<IMachineMessageParser>(), data)
            .Build(sample);
        var later = sample.SampledAt + ReadingRowWriterTestData.Later;
        var payload = MachinePayloadBuilder.Build(sample.Sample, data.Definitions, later).Json;
        await WriteAsync([rows], TestContext.Current.CancellationToken);

        var response = await _fixture.Hosts.Telemetry
            .SendAsync<ReceiveMachineMessageRequest, ReceiveMachineMessageResponse>(
                SeederTestData.InitializationActor,
                new ReceiveMachineMessageRequest
                {
                    Topic = rows.Received.Topic,
                    Payload = payload,
                    ReceivedAt = later,
                },
                TestContext.Current.CancellationToken);

        var written = await LoadAsync(machineId, sample.SampledAt);
        var received = await LoadAsync(machineId, later);

        response.Result.Should().Be(ReceiveMachineMessageResult.Stored);
        written.Message.Errors.Should().BeEmpty();
        received.Message.Errors.Should().BeEmpty();
        written.Message.SchemaRevision.Should().Be(received.Message.SchemaRevision);
        ReadingRowWriterTestData.RemoveSampledAt(written.Message.Payload!)
            .Should()
            .Be(ReadingRowWriterTestData.RemoveSampledAt(received.Message.Payload!));
        written.Numeric.Should().NotBeEmpty();
        written.Numeric.Select(ToNumericKey).Should().Equal(received.Numeric.Select(ToNumericKey));
        written.Discrete.Select(ToDiscreteKey).Should().Equal(received.Discrete.Select(ToDiscreteKey));
        written.Statuses.Select(reading => reading.StatusDefinitionId)
            .Should()
            .Equal(received.Statuses.Select(reading => reading.StatusDefinitionId));
    }

    [Fact(Explicit = true)]
    public async Task WriteAsync_WithAGivenMessageId_StoresTheMessageWithThatId()
    {
        var context = await _fixture.RunMachinesSeedAsync(TestContext.Current.CancellationToken);
        var machineId = context.GetId(ReadingRowWriterTestData.LatheKey);
        var data = await HistoryRun.ReadDataAsync(
            _fixture.Hosts.Telemetry, machineId, TestContext.Current.CancellationToken);
        var sample = ReadingRowWriterTestData.CreateLatheSample(data, machineId);
        var rows = new HistoryMessageBuilder(_fixture.Hosts.Telemetry.GetService<IMachineMessageParser>(), data)
            .Build(sample, ReadingRowWriterTestData.MarkerId);

        await WriteAsync([rows], TestContext.Current.CancellationToken);

        var hasMarker = await _fixture.HasReceivedMessageAsync(ReadingRowWriterTestData.MarkerId);

        hasMarker.Should().BeTrue();
    }

    [Fact(Explicit = true)]
    public async Task WriteAsync_CancelledAfterTheMessages_LeavesNoRowOfTheCall()
    {
        var context = await _fixture.RunMachinesSeedAsync(TestContext.Current.CancellationToken);
        var machineId = context.GetId(ReadingRowWriterTestData.LatheKey);
        var data = await HistoryRun.ReadDataAsync(
            _fixture.Hosts.Telemetry, machineId, TestContext.Current.CancellationToken);
        var sample = ReadingRowWriterTestData.CreateLatheSample(data, machineId);
        var built = new HistoryMessageBuilder(_fixture.Hosts.Telemetry.GetService<IMachineMessageParser>(), data)
            .Build(sample);
        using var stop = new CancellationTokenSource();
        var rows = new HistoryRows
        {
            Received = built.Received,
            Numeric = new ReadingRowWriterTestData.CancellingList<NumericReading>(built.Numeric, stop),
            Discrete = built.Discrete,
            Statuses = built.Statuses,
        };
        var act = () => WriteAsync([rows], stop.Token);

        var thrown = await Record.ExceptionAsync(act);

        var counts = await _fixture.CountMessagesByMachineAsync();
        var numericCount = await CountNumericAsync();

        thrown.Should().BeAssignableTo<OperationCanceledException>();
        counts.Should().BeEmpty();
        numericCount.Should().Be(expected: 0);
    }

    [Fact(Explicit = true)]
    public async Task Build_WithAStatusCodeThatTheMachineDoesNotHave_ThrowsInsteadOfStoringAnError()
    {
        var context = await _fixture.RunMachinesSeedAsync(TestContext.Current.CancellationToken);
        var machineId = context.GetId(ReadingRowWriterTestData.LatheKey);
        var data = await HistoryRun.ReadDataAsync(
            _fixture.Hosts.Telemetry, machineId, TestContext.Current.CancellationToken);
        var sample = ReadingRowWriterTestData.CreateLatheSample(data, machineId);
        var parser = _fixture.Hosts.Telemetry.GetService<IMachineMessageParser>();
        var builder = new HistoryMessageBuilder(parser, ReadingRowWriterTestData.WithoutStatuses(data));
        var act = () => builder.Build(sample);

        act.Should().Throw<InvalidOperationException>();
    }

    private static (Guid MetricId, double Value, bool IsOutOfRange) ToNumericKey(NumericReading reading) =>
        (reading.MetricDefinitionId, reading.Value, reading.IsOutOfRange);

    private static (Guid MetricId, string Value, bool IsOutOfRange) ToDiscreteKey(DiscreteReading reading) =>
        (reading.MetricDefinitionId, reading.Value, reading.IsOutOfRange);

    private async Task WriteAsync(IReadOnlyList<HistoryRows> batch, CancellationToken cancellationToken)
    {
        using var scope = _fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);

        await new ReadingRowWriter().WriteAsync(
            scope.ServiceProvider.GetRequiredService<TelemetryDbContext>(), batch, cancellationToken);
    }

    private async Task<int> CountNumericAsync()
    {
        using var scope = _fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);

        return await scope.ServiceProvider.GetRequiredService<TelemetryDbContext>().NumericReadings.CountAsync();
    }

    private async Task<(ReceivedMessage Message, List<NumericReading> Numeric, List<DiscreteReading> Discrete,
        List<StatusReading> Statuses)> LoadAsync(Guid machineId, DateTime sampledAt)
    {
        using var scope = _fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);
        var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
        var message = await dbContext.ReceivedMessages
            .AsNoTracking()
            .SingleAsync(candidate => candidate.MachineRegistrationId == machineId && candidate.SampledAt == sampledAt);

        return (
            message,
            await dbContext.NumericReadings.AsNoTracking()
                .Where(reading => reading.SampledAt == sampledAt)
                .OrderBy(reading => reading.MetricDefinitionId)
                .ToListAsync(),
            await dbContext.DiscreteReadings.AsNoTracking()
                .Where(reading => reading.SampledAt == sampledAt)
                .OrderBy(reading => reading.MetricDefinitionId)
                .ToListAsync(),
            await dbContext.StatusReadings.AsNoTracking()
                .Where(reading => reading.SampledAt == sampledAt)
                .OrderBy(reading => reading.StatusDefinitionId)
                .ToListAsync());
    }
}
