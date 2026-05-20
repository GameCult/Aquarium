using System.Numerics;
using Aquarium.Engine.Fractal;

namespace Aquarium.Engine.Fractal.Grammar;

public readonly record struct FractalAffineIfsTransform2D(
    string Name,
    Vector4 Matrix,
    Vector2 Translation,
    float Weight)
{
    public Vector2 Apply(Vector2 point)
    {
        return new Vector2(
            (Matrix.X * point.X) + (Matrix.Y * point.Y) + Translation.X,
            (Matrix.Z * point.X) + (Matrix.W * point.Y) + Translation.Y);
    }
}

public sealed class FractalAffineIfsDefinition
{
    public FractalAffineIfsDefinition(
        AquariumFractalKey key,
        AquariumFractalKey domainKey,
        string name,
        Vector2 start,
        int seed,
        string tags,
        IReadOnlyList<FractalAffineIfsTransform2D> transforms)
    {
        if (transforms.Count == 0)
        {
            throw new ArgumentException("Affine IFS definitions need at least one transform.", nameof(transforms));
        }

        Key = key;
        DomainKey = domainKey;
        Name = name;
        Start = start;
        Seed = seed;
        Tags = tags;
        Transforms = transforms;
    }

    public AquariumFractalKey Key { get; }

    public AquariumFractalKey DomainKey { get; }

    public string Name { get; }

    public Vector2 Start { get; }

    public int Seed { get; }

    public string Tags { get; }

    public IReadOnlyList<FractalAffineIfsTransform2D> Transforms { get; }
}

