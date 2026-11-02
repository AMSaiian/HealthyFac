namespace HealthyFac.Shared.IntegrationTests.ServiceTokens.Data;

public static class TokenStorageFixtureExtensions
{
    extension(TokenStorageFixture fixture)
    {
        /// <summary>
        /// Reads the time to live of a key straight from Redis.
        /// </summary>
        /// <param name="key">The key to read.</param>
        /// <returns>The time to live, or <see langword="null"/> when the key has none or does not exist.</returns>
        public async Task<TimeSpan?> ReadTimeToLiveAsync(string key)
        {
            var timeToLive = await fixture.Connection.GetDatabase().KeyTimeToLiveAsync(key);

            return timeToLive;
        }
    }
}
