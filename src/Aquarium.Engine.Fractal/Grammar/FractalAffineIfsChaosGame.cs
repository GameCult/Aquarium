using System.Numerics;

namespace Aquarium.Engine.Fractal.Grammar;

public static class FractalAffineIfsChaosGame
{
    public static Vector2[] Generate(
        FractalAffineIfsDefinition definition,
        int count,
        int burnIn,
        IFractalRandom random)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfNegative(burnIn);

        var points = new Vector2[count];
        var point = definition.Start;
        var total = definition.Transforms.Sum(transform => MathF.Max(transform.Weight, 0.0f));
        if (total <= 0.0f)
        {
            throw new ArgumentException("Affine IFS transform weights must have positive total.", nameof(definition));
        }

        for (var step = 0; step < burnIn + count; step++)
        {
            point = SelectTransform(definition.Transforms, total, (float)random.NextDouble()).Apply(point);
            if (step >= burnIn)
            {
                points[step - burnIn] = point;
            }
        }

        return points;
    }

    private static FractalAffineIfsTransform2D SelectTransform(
        IReadOnlyList<FractalAffineIfsTransform2D> transforms,
        float total,
        float unit)
    {
        var target = unit * total;
        var cumulative = 0.0f;
        for (var index = 0; index < transforms.Count; index++)
        {
            cumulative += MathF.Max(transforms[index].Weight, 0.0f);
            if (target <= cumulative)
            {
                return transforms[index];
            }
        }

        return transforms[^1];
    }
}

