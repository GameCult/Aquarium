using System.Numerics;

namespace Aquarium.Engine.Fractal.Lod;

public static class FractalDensitySplat3DCompiler
{
    public static AquariumFractalDensitySplat3D FromProbe(
        FractalProbeSample probe,
        float depthRadius,
        float densityScale = 1.0f,
        float extinctionScale = 1.0f,
        float falloff = 4.0f,
        float shapePower = 1.0f)
    {
        if (probe.Layer != AquariumFieldLayer.Form ||
            (probe.Encoding != AquariumFieldEncoding.Density && probe.Encoding != AquariumFieldEncoding.Extinction))
        {
            throw new ArgumentException("3D density splat lowering only accepts Form/Density or Form/Extinction probes.", nameof(probe));
        }

        if (!float.IsFinite(probe.BoundRadius) || probe.BoundRadius <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(probe), probe.BoundRadius, "Probe bound radius must be positive and finite.");
        }

        if (!float.IsFinite(depthRadius) || depthRadius <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(depthRadius), depthRadius, "3D density splat depth radius must be positive and finite.");
        }

        if (!float.IsFinite(densityScale) || densityScale < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(densityScale), densityScale, "Density scale must be finite and non-negative.");
        }

        if (!float.IsFinite(extinctionScale) || extinctionScale < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(extinctionScale), extinctionScale, "Extinction scale must be finite and non-negative.");
        }

        if (!float.IsFinite(falloff) || falloff <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(falloff), falloff, "3D density splat falloff must be positive and finite.");
        }

        if (!float.IsFinite(shapePower) || shapePower <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(shapePower), shapePower, "3D density splat shape power must be positive and finite.");
        }

        var contribution = Math.Clamp(probe.TargetContribution, 0.0f, 1.0f);
        var density = probe.Encoding == AquariumFieldEncoding.Density
            ? contribution * densityScale
            : contribution;
        var extinction = probe.Encoding == AquariumFieldEncoding.Extinction
            ? contribution * extinctionScale
            : contribution * densityScale * extinctionScale;

        return new AquariumFractalDensitySplat3D(
            new AquariumFractalDensitySplat3DKey(probe.DomainKey, probe.NodeKey, probe.PayloadHandle),
            probe.LocalCenter,
            Quaternion.Identity,
            new Vector3(probe.BoundRadius, probe.BoundRadius, depthRadius),
            falloff,
            shapePower,
            Math.Clamp(density, 0.0f, 1.0f),
            Math.Clamp(extinction, 0.0f, 1.0f),
            contribution);
    }
}
