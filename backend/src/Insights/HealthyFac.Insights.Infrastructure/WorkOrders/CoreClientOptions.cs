using FluentValidation;
using HealthyFac.Shared.Application.Configuration;

namespace HealthyFac.Insights.Infrastructure.WorkOrders;

public sealed class CoreClientOptions
{
    public const string SectionName = "CoreClient";

    /// <summary>The address of the HTTP port of Core.</summary>
    public required string BaseAddress { get; init; }
}

public sealed class CoreClientOptionsValidator : AbstractValidator<CoreClientOptions>
{
    public CoreClientOptionsValidator()
    {
        RuleFor(options => options.BaseAddress)
            .MustBeAbsoluteHttpUri()
            .WithMessage(CoreClientMessages.BaseAddressInvalid);
    }
}
