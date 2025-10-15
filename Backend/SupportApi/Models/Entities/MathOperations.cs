namespace SupportApi.Models.Entities;

public static class MathOperations
{
    public static double CosineSimilarity(double[] v1, double[] v2)
    {
        double dot = 0, mag1 = 0, mag2 = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            dot += v1[i] * v2[i];
            mag1 += v1[i] * v1[i];
            mag2 += v2[i] * v2[i];
        }
        return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
    }
}