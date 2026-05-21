using System.Numerics;
using Aquarium.Engine.Fractal.Grammar;

namespace Aquarium.Engine.Fractal.Tests;

public sealed class FractalHistogramResidualFocusTests
{
    [Fact]
    public void EstimateReturnsZeroWhenCandidateMatchesReference()
    {
        var histogram = new FractalPointHistogram(2, 2, new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), [0, 4, 2, 0]);

        Assert.Equal(Vector4.Zero, FractalHistogramResidualFocus.Estimate(histogram, histogram));
    }

    [Fact]
    public void EstimateTargetsUnderrepresentedReferenceMass()
    {
        var reference = new FractalPointHistogram(4, 4, new Vector4(-2.0f, -2.0f, 2.0f, 2.0f), new int[16]);
        var candidate = new FractalPointHistogram(4, 4, reference.Bounds, new int[16]);
        reference.Bins[(1 * 4) + 2] = 100;
        candidate.Bins[(3 * 4) + 0] = 100;

        var focus = FractalHistogramResidualFocus.Estimate(reference, candidate);

        Assert.Equal(0.5f, focus.X, 5);
        Assert.Equal(-0.5f, focus.Y, 5);
        Assert.True(focus.Z > 0.0f);
        Assert.True(focus.W > 0.5f);
    }
}
