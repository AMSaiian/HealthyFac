using System.Security.Cryptography;

namespace HealthyFac.Shared.Domain.Gateways;

/// <summary>
/// Defines the gateway ID: the public, immutable identifier of a machine in MQTT messages. Core generates it, and
/// Telemetry checks the topic with the same format.
/// </summary>
public static class GatewayIds
{
    public const int Length = 16;

    public const string Alphabet = "abcdefghijklmnopqrstuvwxyz234567";

    /// <summary>
    /// Creates a random gateway ID with a cryptographic random generator, so a device cannot guess the ID of another
    /// machine.
    /// </summary>
    /// <returns>A new gateway ID of <see cref="Length"/> characters from <see cref="Alphabet"/>.</returns>
    public static string Create() => RandomNumberGenerator.GetString(Alphabet, Length);

    /// <summary>
    /// Checks the length and the alphabet. It never throws.
    /// </summary>
    /// <param name="value">The text to check, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> has the gateway ID format.</returns>
    public static bool IsValid(string? value) =>
        value is { Length: Length } && value.AsSpan().IndexOfAnyExcept(Alphabet) < 0;
}
