using HealthyFac.Insights.Application.Embeddings;
using HealthyFac.Insights.UnitTests.Embeddings.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Embeddings;

[Feature(TestFeatures.Ingest)]
public sealed class VectorNormalizationTests
{
    [Fact]
    public void ToUnitLength_WithAnyVector_ReturnsLengthOneAndKeepsTheDirection()
    {
        float[] vector = [3f, 4f];

        var scaled = VectorNormalization.ToUnitLength(vector);

        scaled[0].Should().BeApproximately(expectedValue: 0.6f, EmbeddingTestData.NormTolerance);
        scaled[1].Should().BeApproximately(expectedValue: 0.8f, EmbeddingTestData.NormTolerance);
        vector.Should().Equal(3f, 4f);
    }

    [Fact]
    public void ToUnitLength_WithOnlyZeroValues_Throws()
    {
        var act = () => VectorNormalization.ToUnitLength(new float[EmbeddingTestData.Dimensions]);

        act.Should().Throw<InvalidOperationException>();
    }
}
