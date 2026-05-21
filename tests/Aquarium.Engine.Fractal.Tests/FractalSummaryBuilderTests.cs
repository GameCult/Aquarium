using System.Numerics;
using Aquarium.Engine.Fractal;
using Aquarium.Engine.Fractal.Grammar;

namespace Aquarium.Engine.Fractal.Tests;

public sealed class FractalSummaryBuilderTests
{
    [Fact]
    public void SummaryConservativelyCoversFlatHeightClaims()
    {
        var domainKey = new AquariumFractalKey("domain/tile");
        var rootKey = new AquariumFractalKey("domain/tile/root");
        var domain = new AquariumFractalDomain(domainKey, AquariumFractalDomainKind.CubeSphereTile, default, Vector4.Zero, Vector4.Zero);
        var claims = new[]
        {
            Claim("claim/a", domainKey, rootKey, Vector2.Zero, Vector2.One, 0.5f),
            Claim("claim/b", domainKey, rootKey, new Vector2(3.0f, 0.0f), new Vector2(2.0f, 1.0f), -1.25f),
        };
        var tree = FractalOwnershipTreeBuilder.BuildFlatUnion(domain, rootKey, claims);

        var summaries = FractalSummaryBuilder.Build(tree);

        Assert.Single(summaries);
        Assert.Equal(rootKey, summaries[0].NodeKey);
        Assert.Equal(new Vector4(-1.0f, -2.0f, 5.0f, 2.0f), summaries[0].BoundsMinMax);
        Assert.Equal(1.75f, summaries[0].MaxHeightError);
        Assert.Equal(AquariumFieldEncodingFlags.Height, summaries[0].FormEncodingMask);
        Assert.Equal(2.0f, summaries[0].EstimatedCost);
        Assert.Equal(2, summaries[0].DescendantCount);
    }

    [Fact]
    public void SummaryTreatsDensityAndExtinctionAsFormContribution()
    {
        var domainKey = new AquariumFractalKey("domain/volume");
        var rootKey = new AquariumFractalKey("domain/volume/root");
        var domain = new AquariumFractalDomain(domainKey, AquariumFractalDomainKind.Object3D, default, Vector4.Zero, Vector4.Zero);
        var claims = new[]
        {
            Claim("claim/density", domainKey, rootKey, Vector2.Zero, Vector2.One, 0.5f, AquariumFractalPayloadKind.Density),
            Claim("claim/extinction", domainKey, rootKey, new Vector2(3.0f, 0.0f), new Vector2(2.0f, 1.0f), 1.25f, AquariumFractalPayloadKind.Extinction),
        };
        var tree = FractalOwnershipTreeBuilder.BuildFlatUnion(domain, rootKey, claims);

        var summary = Assert.Single(FractalSummaryBuilder.Build(tree));

        Assert.Equal(1.75f, summary.MaxHeightError);
        Assert.Equal(AquariumFieldEncodingFlags.Density | AquariumFieldEncodingFlags.Extinction, summary.FormEncodingMask);
    }

    private static AquariumBrushClaim Claim(
        string key,
        AquariumFractalKey domainKey,
        AquariumFractalKey nodeKey,
        Vector2 center,
        Vector2 radii,
        float amplitude,
        AquariumFractalPayloadKind payloadKind = AquariumFractalPayloadKind.Height)
    {
        return new AquariumBrushClaim(
            new AquariumFractalKey(key),
            domainKey,
            nodeKey,
            payloadKind,
            center,
            radii,
            RotationRadians: 0.0f,
            Falloff: 3.0f,
            ShapePower: 1.0f,
            Amplitude: amplitude,
            Seed: 0,
            Tags: "summary");
    }
}
