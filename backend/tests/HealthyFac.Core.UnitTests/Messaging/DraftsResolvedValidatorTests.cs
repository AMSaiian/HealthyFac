using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Core.UnitTests.Messaging.Data;

namespace HealthyFac.Core.UnitTests.Messaging;

public sealed class DraftsResolvedValidatorTests
{
    private static readonly Guid Id = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000d02");

    [Theory]
    [MemberData(nameof(DraftsResolvedTestData.Entries), MemberType = typeof(DraftsResolvedTestData))]
    public void Validate_WithEntry_ChecksTheTargetAgainstTheResolution(
        string caseName,
        DraftResolution resolution,
        ResolvedEntry entry,
        bool isValid
    )
    {
        var message = new DraftsResolved
        {
            AttemptId = Id,
            CompanyId = Id,
            Resolution = resolution,
            Entries = [entry],
        };

        var result = new DraftsResolvedValidator().Validate(message);

        result.IsValid.Should().Be(isValid, caseName);
    }
}
