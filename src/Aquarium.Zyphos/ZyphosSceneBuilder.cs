using System.Numerics;
using Aquarium.Engine.Render;

namespace Aquarium.Zyphos;

public static class ZyphosSceneBuilder
{
    public static AquariumSceneState Build(float timeSeconds, float previousTimeSeconds, ZyphosFractalRenderPlan fractalPlan)
    {
        return new AquariumSceneState
        {
            TraceHeightFieldSurface = false,
            UseStarfieldBackground = true,
            HeightFieldBrushes = fractalPlan.HeightBrushes,
            FractalReservoirField = new AquariumFractalReservoirField
            {
                SplatCount = 2_000_000,
                ReservoirUpdatesPerPass = 20_000,
                WorldCenterRadius = new Vector4(ZyphosUmbrosSystem.ZyphosCenter, ZyphosUmbrosSystem.ZyphosSurfaceRadius),
                ProgramTransforms = fractalPlan.GpuProgramTransforms,
            },
            SdfObjects = BuildSdfObjects(timeSeconds, previousTimeSeconds),
            SdfLights = BuildSdfLights(timeSeconds),
        };
    }

    private static AquariumSdfObject[] BuildSdfObjects(float timeSeconds, float previousTimeSeconds)
    {
        var rotation = ZyphosUmbrosSystem.MutualPhase(timeSeconds);
        var starCenter = ZyphosUmbrosSystem.PrimaryStarCenter(timeSeconds);
        var previousStarCenter = ZyphosUmbrosSystem.PrimaryStarCenter(previousTimeSeconds);

        var objects = new AquariumSdfObject[ZyphosRenderPlan.SdfObjectCount];
        objects[ZyphosRenderPlan.StarIndex] = new AquariumSdfObject(
            new Vector4(starCenter, ZyphosUmbrosSystem.PrimaryStarVisualRadius),
            new Vector4(previousStarCenter, 0.0f),
            new Vector4(ZyphosUmbrosSystem.PrimaryStarVisualRadius, rotation, 0.0f, 0.0f));

        return objects;
    }

    private static AquariumSdfLight[] BuildSdfLights(float timeSeconds)
    {
        var starCenter = ZyphosUmbrosSystem.PrimaryStarCenter(timeSeconds);
        return
        [
            new AquariumSdfLight(
                new Vector4(starCenter, ZyphosUmbrosSystem.PrimaryStarVisualRadius),
                new Vector4(3.2f, 2.5f, 1.8f, -100.0f)),
            new AquariumSdfLight(
                new Vector4(ZyphosUmbrosSystem.UmbrosCenter(timeSeconds), ZyphosUmbrosSystem.UmbrosSurfaceRadius),
                new Vector4(0.06f, 0.08f, 0.11f, -101.0f))
        ];
    }
}
