using HealthyFac.Shared.Web.CurrentUser.ServiceActors;

namespace HealthyFac.Shared.UnitTests.CurrentUser.Data;

public static class ServiceActorOptionsTestData
{
    public static TheoryData<ServiceActorOptions> ValidOptions => new()
    {
        new ServiceActorOptions(),
        new ServiceActorOptions { AllowedClients = [IncomingTokenTestData.ServiceClientId] },
        new ServiceActorOptions
        {
            AllowedClients = [IncomingTokenTestData.ServiceClientId, ActorClaimsTestData.OtherServiceClientId],
        },
    };

    public static TheoryData<ServiceActorOptions, string> InvalidOptionsWithExpectedError => new()
    {
        {
            new ServiceActorOptions
            {
                AllowedClients = [IncomingTokenTestData.ServiceClientId, IncomingTokenTestData.ServiceClientId],
            },
            ServiceActorValidationMessages.ClientDuplicated
        },
    };
}
