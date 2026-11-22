using HealthyFac.Seeder.Accounts;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.Timeline;

namespace HealthyFac.Seeder.SeedFiles;

/// <summary>
/// Checks the seed events before the job writes anything. It returns every failure, so one run shows all problems
/// of the files.
/// </summary>
public sealed class SeedCheck
{
    private readonly DateTimeOffset _anchor;
    private readonly TimeZoneInfo _zone;
    private readonly Func<SeedRequest, CancellationToken, Task<IReadOnlyList<string>>> _validate;

    /// <summary>Creates a check.</summary>
    /// <param name="anchor">The start of the job.</param>
    /// <param name="zone">The company zone.</param>
    /// <param name="validate">
    /// Runs the validator of the owning service on a request, without a database call. It returns the failure texts.
    /// </param>
    public SeedCheck(
        DateTimeOffset anchor,
        TimeZoneInfo zone,
        Func<SeedRequest, CancellationToken, Task<IReadOnlyList<string>>> validate
    )
    {
        _anchor = anchor;
        _zone = zone;
        _validate = validate;
    }

    /// <summary>Checks the events.</summary>
    /// <param name="events">The events of all parts, in file order.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The failures. The list is empty when the events are clean.</returns>
    public async Task<IReadOnlyList<SeedCheckFailure>> RunAsync(
        IReadOnlyList<SeedEvent> events,
        CancellationToken cancellationToken
    )
    {
        var failures = new List<SeedCheckFailure>();
        var moments = new List<(SeedEvent Event, DateTimeOffset At)>();
        foreach (var seedEvent in events)
        {
            var moment = ResolveMoment(seedEvent, failures);
            if (moment is { } at)
            {
                moments.Add((seedEvent, at));
            }
        }

        var order = moments.OrderBy(entry => entry.At).Select(entry => entry.Event).ToList();
        CheckKeys(events, order, failures);
        foreach (var seedEvent in events)
        {
            await CheckRequestsAsync(seedEvent, failures, cancellationToken);
            CheckCredentials(seedEvent, failures);
        }

        return failures;
    }

    private DateTimeOffset? ResolveMoment(SeedEvent seedEvent, List<SeedCheckFailure> failures)
    {
        try
        {
            var at = SeedCalendar.ToUtc(seedEvent.At, _anchor, _zone);
            if (at > _anchor)
            {
                failures.Add(Failure(seedEvent, SeedFileErrors.EventsLieInThePast));
            }

            return at;
        }
        catch (MomentException exception)
        {
            failures.Add(Failure(seedEvent, exception.Message));

            return null;
        }
    }

    private static void CheckKeys(
        IReadOnlyList<SeedEvent> events,
        IReadOnlyList<SeedEvent> order,
        List<SeedCheckFailure> failures
    )
    {
        var creators = events
            .SelectMany(seedEvent => seedEvent.Creates.Select(key => (Key: key, Event: seedEvent)))
            .GroupBy(entry => entry.Key)
            .ToDictionary(group => group.Key, group => group.First().Event);

        foreach (var seedEvent in events)
        {
            foreach (var key in seedEvent.Uses)
            {
                if (!creators.TryGetValue(key, out var creator))
                {
                    failures.Add(Failure(seedEvent, SeedFileErrors.KeyNotCreated(key)));

                    continue;
                }

                var isCreatedLater = IndexOf(order, creator) > IndexOf(order, seedEvent);
                var isOrdered = IndexOf(order, creator) >= 0 && IndexOf(order, seedEvent) >= 0;
                if (isOrdered && isCreatedLater)
                {
                    failures.Add(Failure(seedEvent, SeedFileErrors.KeyUsedBeforeCreated(key, creator.Item)));
                }
            }
        }
    }

    private async Task CheckRequestsAsync(
        SeedEvent seedEvent,
        List<SeedCheckFailure> failures,
        CancellationToken cancellationToken
    )
    {
        foreach (var request in seedEvent.Requests)
        {
            foreach (var reason in await _validate(request, cancellationToken))
            {
                failures.Add(Failure(seedEvent, SeedFileErrors.RequestInvalid(reason)));
            }
        }
    }

    private static void CheckCredentials(SeedEvent seedEvent, List<SeedCheckFailure> failures)
    {
        foreach (var credential in seedEvent.Credentials)
        {
            if (RealmPasswordPolicy.FindViolation(credential.Username, credential.Password) is { } violation)
            {
                failures.Add(Failure(seedEvent, violation));
            }
        }
    }

    private static int IndexOf(IReadOnlyList<SeedEvent> order, SeedEvent seedEvent)
    {
        for (var index = 0; index < order.Count; index++)
        {
            if (ReferenceEquals(order[index], seedEvent))
            {
                return index;
            }
        }

        return -1;
    }

    private static SeedCheckFailure Failure(SeedEvent seedEvent, string rule) =>
        new() { File = SeedItem.FileOf(seedEvent.Item), Item = seedEvent.Item, Rule = rule };
}
