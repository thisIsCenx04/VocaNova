namespace VocaNova.API.Features.Knn.BLL.Services;

public static class KnnMath
{
    public static double CosineSimilarity(double[] a, double[] b)
    {
        if (a.Length == 0 || a.Length != b.Length)
        {
            return 0.0;
        }

        var dotProduct = 0.0;
        var normA = 0.0;
        var normB = 0.0;
        for (var index = 0; index < a.Length; index++)
        {
            dotProduct += a[index] * b[index];
            normA += a[index] * a[index];
            normB += b[index] * b[index];
        }

        return normA == 0.0 || normB == 0.0
            ? 0.0
            : dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
