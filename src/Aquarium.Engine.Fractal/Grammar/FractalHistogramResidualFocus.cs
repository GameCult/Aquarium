using System.Numerics;

namespace Aquarium.Engine.Fractal.Grammar;

public static class FractalHistogramResidualFocus
{
    public static Vector4 Estimate(FractalPointHistogram reference, FractalPointHistogram candidate)
    {
        if (reference.Width != candidate.Width || reference.Height != candidate.Height)
        {
            throw new ArgumentException("Histogram dimensions must match.", nameof(candidate));
        }

        if (reference.Bins.Length != candidate.Bins.Length)
        {
            throw new ArgumentException("Histogram bin counts must match.", nameof(candidate));
        }

        var referenceTotal = Math.Max(reference.HitCount, 1);
        var candidateTotal = Math.Max(candidate.HitCount, 1);
        var span = new Vector2(reference.Bounds.Z - reference.Bounds.X, reference.Bounds.W - reference.Bounds.Y);
        var weighted = Vector2.Zero;
        var mass = 0.0f;

        for (var y = 0; y < reference.Height; y++)
        {
            for (var x = 0; x < reference.Width; x++)
            {
                var index = (y * reference.Width) + x;
                var delta = (reference.Bins[index] / (float)referenceTotal) - (candidate.Bins[index] / (float)candidateTotal);
                if (delta <= 0.0f)
                {
                    continue;
                }

                var world = BinCenter(reference, span, x, y);
                weighted += world * delta;
                mass += delta;
            }
        }

        if (mass <= 0.0f)
        {
            return Vector4.Zero;
        }

        var center = weighted / mass;
        var variance = 0.0f;
        for (var y = 0; y < reference.Height; y++)
        {
            for (var x = 0; x < reference.Width; x++)
            {
                var index = (y * reference.Width) + x;
                var delta = (reference.Bins[index] / (float)referenceTotal) - (candidate.Bins[index] / (float)candidateTotal);
                if (delta <= 0.0f)
                {
                    continue;
                }

                var world = BinCenter(reference, span, x, y);
                variance += Vector2.DistanceSquared(world, center) * delta;
            }
        }

        var minRadius = MathF.Max(span.X / MathF.Max(reference.Width, 1), span.Y / MathF.Max(reference.Height, 1)) * 1.5f;
        var maxRadius = MathF.Max(span.X, span.Y) * 0.75f;
        var radius = Math.Clamp(MathF.Sqrt(variance / mass) * 2.0f, minRadius, maxRadius);
        var strength = Math.Clamp(mass * 3.0f, 0.05f, 1.0f);
        return new Vector4(center.X, center.Y, radius, strength);
    }

    private static Vector2 BinCenter(FractalPointHistogram histogram, Vector2 span, int x, int y)
    {
        return new Vector2(
            histogram.Bounds.X + ((x + 0.5f) / histogram.Width) * span.X,
            histogram.Bounds.Y + ((y + 0.5f) / histogram.Height) * span.Y);
    }
}
