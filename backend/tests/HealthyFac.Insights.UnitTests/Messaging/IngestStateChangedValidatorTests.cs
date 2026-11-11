using HealthyFac.Insights.Messaging.Ingest;

namespace HealthyFac.Insights.UnitTests.Messaging;

public sealed class IngestStateChangedValidatorTests
{
    private readonly IngestStateChangedValidator _validator = new();

    [Fact]
    public void Validate_WithStateOutsideTheSet_Fails()
    {
        var result = _validator.Validate(Create(state: "running"));

        result.Errors.Select(error => error.ErrorMessage)
            .Distinct()
            .Should()
            .Equal(IngestMessageErrors.Validation.StateInvalid);
    }

    [Fact]
    public void Validate_WithEveryAllowedState_Passes()
    {
        var results = IngestMessageStates.All.Select(state => _validator.Validate(Create(state)).IsValid);

        results.Should().OnlyContain(isValid => isValid);
    }

    private static IngestStateChanged Create(string state) =>
        new()
        {
            FileId = Guid.CreateVersion7(),
            CompanyId = Guid.CreateVersion7(),
            State = state,
            FailedCount = 0,
            ChangedAt = new DateTime(year: 2026, month: 11, day: 9, hour: 9, minute: 0, second: 0, DateTimeKind.Utc),
        };
}
