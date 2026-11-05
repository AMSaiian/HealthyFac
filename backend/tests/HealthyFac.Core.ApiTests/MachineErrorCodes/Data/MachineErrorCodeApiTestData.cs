namespace HealthyFac.Core.ApiTests.MachineErrorCodes.Data;

public static class MachineErrorCodeApiTestData
{
    public const string MachinesPath = "v1/machines";
    public const string ErrorCodesSegment = "error-codes";
    public const string SearchSegment = "search";
    public const string ReadPermission = "machine-error-codes:read";
    public const string WritePermission = "machine-error-codes:write";
    public const string DeletePermission = "machine-error-codes:delete";
    public const string InvalidEncryptedId = "not-an-encrypted-id";
    public const string SerialNumberPrefix = "ECAPI-";
    public const string Code = "E-100";
    public const string UpdatedCode = "E-200";
    public const string SearchEmptyBody = "{\"offset\":0,\"limit\":25,\"filter\":{}}";
}
