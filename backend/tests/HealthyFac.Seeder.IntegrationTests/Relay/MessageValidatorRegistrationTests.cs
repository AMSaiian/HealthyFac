using FluentValidation;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.Relay;

[Collection(SeederCollection.Name)]
[Feature(TestFeatures.Seeding)]
[Feature(TestFeatures.Messaging)]
public sealed class MessageValidatorRegistrationTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact(Explicit = true)]
    public void Resolve_ForEveryMessageWithAValidatorClass_ReturnsTheValidatorInEachHost()
    {
        var missing = new List<string>();
        var validatedCount = 0;

        foreach (var host in _fixture.Hosts.All)
        {
            using var scope = host.CreateScope(SeederTestData.InitializationActor);
            var registry = scope.ServiceProvider.GetRequiredService<IntegrationMessageRegistry>();

            foreach (var messageType in registry.MessageClrTypes)
            {
                var validatorInterface = typeof(IValidator<>).MakeGenericType(messageType);

                var hasValidatorClass = AssemblyScanner.FindValidatorsInAssembly(messageType.Assembly)
                    .Any(result => result.InterfaceType == validatorInterface);

                if (!hasValidatorClass)
                {
                    continue;
                }

                validatedCount++;

                if (scope.ServiceProvider.GetService(validatorInterface) is null)
                {
                    missing.Add($"{host.Name.Value}: {messageType.Name}");
                }
            }
        }

        missing.Should().BeEmpty();
        validatedCount.Should().BeGreaterThan(expected: 0);
    }
}
