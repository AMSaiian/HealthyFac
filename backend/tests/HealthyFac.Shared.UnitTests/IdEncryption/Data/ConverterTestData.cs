namespace HealthyFac.Shared.UnitTests.IdEncryption.Data;

public static class ConverterTestData
{
    public static readonly byte[] PrimaryKey = AesKwIdEncryptorTestData.PrimaryKey;
    public static readonly Guid SampleGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public static TheoryData<Guid> ValidGuids =>
    [
        SampleGuid,
        Guid.Empty,
        Guid.CreateVersion7(),
    ];

    public static TheoryData<Guid?> NullableGuidsWithValue =>
    [
        SampleGuid,
        Guid.Empty,
    ];

    public static TheoryData<string> MalformedTokens => new()
    {
        "not-an-encrypted-id",
        "00000000-0000-0000-0000-000000000000",
        "v1.",
        "v1.!!!not-base64!!!",
        "v9.KL01SonK1QYdAtRfguqJTWwxJBoE_J1U",
    };
}
