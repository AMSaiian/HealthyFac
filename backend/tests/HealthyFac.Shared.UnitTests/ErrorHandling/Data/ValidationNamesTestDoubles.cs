using System.Text.Json.Serialization;
using FluentValidation;

namespace HealthyFac.Shared.UnitTests.ErrorHandling.Data;

public sealed record NamedRequest
{
    public string SerialNumber { get; init; } = string.Empty;

    [JsonPropertyName(name: "custom_label")]
    public string Label { get; init; } = "label";

    public NamedChild? Child { get; init; }

    public List<NamedChild> Children { get; init; } = [];
}

public sealed record NamedChild
{
    public string FirstName { get; init; } = string.Empty;
}

public sealed class NamedRequestValidator : AbstractValidator<NamedRequest>
{
    public NamedRequestValidator()
    {
        RuleFor(request => request.SerialNumber).NotEmpty();

        RuleFor(request => request.Label).NotEmpty();

        RuleFor(request => request.Child!.FirstName)
            .NotEmpty()
            .When(request => request.Child is not null);

        RuleForEach(request => request.Children).SetValidator(new NamedChildValidator());
    }
}

public sealed class NamedChildValidator : AbstractValidator<NamedChild>
{
    public NamedChildValidator()
    {
        RuleFor(child => child.FirstName).NotEmpty();
    }
}
