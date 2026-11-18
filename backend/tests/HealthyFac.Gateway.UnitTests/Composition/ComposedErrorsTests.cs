using System.Text.Json.Nodes;
using HealthyFac.Gateway.UnitTests.Composition.Data;
using HealthyFac.Gateway.Web.Composition;

namespace HealthyFac.Gateway.UnitTests.Composition;

public sealed class ComposedErrorsTests
{
    [Theory]
    [MemberData(nameof(ComposedErrorsTestData.Failures), MemberType = typeof(ComposedErrorsTestData))]
    public void Combine_WithAFailure_AnswersOneStatusAndBody(
        string caseName,
        DownstreamResult[] results,
        int expectedStatus,
        string expectedTitle
    )
    {
        var outcome = ComposedErrors.Combine(results);

        var failure = outcome.Should().BeOfType<ComposedOutcome.Failure>().Subject;

        failure.Status.Should().Be(expectedStatus, caseName);
        failure.Problem[propertyName: "title"]!.GetValue<string>().Should().Be(expectedTitle, caseName);
    }

    [Fact]
    public void Combine_WithTwoSuccesses_ReturnsBothBodiesInTheOrderOfTheCalls()
    {
        var core = new JsonObject { [propertyName: "source"] = "core" };
        var telemetry = new JsonObject { [propertyName: "source"] = "telemetry" };

        var outcome = ComposedErrors.Combine(
            [new DownstreamResult.Success { Body = core }, new DownstreamResult.Success { Body = telemetry }]);

        outcome.Should().BeOfType<ComposedOutcome.Success>().Which.Bodies.Should().Equal(core, telemetry);
    }

    [Fact]
    public void Combine_WithTwoValidationErrors_MergesTheMessagesOfEachFieldWithoutDuplicates()
    {
        var core = ComposedErrorsTestData.Error(
            status: 400,
            ComposedErrorsTestData.CoreTitle,
            errorsJson: """{"limit":["too big"],"after":["bad position"]}""");
        var telemetry = ComposedErrorsTestData.Error(
            status: 400,
            ComposedErrorsTestData.TelemetryTitle,
            errorsJson: """{"limit":["too big","not a number"],"filter.kinds":["unknown"]}""");

        var outcome = ComposedErrors.Combine([core, telemetry]);

        var failure = outcome.Should().BeOfType<ComposedOutcome.Failure>().Subject;

        failure.Status.Should().Be(expected: 400);
        JsonNode.DeepEquals(
                failure.Problem[propertyName: "errors"],
                JsonNode.Parse(
                    json:
                    """{"limit":["too big","not a number"],"after":["bad position"],"filter.kinds":["unknown"]}"""))
            .Should()
            .BeTrue();
    }
}
