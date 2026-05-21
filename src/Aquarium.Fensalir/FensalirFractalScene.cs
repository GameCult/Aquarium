using Aquarium.Engine.Fractal;
using Aquarium.Engine.Fractal.Brushes;
using Aquarium.Engine.Fractal.Debug;
using Aquarium.Engine.Fractal.Grammar;
using Aquarium.Engine.Fractal.Lod;
using Aquarium.Engine.Render;

namespace Aquarium.Fensalir;

public static class FensalirFractalScene
{
    private const string PatchRelativePath = "Worlds/fensalir-spine-emblem.aquageo";

    private static readonly Lazy<string> PatchSource = new(() => File.ReadAllText(PatchPath));
    private static readonly Lazy<FractalOwnershipTree> Tree = new(() => FractalDslCompiler.Compile(PatchSource.Value));
    private static readonly Lazy<AquariumFractalSummary[]> Summaries = new(() => FractalSummaryBuilder.Build(Tree.Value));
    private static readonly Lazy<AquariumSelectedCut[]> SelectedCut = new(() => FractalSelectedCutBuilder.Build(Summaries.Value, _ => 12.0f, maxEstimatedCost: 64.0f));
    private static readonly Lazy<AquariumHeightFieldBrush[]> Brushes = new(() => OrderBrushes(FractalHeightBrushCompiler.CompileSelectedTree(Tree.Value, SelectedCut.Value)));

    public static string PatchPath => Path.Combine(AssemblyDirectory, PatchRelativePath);

    public static FractalOwnershipTree OwnershipTree => Tree.Value;

    public static AquariumHeightFieldBrush[] HeightBrushes => Brushes.Value;

    public static AquariumFractalSummary[] NodeSummaries => Summaries.Value;

    public static AquariumSelectedCut[] SelectedCuts => SelectedCut.Value;

    public static string Summary =>
        $"{Path.GetFileName(PatchPath)} / {OwnershipTree.Claims.Count} claims / {HeightBrushes.Length} surface brushes / {SelectedCuts.Length} cuts";

    public static string DebugDump => FractalDebugDump.Build(OwnershipTree, NodeSummaries, SelectedCuts);

    private static AquariumHeightFieldBrush[] OrderBrushes(AquariumHeightFieldBrush[] brushes)
    {
        return brushes
            .OrderByDescending(brush => MathF.Abs(brush.Amplitude) * MathF.Max(brush.Radius, brush.RadiusY) + MathF.Abs(brush.WaveAmplitude) * brush.Radius)
            .ThenBy(brush => brush.Center.X)
            .ThenBy(brush => brush.Center.Y)
            .ToArray();
    }

    private static string AssemblyDirectory =>
        Path.GetDirectoryName(typeof(FensalirFractalScene).Assembly.Location)
        ?? AppContext.BaseDirectory;
}
