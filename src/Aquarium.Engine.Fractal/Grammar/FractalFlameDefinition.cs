using System.Numerics;

namespace Aquarium.Engine.Fractal.Grammar;

public readonly record struct FractalFlameVariationWeights(
    float Linear,
    float Spherical,
    float Bubble)
{
    public bool HasAny => Linear != 0.0f || Spherical != 0.0f || Bubble != 0.0f;

    public Vector2 Apply(Vector2 point)
    {
        var result = Vector2.Zero;
        if (Linear != 0.0f)
        {
            result += point * Linear;
        }

        var radiusSquared = Vector2.Dot(point, point);
        if (Spherical != 0.0f)
        {
            result += point * (Spherical / MathF.Max(radiusSquared, 0.000001f));
        }

        if (Bubble != 0.0f)
        {
            result += point * (Bubble * 4.0f / (radiusSquared + 4.0f));
        }

        return result;
    }
}

public readonly record struct FractalFlameTransform2D(
    string Name,
    Vector4 Matrix,
    Vector2 Translation,
    float Weight,
    float Color,
    FractalFlameVariationWeights Variations)
{
    public Vector2 Apply(Vector2 point)
    {
        var affine = new Vector2(
            (Matrix.X * point.X) + (Matrix.Y * point.Y) + Translation.X,
            (Matrix.Z * point.X) + (Matrix.W * point.Y) + Translation.Y);
        return Variations.HasAny ? Variations.Apply(affine) : affine;
    }
}

public sealed class FractalFlameDefinition
{
    public FractalFlameDefinition(
        string name,
        Vector2 start,
        int seed,
        IReadOnlyList<FractalFlameTransform2D> transforms)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Flame name must not be empty.", nameof(name));
        }

        if (transforms.Count == 0)
        {
            throw new ArgumentException("Flame definitions need at least one transform.", nameof(transforms));
        }

        Name = name;
        Start = start;
        Seed = seed;
        Transforms = transforms;
    }

    public string Name { get; }

    public Vector2 Start { get; }

    public int Seed { get; }

    public IReadOnlyList<FractalFlameTransform2D> Transforms { get; }
}

