using Aquarium.Engine.Fractal.Grammar;
using Aquarium.Engine.Fractal.Lod;

namespace Aquarium.Engine.Fractal.Tests;

public sealed class FractalGpuProgramCompilerTests
{
    [Fact]
    public void CompilesSelectedClaimsIntoPackedGpuTransforms()
    {
        const string source = """
            domain Planetary zyphos -
            tile PositiveZ 2 1 1 zyphos/equator zyphos
            height ridge 0.10 -0.20 0.30 0.15 0.70 4.0 0.85 1.25 17 basalt
            ifs grove 2 2 0.00 0.00 0.20 0.10 0.50 1.2 0.4 3.0 0.9 0.4 91 canopy
            """;
        var tree = FractalDslCompiler.Compile(source);
        var summaries = FractalSummaryBuilder.Build(tree);
        var selected = FractalSelectedCutBuilder.Build(summaries, _ => 8.0f, 64.0f);

        var transforms = FractalGpuProgramCompiler.CompileSelectedTree(tree, selected, maxTransformCount: 8);

        Assert.NotEmpty(transforms);
        Assert.All(transforms, transform =>
        {
            Assert.True(transform.OffsetScaleAmplitude.Z > 0.0f);
            Assert.True(transform.RadiiRotationFalloff.X > 0.0f);
            Assert.True(transform.RadiiRotationFalloff.Y > 0.0f);
            Assert.True(transform.RadiiRotationFalloff.W > 0.0f);
            Assert.InRange(transform.MaterialSeedShape.X, 0.0f, 1.0f);
        });
    }

    [Fact]
    public void HonorsTransformBudget()
    {
        const string source = """
            tile PositiveZ 2 1 1 zyphos/equator
            ifs grove 4 3 0.00 0.00 0.20 0.10 0.50 1.2 0.4 3.0 0.9 0.4 91 canopy
            """;
        var tree = FractalDslCompiler.Compile(source);
        var summaries = FractalSummaryBuilder.Build(tree);
        var selected = FractalSelectedCutBuilder.Build(summaries, _ => 8.0f, 64.0f);

        var transforms = FractalGpuProgramCompiler.CompileSelectedTree(tree, selected, maxTransformCount: 5);

        Assert.Equal(5, transforms.Length);
    }
}
