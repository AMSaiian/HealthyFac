using FluentValidation;
using HealthyFac.Shared.Application.Configuration;

namespace HealthyFac.Insights.Infrastructure.WorkOrders;

public sealed class CoreClientOptions
{
    public const string SectionName = "CoreClient";

    /// <summary>The address of the gRPC port of Core.</summary>
    public required string GrpcAddress { get; init; }
}

public sealed class CoreClientOptionsValidator : AbstractValidator<CoreClientOptions>
{
    public CoreClientOptionsValidator()
    {
        RuleFor(options => options.GrpcAddress)
            .MustBeAbsoluteHttpUri()
            .WithMessage(CoreClientMessages.GrpcAddressInvalid);
    }
}
