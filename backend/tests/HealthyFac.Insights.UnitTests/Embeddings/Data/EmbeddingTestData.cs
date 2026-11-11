namespace HealthyFac.Insights.UnitTests.Embeddings.Data;

public static class EmbeddingTestData
{
    public const int Dimensions = 1536;
    public const int Parallelism = 4;
    public const float NormTolerance = 1e-5f;

    public static IReadOnlyList<string> Texts(int count) =>
        Enumerable.Range(start: 0, count).Select(index => index.ToString()).ToList();
}
