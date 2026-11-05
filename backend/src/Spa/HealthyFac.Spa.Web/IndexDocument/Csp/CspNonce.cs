using System.Security.Cryptography;

namespace HealthyFac.Spa.Web.IndexDocument;

public static class CspNonce
{
    private const int ByteCount = 16;

    /// <summary>
    /// Creates a nonce for one response. A nonce must never repeat, so every response gets a new one.
    /// </summary>
    /// <returns>16 random bytes in Base64.</returns>
    public static string Create() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(ByteCount));
}
