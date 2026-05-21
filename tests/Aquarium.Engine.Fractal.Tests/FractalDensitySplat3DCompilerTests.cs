using System.Numerics;
using System.Runtime.InteropServices;
using Aquarium.Engine.Fractal;
using Aquarium.Engine.Fractal.Lod;

namespace Aquarium.Engine.Fractal.Tests;

public sealed class FractalDensitySplat3DCompilerTests
{
    [Fact]
    public void DensitySplat3DCompilerLowersSharedProbeIntoTransparentFormPacket()
    {
        var probe = new FractalProbeSample(
            new AquariumFractalKey("domain/flame"),
            new AquariumFractalKey("node/flame"),
            new Vector3(1.0f, 2.0f, 3.0f),
            BoundRadius: 4.0f,
            TargetContribution: 0.8f,
            SourcePdf: 0.25f,
            MaterialDelta: 0.3f,
            PayloadHandle: 12,
            Encoding: AquariumFieldEncoding.Density);

        var splat = FractalDensitySplat3DCompiler.FromProbe(
            probe,
            depthRadius: 0.5f,
            densityScale: 0.5f,
            extinctionScale: 0.25f,
            falloff: 3.0f,
            shapePower: 0.75f);

        Assert.Equal("domain/flame:node/flame:density3d:000012", splat.Key.Value);
        Assert.Equal(probe.LocalCenter, splat.Center);
        Assert.Equal(new Vector3(4.0f, 4.0f, 0.5f), splat.Radii);
        Assert.Equal(0.4f, splat.Density, 5);
        Assert.Equal(0.1f, splat.Extinction, 5);
        Assert.Equal(0.8f, splat.Confidence);
    }

    [Fact]
    public void DensitySplat3DCompilerRejectsOpaqueSdfProbe()
    {
        var probe = new FractalProbeSample(
            new AquariumFractalKey("domain/object"),
            new AquariumFractalKey("node/object"),
            Vector3.Zero,
            BoundRadius: 1.0f,
            TargetContribution: 1.0f,
            SourcePdf: 1.0f,
            MaterialDelta: 0.0f,
            PayloadHandle: 0);

        var ex = Assert.Throws<ArgumentException>(() => FractalDensitySplat3DCompiler.FromProbe(probe, depthRadius: 1.0f));

        Assert.Equal("probe", ex.ParamName);
    }

    [Fact]
    public void PackedDensitySplatKeepsGpuStrideAlignedWithOpaqueSplat()
    {
        Assert.Equal(80, Marshal.SizeOf<AquariumPackedFractalDensitySplat3D>());
        Assert.Equal(Marshal.SizeOf<AquariumPackedFractalSdfSplat3D>(), Marshal.SizeOf<AquariumPackedFractalDensitySplat3D>());
    }
}
