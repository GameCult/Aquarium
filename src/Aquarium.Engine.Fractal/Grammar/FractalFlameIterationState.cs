using System.Numerics;

namespace Aquarium.Engine.Fractal.Grammar;

public readonly record struct FractalFlameIterationState(
    Vector2 Point,
    uint RandomState,
    int Step)
{
    public static FractalFlameIterationState Create(uint seed, int sampleIndex)
    {
        var state = Hash(seed ^ (uint)sampleIndex);
        return new FractalFlameIterationState(Vector2.Zero, state == 0 ? 0xA341316Cu : state, 0);
    }

    private static uint Hash(uint value)
    {
        value ^= value >> 16;
        value *= 747796405u;
        value ^= value >> 16;
        value *= 2891336453u;
        value ^= value >> 16;
        return value;
    }
}

public static class FractalFlameIterationStepper
{
    public static FractalFlameIterationState Advance(
        FractalFlameDefinition definition,
        FractalFlameIterationState state,
        int iterations)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentOutOfRangeException.ThrowIfNegative(iterations);

        var random = new StatefulRandom(state.RandomState);
        var point = state.Point;
        var total = definition.Transforms.Sum(transform => MathF.Max(transform.Weight, 0.0f));
        if (total <= 0.0f)
        {
            throw new ArgumentException("Flame transform weights must have positive total.", nameof(definition));
        }

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            point = SelectTransform(definition.Transforms, total, (float)random.NextDouble()).Apply(point, random);
        }

        return new FractalFlameIterationState(point, random.State, checked(state.Step + iterations));
    }

    private static FractalFlameTransform2D SelectTransform(
        IReadOnlyList<FractalFlameTransform2D> transforms,
        float total,
        float unit)
    {
        var target = unit * total;
        var cumulative = 0.0f;
        for (var index = 0; index < transforms.Count; index++)
        {
            cumulative += MathF.Max(transforms[index].Weight, 0.0f);
            if (target <= cumulative)
            {
                return transforms[index];
            }
        }

        return transforms[^1];
    }

    private sealed class StatefulRandom(uint state) : IFractalRandom
    {
        public uint State { get; private set; } = state == 0 ? 0xA341316Cu : state;

        public double NextDouble()
        {
            var value = State;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            State = value;
            return value / ((double)uint.MaxValue + 1.0);
        }
    }
}
