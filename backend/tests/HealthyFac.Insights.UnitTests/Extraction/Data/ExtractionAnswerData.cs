namespace HealthyFac.Insights.UnitTests.Extraction.Data;

public static class ExtractionAnswerData
{
    public static TheoryData<string> InvalidAnswers => new()
    {
        { ExtractionTestData.NotJsonAnswer },
        { ExtractionTestData.UnknownEnumAnswer },
        { ExtractionTestData.ExtraPropertyAnswer },
    };
}
