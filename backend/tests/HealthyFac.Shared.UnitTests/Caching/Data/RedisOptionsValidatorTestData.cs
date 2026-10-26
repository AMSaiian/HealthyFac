using HealthyFac.Shared.Infrastructure.Caching;

namespace HealthyFac.Shared.UnitTests.Caching.Data;

public static class RedisOptionsValidatorTestData
{
    private const string ValidConnectionString = "localhost:6379,password=x";

    public static TheoryData<string> ValidConnectionStrings => new()
    {
        ValidConnectionString,
        "redis:6379",
    };

    public static TheoryData<string> BlankConnectionStrings => new()
    {
        null!,
        string.Empty,
        "   ",
    };

    public static TheoryData<RedisOptions> ValidTimeouts => new()
    {
        new RedisOptions { ConnectionString = ValidConnectionString },
        new RedisOptions
        {
            ConnectionString = ValidConnectionString,
            ConnectTimeoutMilliseconds = RedisConstants.MinTimeoutMilliseconds,
            OperationTimeoutMilliseconds = RedisConstants.MaxTimeoutMilliseconds,
        },
    };

    public static TheoryData<RedisOptions, string> InvalidTimeoutsWithExpectedError => new()
    {
        {
            new RedisOptions { ConnectionString = ValidConnectionString, ConnectTimeoutMilliseconds = 0 },
            RedisValidationMessages.ConnectTimeoutOutOfRange
        },
        {
            new RedisOptions { ConnectionString = ValidConnectionString, OperationTimeoutMilliseconds = 30001 },
            RedisValidationMessages.OperationTimeoutOutOfRange
        },
    };
}
