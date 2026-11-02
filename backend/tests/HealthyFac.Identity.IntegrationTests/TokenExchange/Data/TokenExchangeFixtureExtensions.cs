using HealthyFac.Identity.IntegrationTests.Realm.Data;

namespace HealthyFac.Identity.IntegrationTests.TokenExchange.Data;

public static class TokenExchangeFixtureExtensions
{
    extension(TokenExchangeFixture fixture)
    {
        /// <summary>
        /// Exchanges a user token as <c>healthyfac-core</c> does.
        /// </summary>
        /// <param name="subjectToken">The access token of the user.</param>
        /// <param name="audience">The target client.</param>
        /// <param name="scope">The requested scopes, separated by spaces.</param>
        /// <param name="host">The <c>Host</c> header, or <see langword="null"/> for the container address.</param>
        /// <returns>The status code and the body of the answer.</returns>
        public Task<TokenResponse> ExchangeAsync(
            string subjectToken,
            string audience,
            string scope,
            string? host = null
        ) =>
            fixture.Realm.RequestTokenAsync(
                new Dictionary<string, string>
                {
                    ["grant_type"] = TokenExchangeTestData.GrantType,
                    ["client_id"] = RealmTestData.CoreClientId,
                    ["client_secret"] = RealmTestData.CoreClientSecret,
                    ["subject_token"] = subjectToken,
                    ["subject_token_type"] = TokenExchangeTestData.AccessTokenType,
                    ["audience"] = audience,
                    ["scope"] = scope,
                },
                host);
    }
}
