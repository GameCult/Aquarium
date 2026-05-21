using Aquarium.Engine.Fractal;

namespace Aquarium.Engine.Fractal.Grammar;

public static class FractalSummaryBuilder
{
    public static AquariumFractalSummary[] Build(FractalOwnershipTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var summaries = new AquariumFractalSummary[tree.Nodes.Count];
        for (var nodeIndex = 0; nodeIndex < tree.Nodes.Count; nodeIndex++)
        {
            var node = tree.Nodes[nodeIndex];
            var maxHeightError = 0.0f;
            var estimatedCost = 0.0f;
            for (var claimIndex = node.FirstClaimIndex; claimIndex < node.FirstClaimIndex + node.ClaimCount; claimIndex++)
            {
                var claim = tree.Claims[claimIndex];
                if (IsFormPayload(claim.PayloadKind))
                {
                    maxHeightError += MathF.Abs(claim.Amplitude);
                }

                estimatedCost += 1.0f;
            }

            summaries[nodeIndex] = new AquariumFractalSummary(
                node.Key,
                node.BoundsMinMax,
                maxHeightError,
                MaxMaterialDelta: 0.0f,
                estimatedCost,
                DescendantCount: node.ChildCount + node.ClaimCount);
        }

        return summaries;
    }

    private static bool IsFormPayload(AquariumFractalPayloadKind kind)
    {
        return kind is AquariumFractalPayloadKind.Height
            or AquariumFractalPayloadKind.SignedDistance
            or AquariumFractalPayloadKind.Density
            or AquariumFractalPayloadKind.Extinction;
    }
}
