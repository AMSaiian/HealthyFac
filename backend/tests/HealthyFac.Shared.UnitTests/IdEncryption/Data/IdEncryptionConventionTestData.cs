namespace HealthyFac.Shared.UnitTests.IdEncryption.Data;

public static class IdEncryptionConventionTestData
{
    public static readonly byte[] PrimaryKey = AesKwIdEncryptorTestData.PrimaryKey;

    public static readonly Guid SampleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SampleMachineId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SampleTenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid SampleIgnoredId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid SampleCorrelationToken = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public const int SampleStepId = 42;

    public const string IdPropertyName = "id";
    public const string MachineIdPropertyName = "machineId";
    public const string TenantIdPropertyName = "tenantId";
    public const string IgnoredIdPropertyName = "ignoredId";
    public const string CorrelationTokenPropertyName = "correlationToken";
    public const string StepIdPropertyName = "stepId";

    public static TheoryData<ConventionTestModel> ModelsWithTenantId =>
    [
        new ConventionTestModel
        {
            Id = SampleId,
            MachineId = SampleMachineId,
            TenantId = SampleTenantId,
            IgnoredId = SampleIgnoredId,
            CorrelationToken = SampleCorrelationToken,
            StepId = SampleStepId,
        },
    ];
}
