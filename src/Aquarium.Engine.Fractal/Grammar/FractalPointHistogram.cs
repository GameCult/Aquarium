using System.Numerics;

namespace Aquarium.Engine.Fractal.Grammar;

public readonly record struct FractalPointHistogram(
    int Width,
    int Height,
    Vector4 Bounds,
    int[] Bins)
{
    public int HitCount => Bins.Sum();

    public int this[int x, int y] => Bins[y * Width + x];
}

public static class FractalPointHistogramBuilder
{
    public static FractalPointHistogram Build(
        IReadOnlyList<Vector2> points,
        int width,
        int height,
        Vector4 bounds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var bins = new int[width * height];
        var span = new Vector2(bounds.Z - bounds.X, bounds.W - bounds.Y);
        foreach (var point in points)
        {
            var u = (point.X - bounds.X) / MathF.Max(span.X, 0.000001f);
            var v = (point.Y - bounds.Y) / MathF.Max(span.Y, 0.000001f);
            if (u < 0.0f || u >= 1.0f || v < 0.0f || v >= 1.0f)
            {
                continue;
            }

            var x = Math.Clamp((int)(u * width), 0, width - 1);
            var y = Math.Clamp((int)(v * height), 0, height - 1);
            bins[y * width + x]++;
        }

        return new FractalPointHistogram(width, height, bounds, bins);
    }
}

