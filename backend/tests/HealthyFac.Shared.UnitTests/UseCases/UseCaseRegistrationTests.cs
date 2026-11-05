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
        probe.RunCount.Should().Be(expected: 0);
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

    [Fact]
    public void AddUseCases_WithRequestWithoutPreHandlerCheck_Throws()
    {
        var services = new ServiceCollection();

        var act = () => services.AddUseCases(UseCaseRegistrationTestData.UnruledTypes);

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{nameof(UnruledRequest)}*");
    }

    [Fact]
    public void AddUseCases_WithTwoPreHandlerChecksForOneRequest_Throws()
    {
        var services = new ServiceCollection();

        var act = () => services.AddUseCases(UseCaseRegistrationTestData.DoubleRuledTypes);

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{nameof(DoubleRuledRequest)}*");
    }

    [Fact]
    public async Task HandleAsync_WithRejectingPreHandlerCheck_DoesNotRunHandler()
    {
        var probe = new HandlerProbe();
        await using var provider = BuildProvider(probe, UseCaseRegistrationTestData.RuledTypes);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<RuledRequest, int>>();

        var act = () => handler.HandleAsync(new RuledRequest { Name = "machine", IsRejected = true });

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        probe.RuleRunCount.Should().Be(expected: 1);
        probe.RunCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidInput_DoesNotRunPreHandlerCheck()
    {
        var probe = new HandlerProbe();
        await using var provider = BuildProvider(probe, UseCaseRegistrationTestData.RuledTypes);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<RuledRequest, int>>();

        var act = () => handler.HandleAsync(
            new RuledRequest { Name = UseCaseRegistrationTestData.BlankName, IsRejected = false });

        await act.Should().ThrowAsync<AppValidationException>();
        probe.RuleRunCount.Should().Be(expected: 0);
        probe.RunCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task HandleAsync_WithoutResult_RunsPreHandlerCheck()
    {
        var probe = new HandlerProbe();
        await using var provider = BuildProvider(probe, UseCaseRegistrationTestData.NoResultTypes);
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<NoResultRequest>>();

        await handler.HandleAsync(new NoResultRequest { Name = "machine" });

        probe.RuleRunCount.Should().Be(expected: 1);
        probe.RunCount.Should().Be(expected: 1);
    }

    private static ServiceProvider BuildProvider(HandlerProbe probe, Type[] types)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(probe);
        services.AddUseCases(types);

        return services.BuildServiceProvider(validateScopes: true);
    }
}
