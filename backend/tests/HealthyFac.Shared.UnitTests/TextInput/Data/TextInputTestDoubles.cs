using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Shared.UnitTests.TextInput.Data;

public sealed record TextInputRequest
{
    public string? Name { get; init; }

    [MultilineText]
    public string? Notes { get; init; }

    [RawText]
    public string? Token { get; init; }

    public List<string>? Tags { get; init; }

    public TextInputAddress? Address { get; init; }

    public List<TextInputAddress>? Addresses { get; init; }
}

public sealed record TextInputAddress
{
    public string? Street { get; init; }
}

/// <summary>
/// A mutable node, so a test can close a reference cycle.
/// </summary>
public sealed class TextInputNode
{
    public string? Label { get; set; }

    public TextInputNode? Next { get; set; }
}
