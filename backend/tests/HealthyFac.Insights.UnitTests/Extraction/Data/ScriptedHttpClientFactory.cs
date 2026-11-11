using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Insights.UnitTests.Extraction.Data;

/// <summary>
/// Creates clients over a scripted handler, so a test answers each Telemetry call by path and records the request
/// path, the body, and the downstream scopes at the time of the call.
/// </summary>
public sealed class ScriptedHttpClientFactory(IDownstreamScopeHolder downstreamScopes) : IHttpClientFactory
{
    private readonly ScriptedHandler _handler = new(downstreamScopes);

    public Func<HttpRequestMessage, string?, HttpResponseMessage> Respond
    {
        get => _handler.Respond;
        set => _handler.Respond = value;
    }

    public IReadOnlyList<RecordedCall> Calls => _handler.Calls;

    public HttpClient CreateClient(string name) =>
        new(_handler, disposeHandler: false) { BaseAddress = new Uri(uriString: "http://telemetry.test/") };

    private sealed class ScriptedHandler(IDownstreamScopeHolder downstreamScopes) : HttpMessageHandler
    {
        private readonly IDownstreamScopeHolder _downstreamScopes = downstreamScopes;
        private readonly List<RecordedCall> _calls = [];

        public Func<HttpRequestMessage, string?, HttpResponseMessage> Respond { get; set; } =
            (_, _) => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);

        public IReadOnlyList<RecordedCall> Calls => _calls;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            _calls.Add(
                new RecordedCall(
                    request.Method.Method,
                    request.RequestUri!.AbsolutePath,
                    body,
                    _downstreamScopes.Current.ToList()));

            return Respond(request, body);
        }
    }
}

/// <summary>
/// One recorded Telemetry call.
/// </summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The absolute path.</param>
/// <param name="Body">The request body, or <see langword="null"/>.</param>
/// <param name="Scopes">The downstream scopes at the time of the call.</param>
public sealed record RecordedCall(string Method, string Path, string? Body, IReadOnlyList<string> Scopes);

/// <summary>
/// A readable test double of the ID encryptor: the encrypted form is a fixed prefix and the plain GUID.
/// </summary>
public sealed class PrefixIdEncryptor : IIdEncryptor
{
    public const string Prefix = "enc-";

    public string Encrypt(Guid id) => $"{Prefix}{id:N}";

    public Guid Decrypt(string encryptedId) => Guid.ParseExact(encryptedId[Prefix.Length..], format: "N");

    public string Migrate(string encryptedId) => encryptedId;
}
