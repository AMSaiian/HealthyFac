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

    public static InvalidOperationException UseCasesAlreadyAdded() =>
        new(
            "AddUseCases ran before. A service calls AddUseCases once, with its application assembly. "
            + "A second call would decorate the handlers of the first call twice."
        );
}
