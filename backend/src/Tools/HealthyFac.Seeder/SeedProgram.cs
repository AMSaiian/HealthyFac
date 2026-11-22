using HealthyFac.Seeder.Accounts;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.ServiceHosts;
using HealthyFac.Seeder.Timeline;
using Microsoft.Extensions.Configuration;

namespace HealthyFac.Seeder;

/// <summary>
/// The flow of the seed job: read the arguments, build the hosts, check the seed files, and run the timeline. It
/// returns the exit code of the process: 0 for a complete seed, 1 for any failure.
/// </summary>
public static class SeedProgram
{
    public const string ResetArgument = "--reset";
    public const string SeedFolderName = "seed/demo";

    private const int Success = 0;
    private const int Failure = 1;

    /// <summary>Runs the job.</summary>
    /// <param name="args">The command line arguments. Only <see cref="ResetArgument"/> is known.</param>
    /// <param name="output">The writer of the log. The log holds event items, never a password or a token.</param>
    /// <param name="baseDirectory">The directory with the <c>services</c> and <c>seed</c> folders.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the process stop cancels.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        string baseDirectory,
        CancellationToken cancellationToken
    )
    {
        var unknown = args.FirstOrDefault(argument => argument != ResetArgument);
        if (unknown is not null)
        {
            await output.WriteLineAsync(SeedProgramErrors.UnknownArgument(unknown));

            return Failure;
        }

        try
        {
            await RunJobAsync(output, baseDirectory, cancellationToken);

            return Success;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await output.WriteLineAsync(SeedProgramErrors.Cancelled);

            return Failure;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            await output.WriteLineAsync(exception.Message);

            return Failure;
        }
    }

    private static async Task RunJobAsync(TextWriter output, string baseDirectory, CancellationToken cancellationToken)
    {
        var anchor = DateTimeOffset.UtcNow;
        var clock = new SeedClock();
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();

        await using var hosts = ServiceHosts.ServiceHosts.Create(configuration, clock, baseDirectory);
        var folder = new SeedFolder(Path.Combine(baseDirectory, SeedFolderName));
        var people = new PeoplePart(hosts, new KeycloakAccounts(hosts.Auth));
        var events = SeedPlan.Load(SeedParts.Create(hosts, people), folder);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(people.CompanyZoneId);

        var check = new SeedCheck(
            anchor,
            zone,
            (request, token) => HostOf(hosts, request).ValidateAsync(request.Request, token));
        var failures = await check.RunAsync(events.Select(planned => planned.Event).ToList(), cancellationToken);
        if (failures.Count > 0)
        {
            foreach (var failure in failures)
            {
                await output.WriteLineAsync(failure.ToString());
            }

            throw new InvalidOperationException(SeedProgramErrors.CheckFailed(failures.Count));
        }

        var relay = hosts.CreateRelay();
        var runner = new TimelineRunner(
            clock,
            anchor,
            zone,
            token => Task.WhenAll(hosts.All.Select(host => host.InitializeAsync(token))),
            relay.RunAsync,
            output.WriteLine);
        var context = new SeedContext { Anchor = anchor, Zone = zone };

        await runner.RunAsync(events, context, cancellationToken);
    }

    private static ServiceHost HostOf(ServiceHosts.ServiceHosts hosts, SeedRequest request) =>
        hosts.All.Single(host => host.Name == request.Service);
}
