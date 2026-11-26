using HealthyFac.Seeder.Accounts;
using HealthyFac.Seeder.Machines;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.Reset;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.ServiceHosts;
using HealthyFac.Seeder.TelemetryHistory;
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
    public const string TelemetryArgument = "--telemetry";
    public const string SeedFolderName = "seed/demo";

    private const string ResetItem = "reset";
    private const int Success = 0;
    private const int Failure = 1;

    /// <summary>Runs the job.</summary>
    /// <param name="args">
    /// The command line arguments. <see cref="ResetArgument"/> and <see cref="TelemetryArgument"/> are known.
    /// </param>
    /// <param name="output">The writer of the log. The log holds event items, never a password or a token.</param>
    /// <param name="baseDirectory">The directory with the <c>services</c> and <c>seed</c> folders.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the process stop cancels.</param>
    /// <returns>The exit code.</returns>
    public static Task<int> RunAsync(
        string[] args,
        TextWriter output,
        string baseDirectory,
        CancellationToken cancellationToken
    ) =>
        RunAsync(
            args,
            output,
            baseDirectory,
            new ConfigurationBuilder().AddEnvironmentVariables().Build(),
            DateTimeOffset.UtcNow,
            cancellationToken);

    internal static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        string baseDirectory,
        IConfiguration configuration,
        DateTimeOffset anchor,
        CancellationToken cancellationToken
    )
    {
        var unknown = args.FirstOrDefault(argument => argument is not (ResetArgument or TelemetryArgument));

        if (unknown is not null)
        {
            await output.WriteLineAsync(SeedProgramErrors.UnknownArgument(unknown));

            return Failure;
        }

        try
        {
            var isReset = args.Contains(ResetArgument);

            if (isReset && !await ResetAsync(output, baseDirectory, configuration, anchor, cancellationToken))
            {
                return Failure;
            }

            return await RunJobAsync(
                output, baseDirectory, configuration, anchor, args.Contains(TelemetryArgument), cancellationToken);
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

    private static async Task<int> RunJobAsync(
        TextWriter output,
        string baseDirectory,
        IConfiguration configuration,
        DateTimeOffset anchor,
        bool isTelemetry,
        CancellationToken cancellationToken
    )
    {
        var clock = new SeedClock();

        await using var hosts = ServiceHosts.ServiceHosts.Create(configuration, clock, baseDirectory);
        var folder = new SeedFolder(Path.Combine(baseDirectory, SeedFolderName));
        var people = new PeoplePart(hosts, new KeycloakAccounts(hosts.Auth));
        var events = SeedPlan.Load(SeedParts.Create(hosts, people, isTelemetry), folder);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(people.CompanyZoneId);

        var check = new SeedCheck(
            anchor,
            zone,
            (request, token) => HostOf(hosts, request).ValidateAsync(request.Request, token),
            SeedCompletion.FindMarkerItem(folder),
            isTelemetry ? ReadScript(folder) : null);

        var failures = await check.RunAsync([.. events.Select(planned => planned.Event)], cancellationToken);

        if (failures.Count > 0)
        {
            foreach (var failure in failures)
            {
                await output.WriteLineAsync(failure.ToString());
            }

            throw new InvalidOperationException(SeedProgramErrors.CheckFailed(failures.Count));
        }

        var exitCode = Success;

        var completion = new SeedCompletion(
            hosts.Core,
            hosts.Telemetry,
            SeedCompletion.FindLastDraftId(folder),
            SeedCompletion.FindMarkerMessageId(folder));

        var relay = hosts.CreateRelay();

        var runner = new TimelineRunner(
            clock,
            anchor,
            zone,
            token => Task.WhenAll(hosts.All.Select(host => host.InitializeAsync(token))),
            relay.RunAsync,
            output.WriteLine,
            token => ContinueAsync(completion, output, isTelemetry, code => exitCode = code, token));

        var context = new SeedContext { Anchor = anchor, Zone = zone };

        await runner.RunAsync(events, context, cancellationToken);

        return exitCode;
    }

    private static TelemetryScript ReadScript(SeedFolder folder) =>
        new()
        {
            Telemetry = folder.Read<TelemetryFile>(TelemetryHistoryConstants.FileName),
            Machines = folder.Read<MachinesFile>(MachinesConstants.FileName),
        };

    private static async Task<bool> ResetAsync(
        TextWriter output,
        string baseDirectory,
        IConfiguration configuration,
        DateTimeOffset anchor,
        CancellationToken cancellationToken
    )
    {
        var clock = new SeedClock();
        await using var hosts = ServiceHosts.ServiceHosts.Create(configuration, clock, baseDirectory);
        var folder = new SeedFolder(Path.Combine(baseDirectory, SeedFolderName));
        var companyId = folder.Read<CompanyFile>(PeopleConstants.FileName).Company.Id;
        clock.Enter(anchor, ResetItem);

        var reset = new SeedReset(
            hosts,
            new KeycloakAccounts(hosts.Auth),
            companyId,
            SeedCompletion.FindLastDraftId(folder),
            SeedCompletion.FindMarkerMessageId(folder),
            output);

        return await reset.RunAsync(cancellationToken);
    }

    private static async Task<bool> ContinueAsync(
        SeedCompletion completion,
        TextWriter output,
        bool isTelemetry,
        Action<int> setExitCode,
        CancellationToken cancellationToken
    )
    {
        var state = await completion.ReadAsync(cancellationToken);

        if (state == SeedState.Empty)
        {
            return true;
        }

        var isComplete = state == SeedState.CompleteWithHistory || (state == SeedState.Complete && !isTelemetry);
        var incompleteText = isTelemetry ? SeedProgramErrors.IncompleteWithTelemetry : SeedProgramErrors.Incomplete;

        var text = state switch
        {
            _ when isComplete => SeedProgramErrors.Complete,
            SeedState.Complete => SeedProgramErrors.NoCompleteHistory,
            _ => incompleteText,
        };

        await output.WriteLineAsync(text);
        setExitCode(isComplete ? Success : Failure);

        return false;
    }

    private static ServiceHost HostOf(ServiceHosts.ServiceHosts hosts, SeedRequest request) =>
        hosts.All.Single(host => host.Name == request.Service);
}
