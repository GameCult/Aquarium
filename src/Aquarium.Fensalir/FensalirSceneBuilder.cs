using System.Numerics;
using Aquarium.Engine.Render;

namespace Aquarium.Fensalir;

public static class FensalirSceneBuilder
{
    private static readonly Vector3 SpineCenter = new(0.0f, 0.18f, 2.45f);
    private const float SpineBoundRadius = 5.8f;

    public static AquariumSceneState Build(float timeSeconds, float previousTimeSeconds)
    {
        return new AquariumSceneState
        {
            TraceHeightFieldSurface = true,
            UseStarfieldBackground = false,
            HeightFieldBrushes = FensalirFractalScene.HeightBrushes,
            SdfObjects = BuildSdfObjects(timeSeconds, previousTimeSeconds),
            SdfLights = BuildSdfLights(),
        };
    }

    private static AquariumSdfObject[] BuildSdfObjects(float timeSeconds, float previousTimeSeconds)
    {
        var sway = MathF.Sin(timeSeconds * 0.19f) * 0.018f;
        var previousSway = MathF.Sin(previousTimeSeconds * 0.19f) * 0.018f;
        var center = SpineCenter + new Vector3(sway, 0.0f, 0.0f);
        var previousCenter = SpineCenter + new Vector3(previousSway, 0.0f, 0.0f);
        return
        [
            new AquariumSdfObject(
                new Vector4(center, SpineBoundRadius),
                new Vector4(previousCenter, 0.0f),
                new Vector4(1.0f, timeSeconds, 0.0f, 0.0f))
        ];
    }

    private static AquariumSdfLight[] BuildSdfLights()
    {
        return
        [
            new AquariumSdfLight(
                new Vector4(SpineCenter, 2.8f),
                new Vector4(0.95f, 1.75f, 2.8f, 10.0f)),
            new AquariumSdfLight(
                new Vector4(-2.5f, 0.25f, 1.3f, 1.9f),
                new Vector4(1.0f, 0.16f, 0.72f, 10.0f)),
            new AquariumSdfLight(
                new Vector4(2.5f, 0.25f, 1.3f, 1.9f),
                new Vector4(1.0f, 0.16f, 0.72f, 10.0f)),
        ];
    }
}
