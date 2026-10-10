namespace HealthyFac.Shared.UnitTests.Logging.Data;

public static class DestructuringTestData
{
    public static TheoryData<SensitivePropertyUserDto, string, string> SensitivePropertyData => new()
    {
        {
            new SensitivePropertyUserDto
            {
                Username = "sarzur",
                Password = "SuperSecretPassword123!",
            },
            "sarzur",
            "***MASKED***"
        },
    };

    public static TheoryData<FullyMaskedPayloadDto> FullyMaskedData => new()
    {
        {
            new FullyMaskedPayloadDto
            {
                SecretA = "AlphaSecret",
                SecretB = "BetaSecret",
            }
        },
    };

    public static TheoryData<PlainUserDto> PlainUserData => new()
    {
        {
            new PlainUserDto
            {
                Name = "John Doe",
                Age = 30,
            }
        },
    };

    public static TheoryData<object> SimpleScalarData => new()
    {
        { "a simple string" },
        { 42 },
        { DateTime.UtcNow },
        { Guid.CreateVersion7() },
    };

    public static TheoryData<SensitiveCollectionDto> SensitiveCollectionData => new()
    {
        {
            new SensitiveCollectionDto
            {
                Name = "SessionData",
                SecretTokens = ["token_alpha", "token_beta"],
            }
        },
    };

    public static TheoryData<SensitiveCreditCardCollectionDto, IReadOnlyList<string>>
        SensitiveCreditCardCollectionData => new()
    {
        {
            new SensitiveCreditCardCollectionDto
            {
                AccountHolder = "John Doe",
                Cards = ["4111222233334444", "5555666677778888"],
            },
            ["***MASKED***4444", "***MASKED***8888"]
        },
    };

    public static TheoryData<SensitiveDictionaryDto> SensitiveDictionaryData => new()
    {
        {
            new SensitiveDictionaryDto
            {
                Context = "SecurityHeaders",
                Headers = new Dictionary<string, string>
                {
                    ["Authorization"] = "Bearer SecretToken123",
                    ["X-Api-Key"] = "SuperSecretKey456",
                },
            }
        },
    };
}
