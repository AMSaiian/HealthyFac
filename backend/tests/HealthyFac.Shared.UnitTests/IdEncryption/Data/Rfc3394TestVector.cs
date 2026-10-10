namespace HealthyFac.Shared.UnitTests.IdEncryption.Data;

internal sealed record Rfc3394TestVector
{
    public required byte[] Key { get; init; }
    public required Guid PlaintextGuid { get; init; }
    public required byte[] ExpectedCiphertext { get; init; }
}
