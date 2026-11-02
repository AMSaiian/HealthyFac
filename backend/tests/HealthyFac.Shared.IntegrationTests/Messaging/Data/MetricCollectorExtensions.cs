using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Messaging.Tags;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class MetricCollectorExtensions
{
    extension<T>(MetricCollector<T> collector)
        where T : struct
    {
        /// <summary>
        /// Reads the measurements of one message type, so the rows that other tests of the class left behind do
        /// not count.
        /// </summary>
        public IReadOnlyList<CollectedMeasurement<T>> ForMessageType(string messageType) =>
            collector.GetMeasurementSnapshot()
                .Where(measurement => Equals(measurement.Tags.GetValueOrDefault(Tags.MessageType), messageType))
                .ToList();

        /// <summary>
        /// Reads the current values of an observable instrument for one processor.
        /// </summary>
        public IReadOnlyList<T> ObserveForProcessor(string processor)
        {
            collector.RecordObservableInstruments();

            return collector.GetMeasurementSnapshot(true)
                .Where(measurement => Equals(measurement.Tags.GetValueOrDefault(Tags.Processor), processor))
                .Select(measurement => measurement.Value)
                .ToList();
        }
    }
}
