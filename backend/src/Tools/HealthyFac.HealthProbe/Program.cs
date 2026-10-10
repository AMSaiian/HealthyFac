using HealthyFac.HealthProbe;

if (args.Length == 0 || !IsSchemaValid(args[0], out var uri))
{
    await Console.Error.WriteLineAsync(ProbeConstants.Messages.Usage);

    return ProbeConstants.ExitCodes.InvalidUsage;
}

using var handler = new HttpClientHandler
{
    AllowAutoRedirect = false
};
using var client = new HttpClient(handler)
{
    Timeout = TimeSpan.FromSeconds(ProbeConstants.TimeoutSeconds),
};

try
{
    using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
    var statusCode = (int)response.StatusCode;

    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine(ProbeConstants.Messages.Healthy, statusCode);

        return ProbeConstants.ExitCodes.Healthy;
    }

    Console.WriteLine(ProbeConstants.Messages.Unhealthy, statusCode);

    return ProbeConstants.ExitCodes.Unhealthy;
}
catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
{
    Console.WriteLine(ProbeConstants.Messages.Unhealthy, exception.GetType().Name);

    return ProbeConstants.ExitCodes.Unhealthy;
}

bool IsSchemaValid(string uriString, out Uri? uri)
{
    uri = null;

    return Uri.TryCreate(uriString, UriKind.Absolute, out uri)
        && uri.Scheme is ProbeConstants.Schemes.Http
            or ProbeConstants.Schemes.Https;
}
