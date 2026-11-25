using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Core.UnitTests.Messaging.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.UnitTests.Messaging;

[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.Ingest)]
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
            Source = ResolutionSource.Ingest,
            AttemptId = Id,
            CompanyId = Id,
            Resolution = resolution,
            Entries = [entry],
        };

        var result = new DraftsResolvedValidator().Validate(message);

        result.IsValid.Should().Be(isValid, caseName);
    }

    [Theory]
    [MemberData(nameof(DraftsResolvedTestData.Sources), MemberType = typeof(DraftsResolvedTestData))]
    public void Validate_WithSourceAndAttempt_ChecksTheAttemptAgainstTheSource(
        string caseName,
        ResolutionSource source,
        Guid? attemptId,
        bool isValid
    )
    {
        var message = new DraftsResolved
        {
            Source = source,
            AttemptId = attemptId,
            CompanyId = Id,
            Resolution = DraftResolution.Rejected,
            Entries = [new ResolvedEntry { EntryId = Id }],
        };

        var result = new DraftsResolvedValidator().Validate(message);

        result.IsValid.Should().Be(isValid, caseName);
    }
}
