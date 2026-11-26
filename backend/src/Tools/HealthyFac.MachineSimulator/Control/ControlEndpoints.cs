using FluentValidation.Results;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.Simulation;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace HealthyFac.MachineSimulator.Control;

/// <summary>
/// Maps the control API of the simulator. It has no authentication: the service listens on the private network only.
/// </summary>
public static class ControlEndpoints
{
    private static readonly string IsRunningKey = ValidationNames.GetJsonName(
        typeof(SetRunRequest).GetProperty(nameof(SetRunRequest.IsRunning))!);

    private static readonly string ModeKey = ValidationNames.GetJsonName(
        typeof(SetModuleModeRequest).GetProperty(nameof(SetModuleModeRequest.Mode))!);

    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapControlEndpoints()
        {
            app.MapGet(ControlRoutes.MachinesRoute, List);
            app.MapGet(ControlRoutes.MachineRoute, Get);
            app.MapPut(ControlRoutes.RunRoute, SetRun);
            app.MapPut(ControlRoutes.ModeRoute, SetMode);

            return app;
        }
    }

    private static Ok<IReadOnlyList<SimulatedMachineResponse>> List(SimulationEngine engine, TimeProvider timeProvider)
    {
        IReadOnlyList<SimulatedMachineResponse> machines =
        [
            .. engine
                .Snapshot(timeProvider.GetUtcNow().UtcDateTime)
                .Select(SimulatedMachineResponse.From),
        ];

        return TypedResults.Ok(machines);
    }

    private static Ok<SimulatedMachineResponse> Get(
        [FromRoute(Name = ControlRoutes.MachineIdParameter)] Guid machineId,
        SimulationEngine engine,
        TimeProvider timeProvider
    )
    {
        var view = engine.Find(machineId, timeProvider.GetUtcNow().UtcDateTime)
            ?? throw new NotFoundException(SimulatorErrors.MachineNotFound);

        return TypedResults.Ok(SimulatedMachineResponse.From(view));
    }

    private static NoContent SetRun(
        [FromRoute(Name = ControlRoutes.MachineIdParameter)] Guid machineId,
        SetRunRequest request,
        SimulationEngine engine
    )
    {
        if (request.IsRunning is not { } isRunning)
        {
            throw Invalid(IsRunningKey, SimulatorErrors.RunRequired);
        }

        ThrowIfNotDone(engine.SetRunning(machineId, isRunning));

        return TypedResults.NoContent();
    }

    private static NoContent SetMode(
        [FromRoute(Name = ControlRoutes.MachineIdParameter)] Guid machineId,
        [FromRoute(Name = ControlRoutes.ModuleParameter)]
        string module,
        SetModuleModeRequest request,
        SimulationEngine engine,
        TimeProvider timeProvider
    )
    {
        if (request.Mode is not { } mode)
        {
            throw Invalid(ModeKey, SimulatorErrors.ModeRequired);
        }

        var isKnownMachine = engine.Find(machineId, timeProvider.GetUtcNow().UtcDateTime) is not null;

        var result = ModuleName.TryFromValue(module, out var moduleName)
            ? engine.SetMode(machineId, moduleName, mode)
            : isKnownMachine
                ? ControlResult.ModuleNotFound
                : ControlResult.MachineNotFound;

        ThrowIfNotDone(result);

        return TypedResults.NoContent();
    }

    private static void ThrowIfNotDone(ControlResult result)
    {
        if (result == ControlResult.MachineNotFound)
        {
            throw new NotFoundException(SimulatorErrors.MachineNotFound);
        }

        if (result == ControlResult.ModuleNotFound)
        {
            throw new NotFoundException(SimulatorErrors.ModuleNotFound);
        }
    }

    private static AppValidationException Invalid(string property, string message) =>
        new([new ValidationFailure(property, message)]);
}
