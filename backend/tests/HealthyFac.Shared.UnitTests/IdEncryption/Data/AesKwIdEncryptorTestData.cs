namespace HealthyFac.Shared.UnitTests.IdEncryption.Data;

public static class AesKwIdEncryptorTestData
{
    public const string VersionPrefix = "v1.";
    public const int VersionPrefixLength = 3;

    public static readonly byte[] PrimaryKey =
    [
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
    ];

    public static TheoryData<Guid> SampleGuidsData =>
    [
        Guid.CreateVersion7(),
        new Guid(
            [
                0x01, 0x92, 0x34, 0x56, 0x78, 0x9A, 0x7B, 0xC0,
                0x81, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF,
            ],
            bigEndian: true
        ),

        Guid.Empty,
    ];

    public static TheoryData<int> InvalidKeyLengthsData =>
    [
        16,
        24,
        48,
    ];

    public static TheoryData<int> TamperedByteIndicesData =>
    [
        0,
        12,
        23,
    ];

    public static TheoryData<string> InvalidBase64UrlPayloadsData =>
    [
        "v1.!!!NotValidBase64Url$$$",
        "v1.invalid==padding",
        "v1.with spaces in token",
        "v1.tooShort",
    ];

    public static TheoryData<string> InvalidVersionPrefixedTokensData =>
    [
        "v2.64E8C3F9CE0F5BA263E9777905818A2A93C8191E7D6E8AE7",
        "v0.64E8C3F9CE0F5BA263E9777905818A2A93C8191E7D6E8AE7",
        "64E8C3F9CE0F5BA263E9777905818A2A93C8191E7D6E8AE7",
        "v164E8C3F9CE0F5BA263E9777905818A2A93C8191E7D6E8AE7",
    ];

    public static TheoryData<string> EmptyOrWhitespaceStringsData =>
    [
        string.Empty,
        "   ",
        "\t",
    ];

    public static TheoryData<string> WrongLengthBase64UrlPayloadsData =>
    [
        "v1.AQID",
        "v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
    ];

    internal static readonly Rfc3394TestVector Rfc3394Section43Vector = new()
    {
        Key =
        [
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
            0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
        ],
        PlaintextGuid = new Guid(
            [
                0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,
            ],
            bigEndian: true
        ),
        ExpectedCiphertext =
        [
            0x64, 0xE8, 0xC3, 0xF9, 0xCE, 0x0F, 0x5B, 0xA2,
            0x63, 0xE9, 0x77, 0x79, 0x05, 0x81, 0x8A, 0x2A,
            0x93, 0xC8, 0x19, 0x1E, 0x7D, 0x6E, 0x8A, 0xE7,
        ],
    };

    public static TheoryData<byte[], Guid, byte[]> Rfc3394TestVectorsData => new()
    {
        {
            Rfc3394Section43Vector.Key,
            Rfc3394Section43Vector.PlaintextGuid,
            Rfc3394Section43Vector.ExpectedCiphertext
        },
    };
}
