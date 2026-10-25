using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using HealthyFac.Core.Application.Employees.InviteTokens;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.Infrastructure.Employees.InviteTokens;

internal sealed class HmacInviteTokenService : IInviteTokenService
{
    private readonly InviteTokenOptions _options;
    private readonly Dictionary<string, byte[]> _secretsByKeyId;
    private readonly string _defaultKeyId;

    public HmacInviteTokenService(IOptions<InviteTokenOptions> options)
    {
        _options = options.Value;
        _secretsByKeyId = _options.Keys.ToDictionary(
            key => key.Id,
            key => Convert.FromBase64String(key.Secret),
            StringComparer.Ordinal);
        _defaultKeyId = _options.Keys.Single(key => key.IsDefault).Id;
    }

    public byte[] CreateSalt() => RandomNumberGenerator.GetBytes(InviteTokenConstants.Lengths.SaltBytes);

    public DateTime GetExpiresAt(DateTime issuedAt) => issuedAt.AddHours(_options.LifetimeHours);

    public string CreateToken(Guid inviteId, byte[] tokenSalt) =>
        string.Join(
            InviteTokenConstants.Format.Separator,
            inviteId.ToString(InviteTokenConstants.Format.InviteIdFormat),
            _defaultKeyId,
            Sign(_secretsByKeyId[_defaultKeyId], inviteId, tokenSalt));

    public bool TryReadInviteId(string token, out Guid inviteId)
    {
        inviteId = Guid.Empty;
        var parts = token.Split(InviteTokenConstants.Format.Separator);

        return parts.Length == InviteTokenConstants.Format.PartCount
            && Guid.TryParseExact(parts[0], InviteTokenConstants.Format.InviteIdFormat, out inviteId);
    }

    public bool IsValid(string token, byte[] tokenSalt)
    {
        if (!TryReadInviteId(token, out var inviteId))
        {
            return false;
        }

        var parts = token.Split(InviteTokenConstants.Format.Separator);

        return _secretsByKeyId.TryGetValue(parts[1], out var secret)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(parts[2]),
                Encoding.UTF8.GetBytes(Sign(secret, inviteId, tokenSalt)));
    }

    private static string Sign(byte[] secret, Guid inviteId, byte[] tokenSalt) =>
        Base64Url.EncodeToString(HMACSHA256.HashData(secret, inviteId.ToByteArray().Concat(tokenSalt).ToArray()));
}
