namespace HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;

public static class RateLimitClientExtensions
{
    extension(HttpClient client)
    {
        public Task<HttpResponseMessage> PostVerifyInviteAsync(string? forwardedFor = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, RateLimitTestData.VerifyInviteRoute);
            if (forwardedFor is not null)
            {
                request.Headers.Add(RateLimitTestData.ForwardedForHeader, forwardedFor);
            }

            return client.SendAsync(request);
        }

        public async Task SpendDefaultLimitAsync()
        {
            for (var attempt = 0; attempt < RateLimitTestData.DefaultPermitLimit; attempt++)
            {
                using var response = await client.GetAsync(RateLimitTestData.MachinesRoute);
            }
        }

        public async Task SpendStrictLimitAsync()
        {
            for (var attempt = 0; attempt < RateLimitTestData.StrictPermitLimit; attempt++)
            {
                using var response = await client.PostVerifyInviteAsync();
            }
        }
    }
}
