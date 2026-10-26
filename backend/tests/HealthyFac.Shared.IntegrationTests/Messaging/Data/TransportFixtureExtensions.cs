using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class TransportFixtureExtensions
{
    extension(TransportFixture fixture)
    {
        /// <summary>
        /// Waits until the host logged an error that names the message ID, then waits a quiet period, so a
        /// second entry would show in the result.
        /// </summary>
        public async Task<List<string>> WaitForErrorLogsAsync(Guid messageId)
        {
            using var cancellation = new CancellationTokenSource(TransportTestData.WaitTimeout);
            while (!cancellation.IsCancellationRequested && fixture.ReadErrorLogs(messageId).Count == 0)
            {
                await Task.Delay(TransportTestData.PollDelay);
            }

            await Task.Delay(TransportTestData.QuietPeriod);

            return fixture.ReadErrorLogs(messageId);
        }

        private List<string> ReadErrorLogs(Guid messageId) =>
            fixture.Logs.Entries
                .Where(entry => entry.Level == LogLevel.Error && entry.Message.Contains(messageId.ToString()))
                .Select(entry => entry.Message)
                .ToList();
    }
}
