using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Shared.Application.UseCases;

/// <summary>
/// Defines the exceptions of the use case registration.
/// </summary>
public static class UseCaseErrors
{
    public static InvalidOperationException HandlerAlreadyRegistered(Type requestType) =>
        new(
            $"The request '{requestType.Name}' already has a handler. Each request has one handler. "
            + "A second handler would replace the first without an error."
        );

    public static InvalidOperationException PreHandlerCheckMissing(Type requestType) =>
        new(
            $"The request '{requestType.Name}' has no pre-handler check. Each request has one IPreHandlerCheck. "
            + "A use case without a scope rule declares an empty subclass of PassThroughPreHandlerCheck."
        );

    public static InvalidOperationException PreHandlerCheckAlreadyRegistered(Type requestType) =>
        new(
            $"The request '{requestType.Name}' already has a pre-handler check. "
            + "Each request has one pre-handler check. A second one would run beside the first without a clear order."
        );

    public static InvalidOperationException UseCasesAlreadyAdded() =>
        new(
            "AddUseCases ran before. A service calls AddUseCases once, with its application assembly. "
            + "A second call would decorate the handlers of the first call twice."
        );
}
