using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.UnitTests.UseCases.Data;

namespace HealthyFac.Shared.UnitTests.Messaging.Data;

public static class IntegrationMessageValidationTestData
{
    public const string BlankName = "";
    public const string MessageType = "test.v1:ValidatedMessage";

    public static readonly Type[] Types =
    [
        typeof(ValidatedMessage),
        typeof(ValidatedMessagePreHandlerCheck),
        typeof(ValidatedMessageHandler),
    ];
}

public sealed record ValidatedMessage : IIntegrationMessage, IRequest
{
    public static string MessageType => IntegrationMessageValidationTestData.MessageType;

    public required string Name { get; init; }
}

public sealed class ValidatedMessageValidator : AbstractValidator<ValidatedMessage>
{
    public ValidatedMessageValidator()
    {
        RuleFor(message => message.Name).NotEmpty();
    }
}

public sealed class ValidatedMessagePreHandlerCheck : PassThroughPreHandlerCheck<ValidatedMessage>;

public sealed class ValidatedMessageHandler(HandlerProbe probe) : RequestHandler<ValidatedMessage>
{
    private readonly HandlerProbe _probe = probe;

    protected override Task HandleInternalAsync(ValidatedMessage request, CancellationToken cancellationToken)
    {
        _probe.Record();

        return Task.CompletedTask;
    }
}
