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
                    [key: "grant_type"] = TokenExchangeTestData.GrantType,
                    [key: "client_id"] = RealmTestData.CoreClientId,
                    [key: "client_secret"] = RealmTestData.CoreClientSecret,
                    [key: "subject_token"] = subjectToken,
                    [key: "subject_token_type"] = TokenExchangeTestData.AccessTokenType,
                    [key: "audience"] = audience,
                    [key: "scope"] = scope,
                },
                host);

        /// <summary>
        /// Disables the user, exchanges the token, and enables the user again in a <c>finally</c> block. The
        /// collection shares one realm, so a failed exchange must never leave the user disabled for the next test.
        /// </summary>
        /// <param name="subjectToken">The access token of the user, issued before the user is disabled.</param>
        /// <param name="userId">The Keycloak ID of the user to disable for the exchange.</param>
        /// <param name="audience">The target client.</param>
        /// <param name="scope">The requested scopes, separated by spaces.</param>
        /// <returns>The status code and the body of the answer.</returns>
        public async Task<TokenResponse> ExchangeWhileUserDisabledAsync(
            string subjectToken,
            Guid userId,
            string audience,
            string scope
        )
        {
            await fixture.Realm.SetUserEnabledAsync(userId, isEnabled: false);
            try
            {
                return await fixture.ExchangeAsync(subjectToken, audience, scope);
            }
            finally
            {
                await fixture.Realm.SetUserEnabledAsync(userId, isEnabled: true);
            }
        }
    }
}
