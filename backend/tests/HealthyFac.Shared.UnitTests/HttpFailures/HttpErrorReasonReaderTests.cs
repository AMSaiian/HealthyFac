using HealthyFac.Shared.Infrastructure.HttpFailures.ReasonReaders;
using HealthyFac.Shared.UnitTests.HttpFailures.Data;

namespace HealthyFac.Shared.UnitTests.HttpFailures;

public sealed class HttpErrorReasonReaderTests
{
    [Theory]
    [MemberData(nameof(HttpFailureTestData.BodiesWithReason), MemberType = typeof(HttpFailureTestData))]
    public void ReadReason_WithEachFormat_ReturnsReason(string json, string expected)
    {
        var reason = HttpErrorReasonReaders.Default.ReadReason(HttpFailureTestData.Parse(json));

        reason.Should().Be(expected);
    }

    [Fact]
    public void ReadReason_WithSeveralFormats_ReturnsFirstInOrder()
    {
        var reason = HttpErrorReasonReaders.Default.ReadReason(
            HttpFailureTestData.Parse(HttpFailureTestData.ProblemDetailAndOAuthBody));

        reason.Should().Be(HttpFailureTestData.ProblemDetail);
    }

    [Fact]
    public void ReadReason_WithBlankFirstReason_UsesNextReader()
    {
        var reader = new FirstReasonReader(
            new FixedReasonReader(reason: "  "),
            new FixedReasonReader(HttpFailureTestData.SecondReason));

        var reason = reader.ReadReason(HttpFailureTestData.Parse(json: "{}"));

        reason.Should().Be(HttpFailureTestData.SecondReason);
    }

    [Theory]
    [MemberData(nameof(HttpFailureTestData.BodiesWithoutReason), MemberType = typeof(HttpFailureTestData))]
    public void ReadReason_WithUnknownFields_ReturnsNull(string json)
    {
        var reason = HttpErrorReasonReaders.Default.ReadReason(HttpFailureTestData.Parse(json));

        reason.Should().BeNull();
    }

    [Fact]
    public void ReadReason_WithReasonFieldInNestedObject_ReturnsNull()
    {
        var reason = HttpErrorReasonReaders.Default.ReadReason(
            HttpFailureTestData.Parse(HttpFailureTestData.NestedReasonBody));

        reason.Should().BeNull();
    }

    [Fact]
    public void ReadReason_WithReasonFieldAtTopLevel_ReturnsReason()
    {
        var reason = HttpErrorReasonReaders.Default.ReadReason(
            HttpFailureTestData.Parse(HttpFailureTestData.TopLevelAfterNestedBody));

        reason.Should().Be(HttpFailureTestData.ProblemDetail);
    }

    [Fact]
    public void ReadReason_WithKeycloakError_ReturnsErrorMessage()
    {
        var reason = HttpErrorReasonReaders.Default.ReadReason(
            HttpFailureTestData.Parse(HttpFailureTestData.KeycloakValidationBody));

        reason.Should().Be(HttpFailureTestData.KeycloakMessage);
    }

    [Fact]
    public void ReadReason_WithKeycloakValidationError_LeavesParamsOut()
    {
        var reason = HttpErrorReasonReaders.Default.ReadReason(
            HttpFailureTestData.Parse(HttpFailureTestData.KeycloakValidationBody));

        reason.Should().NotContain(HttpFailureTestData.KeycloakParamEmail);
    }
}
