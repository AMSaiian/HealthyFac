namespace HealthyFac.Telemetry.Application.Intake;

/// <summary>
/// Reads the raw payload of a machine message and checks it against the stored schema of its machine. Infrastructure
/// implements it, so only that layer references the schema library.
/// </summary>
public interface IMachineMessageParser
{
    /// <summary>
    /// Decodes a raw payload and tests that it is storable JSON.
    /// </summary>
    /// <param name="payload">The raw bytes of the message.</param>
    /// <returns>The text of a readable payload, or the error code of an unreadable one (malformed or unstorable
    /// JSON).</returns>
    PayloadReading ReadPayload(byte[] payload);

    /// <summary>
    /// Checks one readable payload.
    /// </summary>
    /// <param name="schemaJson">The stored schema of the machine.</param>
    /// <param name="payloadText">The text from <see cref="ReadPayload"/>.</param>
    /// <param name="receivedAt">The moment of receipt, as a UTC instant.</param>
    /// <returns>The errors and the valid values of the message.</returns>
    ParsedMachineMessage Parse(string schemaJson, string payloadText, DateTime receivedAt);
}
