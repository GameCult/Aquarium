using System.Numerics;
using Aquarium.Engine.Render;

namespace Aquarium.Engine.Fractal.Tests;

public sealed class FractalReservoirFieldContractTests
{
    [Fact]
    public void SceneStateCarriesFractalReservoirFieldRequest()
    {
        var field = new AquariumFractalReservoirField
        {
            SplatCount = 2_000_000,
            SplatUpdatesPerFrame = 50_000,
            ReservoirUpdatesPerPass = 50_000,
            PriorityFocus = new Vector4(0.25f, -0.5f, 0.125f, 0.75f),
        };
        var scene = new AquariumSceneState
        {
            FractalReservoirField = field,
        };

        Assert.True(scene.FractalReservoirField.HasInput);
        Assert.Equal(8, scene.FractalReservoirField.Depth);
        Assert.Equal(2, scene.FractalReservoirField.CandidatesPerReservoirUpdate);
        Assert.Equal(50_000, scene.FractalReservoirField.SplatUpdatesPerFrame);
        Assert.Equal(0xA17EA11u, scene.FractalReservoirField.Seed);
        Assert.Equal(new Vector4(0.25f, -0.5f, 0.125f, 0.75f), scene.FractalReservoirField.PriorityFocus);
    }

    [Fact]
    public void FractalReservoirFieldRejectsNonConvergingUpdateBudgets()
    {
        Assert.False(AquariumFractalReservoirField.Empty.HasInput);
        Assert.False(new AquariumFractalReservoirField { SplatCount = 16, SplatUpdatesPerFrame = 4, ReservoirUpdatesPerPass = 0 }.HasInput);
        Assert.False(new AquariumFractalReservoirField { SplatCount = 16, SplatUpdatesPerFrame = 4, ReservoirUpdatesPerPass = 17 }.HasInput);
        Assert.False(new AquariumFractalReservoirField { SplatCount = 16, SplatUpdatesPerFrame = 17, ReservoirUpdatesPerPass = 4 }.HasInput);
        Assert.False(new AquariumFractalReservoirField { SplatCount = 16, SplatUpdatesPerFrame = 4, ReservoirUpdatesPerPass = 4, CandidatesPerReservoirUpdate = 0 }.HasInput);
        Assert.False(new AquariumFractalReservoirField { SplatCount = 16, SplatUpdatesPerFrame = 4, ReservoirUpdatesPerPass = 4, Depth = 0 }.HasInput);
    }
}
