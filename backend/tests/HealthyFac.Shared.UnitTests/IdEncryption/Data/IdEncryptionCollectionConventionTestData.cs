namespace HealthyFac.Shared.UnitTests.IdEncryption.Data;

public static class IdEncryptionCollectionConventionTestData
{
    public static readonly byte[] PrimaryKey = AesKwIdEncryptorTestData.PrimaryKey;

    public static readonly Guid FirstId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SecondId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ThirdId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public const string DepartmentIdsPropertyName = "departmentIds";
    public const string MachineIdsPropertyName = "machineIds";
    public const string StepIdsPropertyName = "stepIds";
    public const string OptionalIdsPropertyName = "optionalIds";
    public const string IgnoredIdsPropertyName = "ignoredIds";
    public const string CorrelationTokensPropertyName = "correlationTokens";

    public const string NonArrayIdsJson = """{"departmentIds":"v1.not-an-array"}""";
    public const string InvalidElementJson = """{"departmentIds":["not-an-encrypted-id"]}""";

    public static TheoryData<CollectionConventionTestModel> Models =>
    [
        new CollectionConventionTestModel
        {
            DepartmentIds = [FirstId, SecondId],
            MachineIds = [ThirdId, FirstId],
            StepIds = [SecondId],
            OptionalIds = null,
            IgnoredIds = [FirstId],
            CorrelationTokens = [SecondId],
        },
    ];
}
