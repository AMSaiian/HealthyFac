using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Shared.UnitTests.Messaging.Data;

public static class RegistryTestData
{
    public const string FirstMessageType = "test.v1:RegistryFirst";
    public const string UnknownMessageType = "test.v1:RegistryUnknown";
    public const string HandlerAlreadyRegisteredText = $"*'{FirstMessageType}'*one handler*";
    public const string MessageTypeConflictText = $"*'{FirstMessageType}'*already registered*";
    public const string MessageHandlerMissingText = $"*'{FirstMessageType}'*AddUseCases*";
}

public sealed record RegistryFirstMessage : IIntegrationMessage, IRequest
{
    public static string MessageType => RegistryTestData.FirstMessageType;
}

public sealed record RegistrySameNameMessage : IIntegrationMessage, IRequest
{
    public static string MessageType => RegistryTestData.FirstMessageType;
}
