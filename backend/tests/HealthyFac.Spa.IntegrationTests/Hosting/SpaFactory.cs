using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HealthyFac.Spa.IntegrationTests.Hosting;

/// <summary>
/// Starts the SPA host in memory over a temporary content folder, so the tests need no frontend build.
/// </summary>
public sealed class SpaFactory : WebApplicationFactory<Program>
{
    public const string ApiBaseUrl = "https://api.healthyfac.test";
    public const string AuthUrl = "https://auth.healthyfac.test";
    public const string AuthRealm = "healthyfac";
    public const string AuthClientId = "healthyfac-spa";
    public const string PrimeUiLicenseKey = "test-primeui-license-key";
    public const string SilentSsoFileName = "silent-check-sso.html";
    public const string HashedAssetName = "main-ABCDEFGH.js";
    public const string LookalikeFileName = "data-ABCDEFGHx.json";
    public const string UnhashedFileName = "i18n/en.json";
    public const string NoncePlaceholder = "__CSP_NONCE__";

    private const string TestEnvironment = "Test";

    private readonly string _contentRoot;
    private readonly bool _isTelemetryEnabled;
    private readonly IReadOnlyDictionary<string, string> _settings;

    public SpaFactory(
        bool hasIndexFile = true,
        bool isTelemetryEnabled = false,
        IReadOnlyDictionary<string, string>? settings = null
    )
    {
        _isTelemetryEnabled = isTelemetryEnabled;
        _settings = settings ?? new Dictionary<string, string>();
        _contentRoot = Path.Combine(Path.GetTempPath(), $"healthyfac-spa-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_contentRoot, "i18n"));
        if (hasIndexFile)
        {
            File.WriteAllText(
                Path.Combine(_contentRoot, "index.html"),
                $"<html><body><hf-root ngCspNonce=\"{NoncePlaceholder}\"></hf-root>"
                + $"<script nonce=\"{NoncePlaceholder}\"></script></body></html>");
        }

        File.WriteAllText(Path.Combine(_contentRoot, HashedAssetName), "console.log(1);");
        File.WriteAllText(Path.Combine(_contentRoot, LookalikeFileName), "x");
        File.WriteAllText(Path.Combine(_contentRoot, UnhashedFileName), "{}");
        File.WriteAllText(Path.Combine(_contentRoot, SilentSsoFileName), "<html></html>");
        File.WriteAllText(Path.Combine(_contentRoot, "config.json"), "{\"apiBaseUrl\":\"https://file.example.com\"}");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestEnvironment);
        builder.UseSetting("Spa:ContentRoot", _contentRoot);
        builder.UseSetting("Spa:ApiBaseUrl", ApiBaseUrl);
        builder.UseSetting("Spa:AuthUrl", AuthUrl);
        builder.UseSetting("Spa:PrimeUiLicenseKey", PrimeUiLicenseKey);

        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }

        if (_isTelemetryEnabled)
        {
            builder.UseSetting("Tracing:IsEnabled", bool.TrueString);
            builder.UseSetting("Metrics:IsEnabled", bool.TrueString);
            builder.UseSetting("OpenTelemetryLogging:IsEnabled", bool.TrueString);
        }
    }

    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);
        if (isDisposing && Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, true);
        }
    }
}
