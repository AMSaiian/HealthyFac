using HealthyFac.Shared.Domain.Emails;
using HealthyFac.Shared.UnitTests.Emails.Data;

namespace HealthyFac.Shared.UnitTests.Emails;

public sealed class EmailAddressesTests
{
    [Theory]
    [MemberData(nameof(EmailAddressesTestData.Emails), MemberType = typeof(EmailAddressesTestData))]
    public void Normalize_WithEmail_TrimsAndLowerCases(string? email, string? expected)
    {
        var normalized = EmailAddresses.Normalize(email);

        normalized.Should().Be(expected);
    }
}
