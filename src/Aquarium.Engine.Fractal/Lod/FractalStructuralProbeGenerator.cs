using System.Numerics;
using Aquarium.Engine.Fractal.Temporal;

namespace Aquarium.Engine.Fractal.Lod;

public static class FractalStructuralProbeGenerator
{
    private const float MinimumBoundRadius = 0.001f;
    private const float MinimumSourcePdf = 1.0e-6f;

    public static FractalProbeSample FromSummary(
        AquariumFractalSummary summary,
        AquariumFractalKey domainKey,
        float projectedPixelsPerWorldUnit,
        float sourcePdf,
        int payloadHandle = 0,
        AquariumFieldLayer layer = AquariumFieldLayer.Form,
        AquariumFieldEncoding? encoding = null)
    {
        var min = new Vector2(summary.BoundsMinMax.X, summary.BoundsMinMax.Y);
        var max = new Vector2(summary.BoundsMinMax.Z, summary.BoundsMinMax.W);
        var center2 = (min + max) * 0.5f;
        var extents = Vector2.Max((max - min) * 0.5f, Vector2.Zero);
        var radius = MathF.Max(extents.Length(), MinimumBoundRadius);
        var formTarget = FractalProjectedErrorScorer.Score(summary, projectedPixelsPerWorldUnit);
        var materialTarget = MathF.Max(summary.MaxMaterialDelta, 0.0f) * MathF.Max(projectedPixelsPerWorldUnit, 0.0f);
        var target = MathF.Max(formTarget, materialTarget);

        return new FractalProbeSample(
            domainKey,
            summary.NodeKey,
            new Vector3(center2, 0.0f),
            radius,
            target,
            MathF.Max(sourcePdf, MinimumSourcePdf),
            MathF.Max(summary.MaxMaterialDelta, 0.0f),
            payloadHandle,
            layer,
            encoding ?? PreferredFormEncoding(summary));
    }

    public static ResampledImportanceCandidate<FractalProbeSample> BuildCandidate(
        AquariumFractalSummary summary,
        AquariumFractalKey domainKey,
        float projectedPixelsPerWorldUnit,
        double sourceProbability,
        int payloadHandle = 0,
        AquariumFieldLayer layer = AquariumFieldLayer.Form,
        AquariumFieldEncoding? encoding = null)
    {
        return FromSummary(
            summary,
            domainKey,
            projectedPixelsPerWorldUnit,
            (float)Math.Clamp(sourceProbability, MinimumSourcePdf, 1.0),
            payloadHandle,
            layer,
            encoding).ToReservoirCandidate();
    }

    private static AquariumFieldEncoding PreferredFormEncoding(AquariumFractalSummary summary)
    {
        var mask = summary.FormEncodingMask;
        if ((mask & AquariumFieldEncodingFlags.Density) != 0 &&
            (mask & (AquariumFieldEncodingFlags.Height | AquariumFieldEncodingFlags.SignedDistance)) == 0)
        {
            return AquariumFieldEncoding.Density;
        }

        if ((mask & AquariumFieldEncodingFlags.Extinction) != 0 &&
            (mask & (AquariumFieldEncodingFlags.Height | AquariumFieldEncodingFlags.SignedDistance | AquariumFieldEncodingFlags.Density)) == 0)
        {
            return AquariumFieldEncoding.Extinction;
        }

        return AquariumFieldEncoding.SignedDistance;
    }
}
