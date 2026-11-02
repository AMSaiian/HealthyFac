using HealthyFac.Core.Infrastructure.Employees.InviteTokens;
using HealthyFac.Core.UnitTests.Employees.InviteTokens.Data;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.UnitTests.Employees.InviteTokens;

public sealed class HmacInviteTokenServiceTests
{
    private const int SaltByteCount = 32;
    private const int KeyIdPartIndex = 1;
    private const int SignaturePartIndex = 2;
    private const string UnknownKeyId = "k9";

    private readonly HmacInviteTokenService _service = BuildService(InviteTokenTestData.CurrentKeyId);

    [Fact]
    public void CreateToken_WithSalt_IsValidWithSameSalt()
    {
        var salt = _service.CreateSalt();

        var token = _service.CreateToken(InviteTokenTestData.InviteId, salt);

        _service.IsValid(token, salt).Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithOtherSalt_ReturnsFalse()
    {
        var token = _service.CreateToken(InviteTokenTestData.InviteId, _service.CreateSalt());
        var otherSalt = _service.CreateSalt();

        var isValid = _service.IsValid(token, otherSalt);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithChangedSignatureCharacter_ReturnsFalse()
    {
        var salt = _service.CreateSalt();
        var parts = _service.CreateToken(InviteTokenTestData.InviteId, salt).Split('.');
        var signature = parts[SignaturePartIndex];
        parts[SignaturePartIndex] = (signature[0] == 'A' ? 'B' : 'A') + signature[1..];

        var isValid = _service.IsValid(string.Join('.', parts), salt);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithTokenOfOlderConfiguredKey_ReturnsTrue()
    {
        var salt = _service.CreateSalt();
        var tokenOfOlderKey = BuildService(InviteTokenTestData.OlderKeyId)
            .CreateToken(InviteTokenTestData.InviteId, salt);

        var isValid = _service.IsValid(tokenOfOlderKey, salt);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithUnknownKeyId_ReturnsFalse()
    {
        var salt = _service.CreateSalt();
        var parts = _service.CreateToken(InviteTokenTestData.InviteId, salt).Split('.');
        parts[KeyIdPartIndex] = UnknownKeyId;

        var isValid = _service.IsValid(string.Join('.', parts), salt);

        isValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(
        nameof(InviteTokenTestData.MalformedTokens),
        MemberType = typeof(InviteTokenTestData)
    )]
    public void IsValid_WithMalformedToken_ReturnsFalse(string token)
    {
        var salt = _service.CreateSalt();

        var isValid = _service.IsValid(token, salt);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void TryReadInviteId_WithValidToken_ReturnsInviteId()
    {
        var token = _service.CreateToken(InviteTokenTestData.InviteId, _service.CreateSalt());

        var isRead = _service.TryReadInviteId(token, out var inviteId);

        isRead.Should().BeTrue();
        inviteId.Should().Be(InviteTokenTestData.InviteId);
    }

    [Theory]
    [MemberData(
        nameof(InviteTokenTestData.TokensWithUnreadableInviteId),
        MemberType = typeof(InviteTokenTestData)
    )]
    public void TryReadInviteId_WithMalformedToken_ReturnsFalse(string token)
    {
        var isRead = _service.TryReadInviteId(token, out _);

        isRead.Should().BeFalse();
    }

    [Fact]
    public void CreateToken_WithDefaultKey_PutsDefaultKeyIdInSecondPart()
    {
        var salt = _service.CreateSalt();

        var token = _service.CreateToken(InviteTokenTestData.InviteId, salt);

        token.Split('.')[KeyIdPartIndex].Should().Be(InviteTokenTestData.CurrentKeyId);
    }

    [Fact]
    public void CreateSalt_TwoCalls_Returns32ByteDistinctValues()
    {
        var first = _service.CreateSalt();
        var second = _service.CreateSalt();

        first.Should().HaveCount(SaltByteCount);
        second.Should().HaveCount(SaltByteCount);
        first.Should().NotEqual(second);
    }

    [Fact]
    public void GetExpiresAt_WithIssuedAt_AddsLifetimeHours()
    {
        var issuedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var expiresAt = _service.GetExpiresAt(issuedAt);

        expiresAt.Should().Be(issuedAt.AddHours(InviteTokenTestData.LifetimeHours));
    }

    private static HmacInviteTokenService BuildService(string defaultKeyId) =>
        new(Options.Create(InviteTokenTestData.BuildTwoKeyOptions(defaultKeyId)));
}
