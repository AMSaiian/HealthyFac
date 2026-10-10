using HealthyFac.Shared.Application.Logging;

namespace HealthyFac.Shared.UnitTests.Logging.Data;

public sealed record PlainUserDto
{
    public required string Name { get; init; }

    public required int Age { get; init; }
}

public sealed record SensitivePropertyUserDto
{
    public required string Username { get; init; }

    [SensitiveData]
    public required string Password { get; init; }
}

[SensitiveData]
public sealed record FullyMaskedPayloadDto
{
    public required string SecretA { get; init; }

    public required string SecretB { get; init; }
}

public sealed record NestedSensitiveDto
{
    public required string Title { get; init; }

    public required SensitivePropertyUserDto User { get; init; }
}

public sealed record NullableSensitiveDto
{
    [SensitiveData]
    public string? OptionalSecret { get; init; }
}

public sealed record SensitiveCollectionDto
{
    public required string Name { get; init; }

    [SensitiveData]
    public required List<string> SecretTokens { get; init; }
}

public sealed record SensitiveCreditCardCollectionDto
{
    public required string AccountHolder { get; init; }

    [SensitiveData(ShowLast = 4)]
    public required List<string> Cards { get; init; }
}

public sealed record SensitiveDictionaryDto
{
    public required string Context { get; init; }

    [SensitiveData]
    public required Dictionary<string, string> Headers { get; init; }
}
