namespace HealthyFac.Insights.Application.Embeddings;

/// <summary>
/// Scales embedding vectors. A vector of length 1 makes the cosine distance equal to a dot product, and the model
/// returns a normalized vector only for its full size.
/// </summary>
public static class VectorNormalization
{
    private const string ZeroVectorMessage = "An embedding with only zero values cannot scale to length 1.";

    /// <summary>
    /// Scales a vector to the Euclidean length 1.
    /// </summary>
    /// <param name="vector">The vector to scale. The method does not change it.</param>
    /// <returns>A new vector of length 1.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when every value of <paramref name="vector"/> is 0.
    /// </exception>
    public static float[] ToUnitLength(ReadOnlySpan<float> vector)
    {
        double sumOfSquares = 0;
        foreach (var value in vector)
        {
            sumOfSquares += (double)value * value;
        }

        if (sumOfSquares == 0)
        {
            throw new InvalidOperationException(ZeroVectorMessage);
        }

        var length = Math.Sqrt(sumOfSquares);
        var scaled = new float[vector.Length];
        for (var index = 0; index < vector.Length; index++)
        {
            scaled[index] = (float)(vector[index] / length);
        }

        return scaled;
    }
}
