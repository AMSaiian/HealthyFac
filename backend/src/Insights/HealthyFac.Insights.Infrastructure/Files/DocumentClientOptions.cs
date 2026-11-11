using FluentValidation;
using HealthyFac.Shared.Application.Configuration;

namespace HealthyFac.Insights.Infrastructure.Files;

public sealed class DocumentClientOptions
{
    public const string SectionName = "DocumentClient";

    /// <summary>The address of the gRPC port of Document.</summary>
    public required string GrpcAddress { get; init; }
}

public sealed class DocumentClientOptionsValidator : AbstractValidator<DocumentClientOptions>
{
    public DocumentClientOptionsValidator()
    {
        RuleFor(options => options.GrpcAddress)
            .MustBeAbsoluteHttpUri()
            .WithMessage(DocumentClientMessages.GrpcAddressInvalid);
    }
}
