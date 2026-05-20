using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using Aquarium.Engine.Fractal.Grammar;

namespace Aquarium.Engine.Fractal.Tests;

public sealed class ApophysisReferenceParityTests
{
    [Fact]
    public void AquariumAffineIfsMatchesApophysisLinearFixtureForFixedXformSequence()
    {
        var root = FindRepoRoot();
        var flamePath = Path.Combine(root, "tests", "Aquarium.Engine.Fractal.Tests", "Fixtures", "Apophysis", "linear-sierpinski.flame");
        var aquageoPath = Path.Combine(root, "tests", "Aquarium.Engine.Fractal.Tests", "Fixtures", "Apophysis", "linear-sierpinski.aquageo");

        var reference = LoadLinearApophysisFixture(flamePath);
        var candidate = FractalDslCompiler.Compile(File.ReadAllText(aquageoPath)).AffineIfsDefinitions.Single();

        Assert.Equal(reference.Count, candidate.Transforms.Count);
        var referencePoint = Vector2.Zero;
        var candidatePoint = candidate.Start;
        var sequence = new[] { 0, 2, 1, 1, 0, 2, 2, 1, 0, 0, 2, 1 };
        foreach (var transformIndex in sequence)
        {
            referencePoint = reference[transformIndex].Apply(referencePoint);
            candidatePoint = candidate.Transforms[transformIndex].Apply(candidatePoint);
            AssertClose(referencePoint, candidatePoint);
        }
    }

    [Fact]
    public void AquariumAffineIfsChaosGameMatchesIndependentReferenceMoments()
    {
        var root = FindRepoRoot();
        var aquageoPath = Path.Combine(root, "tests", "Aquarium.Engine.Fractal.Tests", "Fixtures", "Apophysis", "linear-sierpinski.aquageo");
        var candidate = FractalDslCompiler.Compile(File.ReadAllText(aquageoPath)).AffineIfsDefinitions.Single();

        var points = FractalAffineIfsChaosGame.Generate(candidate, count: 4096, burnIn: 16, new FractalXorShiftRandom(0xA90F_1357u));
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        var sum = Vector2.Zero;
        foreach (var point in points)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
            sum += point;
        }

        var mean = sum / points.Length;
        Assert.InRange(min.X, 0.0f, 0.01f);
        Assert.InRange(min.Y, 0.0f, 0.01f);
        Assert.InRange(max.X, 0.98f, 1.0f);
        Assert.InRange(max.Y, 0.84f, 0.8661f);
        Assert.InRange(mean.X, 0.47f, 0.53f);
        Assert.InRange(mean.Y, 0.27f, 0.33f);
    }

    [Fact]
    public void FlameParserReadsApophysisVariationSubset()
    {
        var root = FindRepoRoot();
        var flamePath = Path.Combine(root, "tests", "Aquarium.Engine.Fractal.Tests", "Fixtures", "Apophysis", "linear-spherical-bubble.flame");

        var flame = FractalFlameFileParser.ParseFirst(File.ReadAllText(flamePath), seed: 77);

        Assert.Equal("linear-spherical-bubble", flame.Name);
        Assert.Equal(77, flame.Seed);
        Assert.Equal(3, flame.Transforms.Count);
        Assert.Equal(1.0f, flame.Transforms[0].Variations.Linear);
        Assert.Equal(0.72f, flame.Transforms[1].Variations.Spherical);
        Assert.Equal(0.84f, flame.Transforms[2].Variations.Bubble);
        Assert.Equal(new Vector2(-0.20f, 0.08f), flame.Transforms[0].Translation);
    }

    [Fact]
    public void FlameVariationEvaluatorMatchesIndependentReference()
    {
        var root = FindRepoRoot();
        var flamePath = Path.Combine(root, "tests", "Aquarium.Engine.Fractal.Tests", "Fixtures", "Apophysis", "linear-spherical-bubble.flame");

        var flame = FractalFlameFileParser.ParseFirst(File.ReadAllText(flamePath), seed: 77);
        var point = new Vector2(0.37f, -0.21f);
        foreach (var transform in flame.Transforms)
        {
            var expected = ApplyReferenceVariation(transform, point);
            var actual = transform.Apply(point);
            AssertClose(expected, actual);
        }
    }

    [Fact]
    public void FlameHistogramIsDeterministicForReferenceFixture()
    {
        var root = FindRepoRoot();
        var flamePath = Path.Combine(root, "tests", "Aquarium.Engine.Fractal.Tests", "Fixtures", "Apophysis", "linear-spherical-bubble.flame");
        var flame = FractalFlameFileParser.ParseFirst(File.ReadAllText(flamePath), seed: 77);

        var points = FractalFlameChaosGame.Generate(flame, count: 8192, burnIn: 64, new FractalXorShiftRandom(0xBADC_0DEu));
        var histogram = FractalPointHistogramBuilder.Build(points, 64, 64, new Vector4(-8.0f, -8.0f, 8.0f, 8.0f));
        var occupied = histogram.Bins.Count(value => value > 0);
        var checksum = histogram.Bins.Aggregate(2166136261u, (hash, value) => unchecked((hash ^ (uint)value) * 16777619u));

        Assert.Equal(8192, histogram.HitCount);
        Assert.InRange(occupied, 450, 540);
        Assert.Equal(0xAEB1C81Bu, checksum);
    }

    private static IReadOnlyList<ReferenceAffineTransform> LoadLinearApophysisFixture(string path)
    {
        var document = XDocument.Load(path);
        var xforms = document.Descendants("xform").ToArray();
        Assert.NotEmpty(xforms);
        return xforms.Select(xform =>
        {
            Assert.Equal("1", (string?)xform.Attribute("linear"));
            var values = ((string?)xform.Attribute("coefs") ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => float.Parse(value, CultureInfo.InvariantCulture))
                .ToArray();
            Assert.Equal(6, values.Length);
            return new ReferenceAffineTransform(
                new Vector4(values[0], values[1], values[3], values[4]),
                new Vector2(values[2], values[5]));
        }).ToArray();
    }

    private static void AssertClose(Vector2 expected, Vector2 actual)
    {
        Assert.True(Vector2.Distance(expected, actual) <= 0.000001f, $"Expected {expected}, got {actual}.");
    }

    private static Vector2 ApplyReferenceVariation(FractalFlameTransform2D transform, Vector2 point)
    {
        var affine = new Vector2(
            (transform.Matrix.X * point.X) + (transform.Matrix.Y * point.Y) + transform.Translation.X,
            (transform.Matrix.Z * point.X) + (transform.Matrix.W * point.Y) + transform.Translation.Y);
        var radiusSquared = Vector2.Dot(affine, affine);
        var result = affine * transform.Variations.Linear;
        if (transform.Variations.Spherical != 0.0f)
        {
            result += affine * (transform.Variations.Spherical / MathF.Max(radiusSquared, 0.000001f));
        }

        if (transform.Variations.Bubble != 0.0f)
        {
            result += affine * (transform.Variations.Bubble * 4.0f / (radiusSquared + 4.0f));
        }

        return result;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Aquarium.Engine.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not find Aquarium repo root.");
    }

    private readonly record struct ReferenceAffineTransform(Vector4 Matrix, Vector2 Translation)
    {
        public Vector2 Apply(Vector2 point)
        {
            return new Vector2(
                (Matrix.X * point.X) + (Matrix.Y * point.Y) + Translation.X,
                (Matrix.Z * point.X) + (Matrix.W * point.Y) + Translation.Y);
        }
    }
}
