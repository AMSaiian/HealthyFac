using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Shared.UnitTests.CurrentUser.Data;

public static class KnownActorsTestData
{
    public const string UnlistedKey = "service:core:notAListedFlow";
    public const string OutboxProcessorName = "core:outboxProcessor";
    public const string VersionDigits = "5";
    public const int VersionCharacterIndex = 14;

    public static readonly Guid KeycloakUserId = Guid.Parse("3f2b8c1e-5a4d-4e6f-9b7a-1c2d3e4f5a6b");
    public static readonly Guid AnonymousId = Guid.Parse("8a43dd69-0d6c-521b-9dad-f9c07cb5be51");

    public static TheoryData<string> ActorValues
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var value in ActorKind.All.Select(kind => kind.Value)
                .Concat(ServiceName.All.Select(service => service.Value))
                .Concat(ServiceActor.All.Select(flow => flow.Value)))
            {
                data.Add(value);
            }

            return data;
        }
    }
}
