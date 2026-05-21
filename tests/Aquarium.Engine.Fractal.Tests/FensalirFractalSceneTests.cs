using Aquarium.Fensalir;

namespace Aquarium.Engine.Fractal.Tests;

public sealed class FensalirFractalSceneTests
{
    [Fact]
    public void FensalirSceneLoadsSplashPatchFromAquageoFile()
    {
        var tree = FensalirFractalScene.OwnershipTree;
        var brushes = FensalirFractalScene.HeightBrushes;

        Assert.EndsWith(".aquageo", FensalirFractalScene.PatchPath, StringComparison.Ordinal);
        Assert.True(File.Exists(FensalirFractalScene.PatchPath), $"Expected copied aquageo patch at {FensalirFractalScene.PatchPath}.");
        Assert.Contains(tree.Domains, domain => domain.Kind == AquariumFractalDomainKind.Surface2D && domain.Key.Value == "fensalir/marsh");
        Assert.Contains(tree.Domains, domain => domain.Kind == AquariumFractalDomainKind.Object3D && domain.Key.Value == "fensalir/spine");
        Assert.Contains(tree.Claims, claim => claim.Tags == "cyan-ripple" && claim.WaveAmplitude > 0.0f);
        Assert.Contains(tree.Claims, claim => claim.Tags == "micro-ripple");
        Assert.Equal(tree.Claims.Count(claim => claim.PayloadKind == AquariumFractalPayloadKind.Height), brushes.Length);
        Assert.Contains(brushes, brush => brush.WaveAmplitude > 0.0f && brush.WaveFrequency > 0.0f && brush.WaveSinePower > 0.0f);
        Assert.Contains("surface brushes", FensalirFractalScene.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void FensalirSceneBuildsReflectiveSurfaceAndSpineProxy()
    {
        var scene = FensalirSceneBuilder.Build(1.0f, 0.9f);

        Assert.True(scene.TraceHeightFieldSurface);
        Assert.False(scene.UseStarfieldBackground);
        Assert.NotEmpty(scene.HeightFieldBrushes);
        var spine = Assert.Single(scene.SdfObjects);
        Assert.True(spine.CenterRadius.W >= 5.0f);
        Assert.Equal(FensalirRenderPlan.SpineIndex, 0);
        Assert.True(scene.SdfLights.Count >= 1);
    }
}
