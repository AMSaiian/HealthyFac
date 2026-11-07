using HealthyFac.Telemetry.Domain.Intake;

namespace HealthyFac.Telemetry.UnitTests.Intake.Data;

public static class ReceivedMessageTestData
{
    public static readonly DateTime ReceivedAt = new(
        year: 2026, month: 11, day: 7, hour: 9, minute: 0, second: 0, DateTimeKind.Utc);

    public static ReceivedMessage Create(byte[]? rawPayload, string? payload) =>
        new(
            machineRegistrationId: null,
            topic: "healthyfac/gateways/abcdefghijklmnop/messages",
            sampledAt: null,
            ReceivedAt,
            schemaRevision: null,
            rawPayload,
            payload,
            []);

    public static TheoryData<byte[]?, string?> InvalidPayloadForms => new()
    {
        { null, null },
        { new byte[] { 0x7B, 0x7D }, "{}" },
    };

    public static TheoryData<byte[]?, string?> ValidPayloadForms => new()
    {
        { new byte[] { 0xFF }, null },
        { null, "{}" },
    };
}
