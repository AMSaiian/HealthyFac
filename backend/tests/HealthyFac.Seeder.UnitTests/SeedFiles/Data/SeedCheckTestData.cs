using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Shared.Application.CurrentUser.Actors;

namespace HealthyFac.Seeder.UnitTests.SeedFiles.Data;

public static class SeedCheckTestData
{
    public const string InvalidRequestText = "name: The name is required.";

    public static readonly DateTimeOffset Anchor = new(
        year: 2027, month: 3, day: 30, hour: 10, minute: 0, second: 0, TimeSpan.Zero);

    public static readonly object InvalidRequest = new();

    public static TheoryData<string, IReadOnlyList<SeedEvent>, string, string> FailingCases => new()
    {
        {
            "a time in the spring clock change",
            [Event(file: "people.json", key: "anna", at: "d-2 03:30")],
            "people.json#anna",
            "does not exist"
        },
        {
            "a date without a time",
            [Event(file: "people.json", key: "anna", at: "d-2")],
            "people.json#anna",
            "needs a time"
        },
        {
            "an event after the anchor",
            [Event(file: "orders.json", key: "order-1", at: "d+1 09:00")],
            "orders.json#order-1",
            "Events lie in the past"
        },
        {
            "a key that no event creates",
            [Event(file: "orders.json", key: "order-1", at: "d-1 09:00", uses: ["machine-1"])],
            "orders.json#order-1",
            "No event creates the key machine-1"
        },
        {
            "a key that a later event creates",
            [
                Event(file: "orders.json", key: "order-1", at: "d-2 09:00", uses: ["machine-1"]),
                Event(file: "machines.json", key: "machine-1", at: "d-1 09:00", ["machine-1"]),
            ],
            "orders.json#order-1",
            "used before the event machines.json#machine-1"
        },
        {
            "a key that an event with the same moment creates later in the file order",
            [
                Event(file: "orders.json", key: "order-1", at: "d-1 09:00", uses: ["machine-1"]),
                Event(file: "machines.json", key: "machine-1", at: "d-1 09:00", ["machine-1"]),
            ],
            "orders.json#order-1",
            "used before the event"
        },
        {
            "a request that the validator rejects",
            [Event(file: "machines.json", key: "machine-1", at: "d-1 09:00", requests: [InvalidRequest])],
            "machines.json#machine-1",
            InvalidRequestText
        },
        {
            "a password that is too short",
            [Event(file: "people.json", key: "anna", at: "d-1 09:00", credentials: [("anna", "short")])],
            "people.json#anna",
            "at least 12 characters"
        },
        {
            "a password that equals the username",
            [
                Event(
                    file: "people.json", key: "anna", at: "d-1 09:00",
                    credentials: [("anna.kovalenko", "anna.kovalenko")]),
            ],
            "people.json#anna",
            "differ from the username"
        },
    };

    public static IReadOnlyList<SeedEvent> CleanEvents =>
    [
        Event(file: "machines.json", key: "machine-1", at: "d-3 09:00", ["machine-1"]),
        Event(file: "orders.json", key: "order-1", at: "d-1 09:00", uses: ["machine-1"]),
        Event(file: "machines.json", key: "machine-2", at: "d-1 09:00", ["machine-2"]),
        Event(file: "orders.json", key: "order-2", at: "d-1 09:00", uses: ["machine-2"]),
        Event(file: "people.json", key: "anna", at: "d-4 09:00", credentials: [("anna", "Kalyna-Demo-2026!")]),
    ];

    public static Task<IReadOnlyList<string>> ValidateAsync(SeedRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> failures = ReferenceEquals(request.Request, InvalidRequest) ? [InvalidRequestText] : [];

        return Task.FromResult(failures);
    }

    private static SeedEvent Event(
        string file,
        string key,
        string at,
        string[]? creates = null,
        string[]? uses = null,
        object[]? requests = null,
        (string Username, string Password)[]? credentials = null
    ) =>
        new()
        {
            At = RelativeMoment.Parse(at),
            Item = SeedItem.Of(file, key),
            Creates = creates ?? [],
            Uses = uses ?? [],
            Requests = (requests ?? []).Select(request => new SeedRequest
                {
                    Service = ServiceName.Core,
                    Request = request,
                })
                .ToList(),
            Credentials = (credentials ?? []).Select(credential => new SeedCredential
                {
                    Username = credential.Username,
                    Password = credential.Password,
                })
                .ToList(),
        };
}
