using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.UnitTests.UseCases.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.UnitTests.UseCases;

public sealed class UseCaseRegistrationTests
{
    [Fact]
    public async Task AddUseCases_WithRequestWithoutResult_RunsValidation()
    {
        var probe = new HandlerProbe();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(probe);
        services.AddUseCases(UseCaseRegistrationTestData.NoResultTypes);
        await using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<NoResultRequest>>();

        var act = () => handler.HandleAsync(new NoResultRequest { Name = UseCaseRegistrationTestData.BlankName });

        await act.Should().ThrowAsync<AppValidationException>();
        probe.RunCount.Should().Be(0);
    }

    [Fact]
    public void AddUseCases_WithMixedTypes_RegistersOnlyTheHandlers()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddUseCases(UseCaseRegistrationTestData.NoResultTypes);

        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(HandlerProbe));
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(NoResultRequest));
    }

    [Fact]
    public void AddUseCases_CalledTwice_Throws()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUseCases(UseCaseRegistrationTestData.NoResultTypes);

        var act = () => services.AddUseCases(UseCaseRegistrationTestData.NoResultTypes);

        act.Should().Throw<InvalidOperationException>().WithMessage(UseCaseRegistrationTestData.AddedTwiceText);
    }

    [Fact]
    public void AddUseCases_WithTwoHandlersForOneRequest_Throws()
    {
        var services = new ServiceCollection();

        var act = () => services.AddUseCases(UseCaseRegistrationTestData.DuplicatedTypes);

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{nameof(DuplicatedRequest)}*");
    }
}
