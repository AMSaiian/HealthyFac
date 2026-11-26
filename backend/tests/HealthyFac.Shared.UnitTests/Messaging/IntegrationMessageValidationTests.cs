using FluentValidation;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Shared.UnitTests.Messaging.Data;
using HealthyFac.Shared.UnitTests.UseCases.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.UnitTests.Messaging;

[Feature(TestFeatures.Messaging)]
public sealed class IntegrationMessageValidationTests
{
    [Fact]
    public void
        AddIntegrationMessage_WhenTheApplicationAssemblyHoldsNoValidator_RegistersTheValidatorOfTheMessageAssembly()
    {
        var services = CreateServicesWithoutTheMessageValidator();

        services.AddIntegrationMessage<ValidatedMessage>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IValidator<ValidatedMessage>>().ToList();
        validators.Should().ContainSingle().Which.Should().BeOfType<ValidatedMessageValidator>();
    }

    [Fact]
    public void AddIntegrationMessage_WhenAddUseCasesRegisteredTheValidator_AddsNoSecondRegistration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new HandlerProbe());
        services.AddUseCases(IntegrationMessageValidationTestData.Types);

        services.AddIntegrationMessage<ValidatedMessage>();

        services.Where(descriptor => descriptor.ServiceType == typeof(IValidator<ValidatedMessage>))
            .Should()
            .ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_WithAnInvalidMessage_ThrowsAppValidationExceptionAndSkipsTheUseCase()
    {
        var probe = new HandlerProbe();
        var services = CreateServicesWithoutTheMessageValidator(probe);
        services.AddIntegrationMessage<ValidatedMessage>();
        await using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<ValidatedMessage, Unit>>();

        var act = () =>
            handler.HandleAsync(new ValidatedMessage { Name = IntegrationMessageValidationTestData.BlankName });

        await act.Should().ThrowAsync<AppValidationException>();
        probe.RunCount.Should().Be(expected: 0);
    }

    private static ServiceCollection CreateServicesWithoutTheMessageValidator(HandlerProbe? probe = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(probe ?? new HandlerProbe());
        services.AddUseCases(IntegrationMessageValidationTestData.Types);

        var validators = services.Where(descriptor => descriptor.ServiceType == typeof(IValidator<ValidatedMessage>))
            .ToList();

        validators.ForEach(descriptor => services.Remove(descriptor));

        return services;
    }
}
