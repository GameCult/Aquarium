using System.Numerics;

namespace Aquarium.Engine.Fractal.Grammar;

public readonly record struct FractalFlameVariationWeights(
    float Linear,
    float Spherical,
    float Bubble,
    float Disc = 0.0f,
    float Julian = 0.0f,
    float JulianPower = 2.0f,
    float JulianDist = 1.0f,
    float GaussianBlur = 0.0f)
{
    public bool HasAny =>
        Linear != 0.0f ||
        Spherical != 0.0f ||
        Bubble != 0.0f ||
        Disc != 0.0f ||
        Julian != 0.0f ||
        GaussianBlur != 0.0f;

    public Vector2 Apply(Vector2 point, IFractalRandom? random = null)
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

        if (Disc != 0.0f)
        {
            var radius = MathF.Sqrt(radiusSquared);
            var theta = MathF.Atan2(point.Y, point.X);
            var scaledTheta = Disc * theta / MathF.PI;
            result += new Vector2(
                MathF.Sin(MathF.PI * radius) * scaledTheta,
                MathF.Cos(MathF.PI * radius) * scaledTheta);
        }

        if (Julian != 0.0f)
        {
            var power = MathF.Abs(JulianPower) < 1.0f ? 1.0f : JulianPower;
            var absPower = MathF.Max(MathF.Abs(power), 1.0f);
            var branch = MathF.Floor(NextUnit(random) * absPower);
            var angle = (MathF.Atan2(point.Y, point.X) + (2.0f * MathF.PI * branch)) / power;
            var radius = MathF.Pow(MathF.Max(MathF.Sqrt(radiusSquared), 0.000001f), JulianDist / power);
            result += new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (Julian * radius);
        }

        if (GaussianBlur != 0.0f)
        {
            var amount = GaussianBlur * (NextUnit(random) + NextUnit(random) + NextUnit(random) + NextUnit(random) - 2.0f);
            var angle = NextUnit(random) * 2.0f * MathF.PI;
            result += new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * amount;
        }

        return result;
    }

    private static float NextUnit(IFractalRandom? random)
    {
        return random is null ? 0.5f : (float)random.NextDouble();
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
        return Apply(point, random: null);
    }

    public Vector2 Apply(Vector2 point, IFractalRandom? random)
    {
        var affine = new Vector2(
            (Matrix.X * point.X) + (Matrix.Y * point.Y) + Translation.X,
            (Matrix.Z * point.X) + (Matrix.W * point.Y) + Translation.Y);
        return Variations.HasAny ? Variations.Apply(affine, random) : affine;
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
