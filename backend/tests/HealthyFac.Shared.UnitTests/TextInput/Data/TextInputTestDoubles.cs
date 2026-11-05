using FluentValidation;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Shared.UnitTests.TextInput.Data;

public sealed record TextInputRequest
{
    public string? Name { get; init; }

    public string? Notes { get; init; }

    public string? Token { get; init; }

    public List<string>? Tags { get; init; }

    public TextInputAddress? Address { get; init; }

    public List<TextInputAddress>? Addresses { get; init; }
}

public sealed record TextInputAddress
{
    public string? Street { get; init; }
}

public sealed class TextInputAddressValidator : AbstractValidator<TextInputAddress>
{
    public TextInputAddressValidator()
    {
        RuleFor(address => address.Street).SingleLineText();
    }
}

public sealed class TextInputRequestValidator : AbstractValidator<TextInputRequest>
{
    public TextInputRequestValidator()
    {
        RuleFor(request => request.Name).SingleLineText();

        RuleFor(request => request.Notes).MultilineText();

        RuleFor(request => request.Token).SingleLineText();

        RuleForEach(request => request.Tags).SingleLineText();

        RuleFor(request => request.Address!)
            .SetValidator(new TextInputAddressValidator())
            .When(request => request.Address is not null);

        RuleForEach(request => request.Addresses)
            .SetValidator(new TextInputAddressValidator());
    }
}
