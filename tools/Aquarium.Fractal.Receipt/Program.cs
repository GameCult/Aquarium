using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Aquarium.Engine.Fractal;
using Aquarium.Engine.Fractal.Grammar;
using Aquarium.Engine.Fractal.Lod;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;

var options = ReceiptOptions.Parse(args);
if (options.ReferencePpmPath is not null)
{
    RunReferencePpmReceipt(options);
    return;
}

if (options.FlamePath is not null)
{
    RunFlameHistogramReceipt(options);
    return;
}

var shaderSource = File.ReadAllText(options.ShaderPath);
using var runner = new GpuFractalSplatReceiptRunner(shaderSource);
var receipt = runner.Run(options);

Directory.CreateDirectory(options.OutputDirectory);
var receiptStamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
var receiptPath = Path.Combine(options.OutputDirectory, $"fractal-gpu-splat-receipt-{receiptStamp}.json");
File.WriteAllText(receiptPath, JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }));
var importReportPath = WriteProgramFlameImportReport(options, receiptStamp);

Console.WriteLine("=== Aquarium Perfect Machine GPU Splat Receipt ===");
Console.WriteLine($"adapter: {receipt.Adapter}");
Console.WriteLine($"splats: {receipt.SplatCount:N0}");
Console.WriteLine($"sdf reservoirs: {receipt.SdfReservoirCount:N0}");
Console.WriteLine($"pbr reservoirs: {receipt.PbrReservoirCount:N0}");
Console.WriteLine($"radiosity reservoirs: {receipt.RadiosityReservoirCount:N0}");
Console.WriteLine($"ifs program transforms: {receipt.ProgramTransformCount:N0}");
Console.WriteLine($"program mode: {DescribeProgramMode(receipt.ProgramMode)}");
Console.WriteLine($"splat updates/frame: {receipt.SplatUpdatesPerFrame:N0}");
Console.WriteLine($"candidates/pass: {receipt.CandidatesPerPass}");
Console.WriteLine($"reservoir updates/pass/frame: {receipt.ReservoirUpdatesPerPass:N0}");
Console.WriteLine($"reservoir full-coverage frames: {receipt.ReservoirFullCoverageFrames:0.0}");
Console.WriteLine($"frames: {receipt.MeasuredFrames}");
Console.WriteLine("shader: D3D12 compute, independent GPU-resident SDF/PBR/radiosity reservoir passes");
Console.WriteLine($"packed bytes/splat: {receipt.BytesPerSplat}");
Console.WriteLine($"packed bytes/reservoir: {receipt.BytesPerReservoir}");
Console.WriteLine($"gpu ms/frame total: {receipt.GpuMillisecondsPerFrame:0.000}");
Console.WriteLine($"gpu equivalent fps: {receipt.GpuEquivalentFps:0.0}");
Console.WriteLine($"gpu splats/sec: {receipt.GpuSplatsPerSecond:N0}");
Console.WriteLine($"gpu reservoir candidates/sec: {receipt.GpuReservoirCandidatesPerSecond:N0}");
Console.WriteLine($"cpu submit+wait ms/frame: {receipt.CpuSubmitAndWaitMillisecondsPerFrame:0.000}");
Console.WriteLine($"readback checksum: 0x{receipt.ReadbackChecksum:X16}");
if (importReportPath is not null)
{
    var report = FractalFlameFileParser.ParseFirstWithReport(File.ReadAllText(options.ProgramFlamePath!), unchecked((int)options.Seed)).Report;
    Console.WriteLine($"flame import: accepted {report.AcceptedFieldCount:N0}, approximated {report.ApproximatedFieldCount:N0}, ignored {report.IgnoredFieldCount:N0}, rejected {report.RejectedFieldCount:N0}");
    Console.WriteLine($"flame import report: {importReportPath}");
}
if (receipt.VisualParity is not null)
{
    Console.WriteLine($"visual parity samples: {receipt.VisualParity.ComparedSamples:N0}/{receipt.VisualParity.ReferenceSamples:N0}");
    Console.WriteLine($"visual parity views: {receipt.VisualParity.Views.Count:N0}");
    Console.WriteLine($"visual parity bins: {receipt.VisualParity.Width}x{receipt.VisualParity.Height}");
    Console.WriteLine($"visual parity occupancy overlap: {receipt.VisualParity.OccupancyOverlapPercent:0.00}%");
    Console.WriteLine($"visual parity distribution score: {receipt.VisualParity.DistributionScorePercent:0.00}%");
    Console.WriteLine($"visual parity l1 distance: {receipt.VisualParity.L1Distance:0.000000}");
    Console.WriteLine($"visual parity rmse: {receipt.VisualParity.Rmse:0.000000}");
    Console.WriteLine($"visual parity cosine: {receipt.VisualParity.CosineSimilarity:0.000000}");
    foreach (var view in receipt.VisualParity.Views)
    {
        Console.WriteLine($"visual view {view.Name}: score {view.DistributionScorePercent:0.00}% / hits gpu {view.GpuHitCount:N0}, ref {view.ReferenceHitCount:N0} / starved bins {view.StarvedReferenceBins:N0} / under-mass {view.UnderrepresentedMassPercent:0.00}%");
    }
}
Console.WriteLine($"receipt: {receiptPath}");

static string DescribeProgramMode(int mode)
{
    return mode switch
    {
        1 => "precomposed selected cut",
        2 => "flame 2D affine/variation program",
        _ => "fallback hash IFS",
    };
}

static void RunReferencePpmReceipt(ReceiptOptions options)
{
    var ppmBytes = File.ReadAllBytes(options.ReferencePpmPath!);
    var image = FractalPpmImageReceiptBuilder.Build(ppmBytes);
    var receipt = new ReferencePpmReceipt(
        Path.GetFullPath(options.ReferencePpmPath!),
        image.Width,
        image.Height,
        image.MaxChannelValue,
        image.PixelCount,
        image.NonBlackPixelCount,
        image.RgbChecksum,
        image.LuminanceChecksum);

    Directory.CreateDirectory(options.OutputDirectory);
    var receiptPath = Path.Combine(options.OutputDirectory, $"fractal-reference-ppm-receipt-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.json");
    File.WriteAllText(receiptPath, JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine("=== Aquarium External Flame Reference PPM Receipt ===");
    Console.WriteLine($"source: {receipt.SourcePath}");
    Console.WriteLine($"image: {receipt.Width}x{receipt.Height}");
    Console.WriteLine($"pixels: {receipt.PixelCount:N0}");
    Console.WriteLine($"non-black pixels: {receipt.NonBlackPixelCount:N0}");
    Console.WriteLine($"rgb checksum: 0x{receipt.RgbChecksum:X16}");
    Console.WriteLine($"luminance checksum: 0x{receipt.LuminanceChecksum:X16}");
    Console.WriteLine($"receipt: {receiptPath}");
}

static void RunFlameHistogramReceipt(ReceiptOptions options)
{
    var flameSource = File.ReadAllText(options.FlamePath!);
    var parsed = FractalFlameFileParser.ParseFirstWithReport(flameSource, unchecked((int)options.Seed));
    var flame = parsed.Definition;
    var points = FractalFlameChaosGame.Generate(
        flame,
        options.HistogramSamples,
        options.HistogramBurnIn,
        new FractalXorShiftRandom(options.Seed));
    var histogram = FractalPointHistogramBuilder.Build(
        points,
        options.HistogramWidth,
        options.HistogramHeight,
        options.HistogramBounds);
    var checksum = histogram.Bins.Aggregate(2166136261u, (hash, value) => unchecked((hash ^ (uint)value) * 16777619u));
    var occupiedBins = histogram.Bins.Count(value => value > 0);
    var receipt = new FlameHistogramReceipt(
        Path.GetFullPath(options.FlamePath!),
        flame.Name,
        flame.Transforms.Count,
        options.HistogramSamples,
        options.HistogramBurnIn,
        options.HistogramWidth,
        options.HistogramHeight,
        [
            options.HistogramBounds.X,
            options.HistogramBounds.Y,
            options.HistogramBounds.Z,
            options.HistogramBounds.W,
        ],
        histogram.HitCount,
        occupiedBins,
        checksum);

    Directory.CreateDirectory(options.OutputDirectory);
    var stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
    var receiptPath = Path.Combine(options.OutputDirectory, $"fractal-flame-histogram-receipt-{stamp}.json");
    File.WriteAllText(receiptPath, JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }));
    var importReportPath = Path.Combine(options.OutputDirectory, $"fractal-flame-import-report-{stamp}.json");
    File.WriteAllText(importReportPath, JsonSerializer.Serialize(new FlameImportReportReceipt(Path.GetFullPath(options.FlamePath!), parsed.Report), new JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine("=== Aquarium Fractal Flame Histogram Receipt ===");
    Console.WriteLine($"flame: {receipt.FlameName}");
    Console.WriteLine($"source: {receipt.SourcePath}");
    Console.WriteLine($"xforms: {receipt.TransformCount:N0}");
    Console.WriteLine($"samples: {receipt.SampleCount:N0}");
    Console.WriteLine($"burn-in: {receipt.BurnIn:N0}");
    Console.WriteLine($"histogram: {receipt.Width}x{receipt.Height}");
    Console.WriteLine($"hits: {receipt.HitCount:N0}");
    Console.WriteLine($"occupied bins: {receipt.OccupiedBins:N0}");
    Console.WriteLine($"histogram checksum: 0x{receipt.Checksum:X8}");
    Console.WriteLine($"flame import: accepted {parsed.Report.AcceptedFieldCount:N0}, approximated {parsed.Report.ApproximatedFieldCount:N0}, ignored {parsed.Report.IgnoredFieldCount:N0}, rejected {parsed.Report.RejectedFieldCount:N0}");
    Console.WriteLine($"flame import report: {importReportPath}");
    Console.WriteLine($"receipt: {receiptPath}");
}

static string? WriteProgramFlameImportReport(ReceiptOptions options, string receiptStamp)
{
    if (options.ProgramFlamePath is null)
    {
        return null;
    }

    var parsed = FractalFlameFileParser.ParseFirstWithReport(File.ReadAllText(options.ProgramFlamePath), unchecked((int)options.Seed));
    var reportPath = Path.Combine(options.OutputDirectory, $"fractal-flame-import-report-{receiptStamp}.json");
    File.WriteAllText(reportPath, JsonSerializer.Serialize(new FlameImportReportReceipt(Path.GetFullPath(options.ProgramFlamePath), parsed.Report), new JsonSerializerOptions { WriteIndented = true }));
    return reportPath;
}

internal sealed class GpuFractalSplatReceiptRunner : IDisposable
{
    private const int ThreadGroupSize = 256;

    private readonly ID3D12Device device;
    private readonly ID3D12CommandQueue queue;
    private readonly ID3D12CommandAllocator allocator;
    private readonly ID3D12GraphicsCommandList commandList;
    private readonly ID3D12Fence fence;
    private readonly AutoResetEvent fenceEvent = new(false);
    private readonly ID3D12RootSignature rootSignature;
    private readonly ID3D12PipelineState splatPipelineState;
    private readonly ID3D12PipelineState sdfPipelineState;
    private readonly ID3D12PipelineState pbrPipelineState;
    private readonly ID3D12PipelineState radiosityPipelineState;
    private readonly string adapterName;
    private ulong fenceValue;

    public GpuFractalSplatReceiptRunner(string shaderSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shaderSource);
        device = D3D12.D3D12CreateDevice<ID3D12Device>(IntPtr.Zero, FeatureLevel.Level_11_0);
        adapterName = ResolveAdapterName();
        queue = device.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct));
        allocator = device.CreateCommandAllocator(CommandListType.Direct);
        commandList = device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, allocator, null);
        commandList.Close();
        fence = device.CreateFence(0);
        rootSignature = CreateRootSignature(device);
        splatPipelineState = CreatePipelineState(device, rootSignature, shaderSource, "D3D12FractalSplatReceiptCS");
        sdfPipelineState = CreatePipelineState(device, rootSignature, shaderSource, "D3D12SdfEnvelopeReservoirCS");
        pbrPipelineState = CreatePipelineState(device, rootSignature, shaderSource, "D3D12PbrMaterialReservoirCS");
        radiosityPipelineState = CreatePipelineState(device, rootSignature, shaderSource, "D3D12RadiosityReservoirCS");
    }

    public GpuFractalSplatReceipt Run(ReceiptOptions options)
    {
        var resolvedProgramTransforms = BuildReceiptProgram(options);
        var resolvedProgramMode = resolvedProgramTransforms.Length > 0 ? options.ProgramMode : 0;
        var splatStride = Marshal.SizeOf<AquariumPackedFractalSdfSplat3D>();
        var reservoirStride = Marshal.SizeOf<AquariumPackedSdfEnvelopeReservoir>();
        var framePlan = FractalGpuReservoirBudgetPlanner.Plan(
            options.SplatCount,
            options.ReservoirUpdatesPerPass,
            options.CandidatesPerPass,
            splatStride,
            reservoirStride);
        var splatBytes = checked((ulong)splatStride * (ulong)options.SplatCount);
        var reservoirBytes = checked((ulong)reservoirStride * (ulong)options.SplatCount);
        using var splats = CreateUavBuffer(splatBytes, "Aquarium Fractal Receipt GPU Splat Buffer");
        using var sdfReservoirs = CreateUavBuffer(reservoirBytes, "Aquarium Fractal Receipt SDF Reservoir Buffer");
        using var pbrReservoirs = CreateUavBuffer(reservoirBytes, "Aquarium Fractal Receipt PBR Reservoir Buffer");
        using var radiosityReservoirs = CreateUavBuffer(reservoirBytes, "Aquarium Fractal Receipt Radiosity Reservoir Buffer");
        using var programTransforms = CreateProgramTransformBuffer(resolvedProgramTransforms);

        var readbackSplatBytes = (ulong)Math.Min(options.ReadbackSplats, options.SplatCount) * (ulong)splatStride;
        var readbackReservoirBytes = (ulong)Math.Min(options.ReadbackSplats, options.SplatCount) * (ulong)reservoirStride;
        var totalReadbackBytes = readbackSplatBytes + (readbackReservoirBytes * 3UL);
        using var readback = device.CreateCommittedResource(HeapType.Readback, ResourceDescription.Buffer(Math.Max(totalReadbackBytes, 1)), ResourceStates.CopyDest, null);
        using var queryHeap = device.CreateQueryHeap<ID3D12QueryHeap>(new QueryHeapDescription(QueryHeapType.Timestamp, 2u));
        using var queryReadback = device.CreateCommittedResource(HeapType.Readback, ResourceDescription.Buffer(16), ResourceStates.CopyDest, null);

        queue.GetTimestampFrequency(out var timestampFrequency);
        var measuredGpuTicks = 0UL;
        var measuredCpuTicks = 0L;
        var measuredFrames = 0;

        for (var frame = 0; frame < options.WarmupFrames + options.MeasuredFrames; frame++)
        {
            allocator.Reset();
            commandList.Reset(allocator, splatPipelineState);
            commandList.SetComputeRootSignature(rootSignature);
            BindConstants(options, frame, resolvedProgramTransforms.Length, resolvedProgramMode);
            commandList.SetComputeRootUnorderedAccessView(1, splats.GPUVirtualAddress);
            commandList.SetComputeRootUnorderedAccessView(2, sdfReservoirs.GPUVirtualAddress);
            commandList.SetComputeRootUnorderedAccessView(3, pbrReservoirs.GPUVirtualAddress);
            commandList.SetComputeRootUnorderedAccessView(4, radiosityReservoirs.GPUVirtualAddress);
            commandList.SetComputeRootShaderResourceView(5, programTransforms.GPUVirtualAddress);
            commandList.EndQuery(queryHeap, QueryType.Timestamp, 0);
            var splatDispatchCount = frame == 0 ? options.SplatCount : options.SplatUpdatesPerFrame;
            Dispatch(splatPipelineState, splatDispatchCount);
            commandList.ResourceBarrier(ResourceBarrier.BarrierUnorderedAccessView(splats));
            Dispatch(sdfPipelineState, options.ReservoirUpdatesPerPass);
            Dispatch(pbrPipelineState, options.ReservoirUpdatesPerPass);
            Dispatch(radiosityPipelineState, options.ReservoirUpdatesPerPass);
            commandList.EndQuery(queryHeap, QueryType.Timestamp, 1);
            commandList.ResolveQueryData(queryHeap, QueryType.Timestamp, 0, 2, queryReadback, 0);

            if (frame == options.WarmupFrames + options.MeasuredFrames - 1 && totalReadbackBytes > 0)
            {
                CopyReceiptReadback(splats, sdfReservoirs, pbrReservoirs, radiosityReservoirs, readback, readbackSplatBytes, readbackReservoirBytes);
            }

            commandList.Close();
            var cpuStart = Stopwatch.GetTimestamp();
            queue.ExecuteCommandList(commandList);
            WaitForGpu();
            var cpuEnd = Stopwatch.GetTimestamp();

            if (frame >= options.WarmupFrames)
            {
                measuredFrames++;
                measuredCpuTicks += cpuEnd - cpuStart;
                unsafe
                {
                    var timestamps = (ulong*)queryReadback.Map<byte>(0);
                    measuredGpuTicks += timestamps[1] - timestamps[0];
                    queryReadback.Unmap(0);
                }
            }
        }

        var checksum = totalReadbackBytes == 0 ? 0UL : Checksum(readback, (int)totalReadbackBytes);
        var visualParity = totalReadbackBytes == 0 ? null : BuildVisualParity(options, readback, splatStride, (int)(readbackSplatBytes / (ulong)splatStride));
        var gpuSeconds = measuredGpuTicks / (double)timestampFrequency;
        var gpuMsPerFrame = gpuSeconds * 1000.0 / Math.Max(measuredFrames, 1);
        var cpuMsPerFrame = measuredCpuTicks * 1000.0 / Stopwatch.Frequency / Math.Max(measuredFrames, 1);
        var splatsPerSecond = options.SplatUpdatesPerFrame * (double)measuredFrames / Math.Max(gpuSeconds, 1.0e-12);
        var reservoirCandidatesPerSecond = options.ReservoirUpdatesPerPass * 3.0 * options.CandidatesPerPass * measuredFrames / Math.Max(gpuSeconds, 1.0e-12);

        return new GpuFractalSplatReceipt(
            adapterName,
            options.SplatCount,
            options.SplatCount,
            options.SplatCount,
            options.SplatCount,
            resolvedProgramTransforms.Length,
            resolvedProgramMode,
            options.SplatUpdatesPerFrame,
            options.CandidatesPerPass,
            options.ReservoirUpdatesPerPass,
            framePlan.Passes[0].ExpectedFullCoverageFrames,
            measuredFrames,
            splatStride,
            reservoirStride,
            gpuMsPerFrame,
            1000.0 / Math.Max(gpuMsPerFrame, 1.0e-12),
            splatsPerSecond,
            reservoirCandidatesPerSecond,
            cpuMsPerFrame,
            checksum,
            visualParity);

        void Dispatch(ID3D12PipelineState state, int elementCount)
        {
            commandList.SetPipelineState(state);
            commandList.Dispatch((uint)((elementCount + ThreadGroupSize - 1) / ThreadGroupSize), 1, 1);
        }
    }

    public void Dispose()
    {
        WaitForGpu();
        radiosityPipelineState.Dispose();
        pbrPipelineState.Dispose();
        sdfPipelineState.Dispose();
        splatPipelineState.Dispose();
        rootSignature.Dispose();
        fence.Dispose();
        commandList.Dispose();
        allocator.Dispose();
        queue.Dispose();
        device.Dispose();
        fenceEvent.Dispose();
    }

    private ID3D12Resource CreateUavBuffer(ulong bytes, string name)
    {
        var resource = device.CreateCommittedResource(HeapType.Default, ResourceDescription.Buffer(bytes, ResourceFlags.AllowUnorderedAccess), ResourceStates.UnorderedAccess, null);
        resource.Name = name;
        return resource;
    }

    private ID3D12Resource CreateProgramTransformBuffer(AquariumPackedFractalIfsTransform[] transforms)
    {
        var stride = Marshal.SizeOf<AquariumPackedFractalIfsTransform>();
        var bytes = (ulong)(Math.Max(transforms.Length, 1) * stride);
        var upload = device.CreateCommittedResource(HeapType.Upload, ResourceDescription.Buffer(bytes), ResourceStates.GenericRead, null);
        upload.Name = "Aquarium Fractal Receipt IFS Program Upload Buffer";
        unsafe
        {
            var target = (AquariumPackedFractalIfsTransform*)upload.Map<byte>(0);
            for (var index = 0; index < transforms.Length; index++)
            {
                target[index] = transforms[index];
            }

            upload.Unmap(0);
        }

        var resource = device.CreateCommittedResource(HeapType.Default, ResourceDescription.Buffer(bytes), ResourceStates.CopyDest, null);
        resource.Name = "Aquarium Fractal Receipt IFS Program Buffer";
        allocator.Reset();
        commandList.Reset(allocator, null);
        commandList.CopyBufferRegion(resource, 0, upload, 0, bytes);
        commandList.ResourceBarrier(ResourceBarrier.BarrierTransition(resource, ResourceStates.CopyDest, ResourceStates.NonPixelShaderResource));
        commandList.Close();
        queue.ExecuteCommandList(commandList);
        WaitForGpu();
        upload.Dispose();
        return resource;
    }

    private static AquariumPackedFractalIfsTransform[] BuildReceiptProgram(ReceiptOptions options)
    {
        if (options.ProgramFlamePath is not null)
        {
            var flame = FractalFlameFileParser.ParseFirst(File.ReadAllText(options.ProgramFlamePath), unchecked((int)options.Seed));
            return FractalGpuProgramCompiler.CompileFlame2D(flame, Math.Max(options.ProgramTransformCount, 1));
        }

        return BuildSyntheticReceiptProgram(options.ProgramTransformCount);
    }

    private static AquariumPackedFractalIfsTransform[] BuildSyntheticReceiptProgram(int transformCount)
    {
        if (transformCount <= 0)
        {
            return [default];
        }

        var transforms = new AquariumPackedFractalIfsTransform[transformCount];
        for (var index = 0; index < transforms.Length; index++)
        {
            var phase = MathF.Tau * index / Math.Max(transformCount, 1);
            var branch = new Vector2(MathF.Cos(phase), MathF.Sin(phase));
            var radius = 0.32f + (index % 5) * 0.035f;
            var scale = 0.46f + (index % 3) * 0.035f;
            var rotation = phase * 0.37f;
            transforms[index] = new AquariumPackedFractalIfsTransform(
                new Vector4(branch * 0.42f, scale, 0.18f + index * 0.013f),
                new Vector4(radius, radius * 0.62f, rotation, 4.0f),
                new Vector4((index + 1.0f) / (transformCount + 1.0f), index * 31 + 7, MathF.Cos(rotation), MathF.Sin(rotation)),
                new Vector4(index % 6, 0.0f, 0.0f, 0.0f));
        }

        return transforms;
    }

    private void BindConstants(ReceiptOptions options, int frame, int programTransformCount, int programMode)
    {
        commandList.SetComputeRoot32BitConstant(0, (uint)options.SplatCount, 0);
        commandList.SetComputeRoot32BitConstant(0, (uint)frame, 1);
        commandList.SetComputeRoot32BitConstant(0, (uint)options.Depth, 2);
        commandList.SetComputeRoot32BitConstant(0, options.Seed, 3);
        commandList.SetComputeRoot32BitConstant(0, (uint)options.CandidatesPerPass, 4);
        commandList.SetComputeRoot32BitConstant(0, (uint)options.ReservoirUpdatesPerPass, 5);
        commandList.SetComputeRoot32BitConstant(0, (uint)programTransformCount, 6);
        commandList.SetComputeRoot32BitConstant(0, (uint)programMode, 7);
    }

    private void CopyReceiptReadback(ID3D12Resource splats, ID3D12Resource sdf, ID3D12Resource pbr, ID3D12Resource radiosity, ID3D12Resource readback, ulong splatBytes, ulong reservoirBytes)
    {
        commandList.ResourceBarrier(ResourceBarrier.BarrierTransition(splats, ResourceStates.UnorderedAccess, ResourceStates.CopySource));
        commandList.ResourceBarrier(ResourceBarrier.BarrierTransition(sdf, ResourceStates.UnorderedAccess, ResourceStates.CopySource));
        commandList.ResourceBarrier(ResourceBarrier.BarrierTransition(pbr, ResourceStates.UnorderedAccess, ResourceStates.CopySource));
        commandList.ResourceBarrier(ResourceBarrier.BarrierTransition(radiosity, ResourceStates.UnorderedAccess, ResourceStates.CopySource));
        commandList.CopyBufferRegion(readback, 0, splats, 0, splatBytes);
        commandList.CopyBufferRegion(readback, splatBytes, sdf, 0, reservoirBytes);
        commandList.CopyBufferRegion(readback, splatBytes + reservoirBytes, pbr, 0, reservoirBytes);
        commandList.CopyBufferRegion(readback, splatBytes + (reservoirBytes * 2), radiosity, 0, reservoirBytes);
        commandList.ResourceBarrier(ResourceBarrier.BarrierTransition(splats, ResourceStates.CopySource, ResourceStates.UnorderedAccess));
        commandList.ResourceBarrier(ResourceBarrier.BarrierTransition(sdf, ResourceStates.CopySource, ResourceStates.UnorderedAccess));
        commandList.ResourceBarrier(ResourceBarrier.BarrierTransition(pbr, ResourceStates.CopySource, ResourceStates.UnorderedAccess));
        commandList.ResourceBarrier(ResourceBarrier.BarrierTransition(radiosity, ResourceStates.CopySource, ResourceStates.UnorderedAccess));
    }

    private static unsafe VisualParityReceipt? BuildVisualParity(
        ReceiptOptions options,
        ID3D12Resource readback,
        int splatStride,
        int readbackSplatCount)
    {
        if (!options.VisualParity || options.ProgramFlamePath is null || readbackSplatCount <= 0)
        {
            return null;
        }

        var flame = FractalFlameFileParser.ParseFirst(File.ReadAllText(options.ProgramFlamePath), unchecked((int)options.Seed));
        var referencePoints = FractalFlameChaosGame.Generate(
            flame,
            options.VisualParityReferenceSamples,
            options.HistogramBurnIn,
            new FractalXorShiftRandom(options.Seed));
        var points = new Vector2[readbackSplatCount];
        var splats = (AquariumPackedFractalSdfSplat3D*)readback.Map<byte>(0);
        for (var index = 0; index < points.Length; index++)
        {
            var center = splats[index].CenterRadius;
            points[index] = new Vector2(center.X, center.Y);
        }

        readback.Unmap(0);
        var views = options.VisualParityViews.Count > 0
            ? options.VisualParityViews
            : [new VisualParityView("global", options.HistogramBounds)];
        var viewReceipts = new VisualParityViewReceipt[views.Count];
        for (var index = 0; index < views.Count; index++)
        {
            viewReceipts[index] = BuildVisualParityView(
                options,
                views[index],
                referencePoints,
                points,
                options.HistogramWidth,
                options.HistogramHeight);
        }

        var primary = viewReceipts[0];
        return new VisualParityReceipt(
            readbackSplatCount,
            options.VisualParityReferenceSamples,
            options.HistogramWidth,
            options.HistogramHeight,
            primary.Bounds,
            primary.ReferenceHitCount,
            primary.GpuHitCount,
            primary.ReferenceOccupiedBins,
            primary.GpuOccupiedBins,
            primary.SharedOccupiedBins,
            primary.OccupancyOverlapPercent,
            primary.L1Distance,
            primary.Rmse,
            primary.CosineSimilarity,
            primary.DistributionScorePercent,
            viewReceipts);
    }

    private static VisualParityViewReceipt BuildVisualParityView(
        ReceiptOptions options,
        VisualParityView view,
        IReadOnlyList<Vector2> referencePoints,
        IReadOnlyList<Vector2> gpuPoints,
        int width,
        int height)
    {
        var referenceHistogram = FractalPointHistogramBuilder.Build(referencePoints, width, height, view.Bounds);
        var gpuHistogram = FractalPointHistogramBuilder.Build(gpuPoints, width, height, view.Bounds);
        WriteVisualParityImages(options, view.Name, referenceHistogram, gpuHistogram);
        var metrics = CompareHistograms(referenceHistogram, gpuHistogram);
        return new VisualParityViewReceipt(
            view.Name,
            [
                view.Bounds.X,
                view.Bounds.Y,
                view.Bounds.Z,
                view.Bounds.W,
            ],
            referenceHistogram.HitCount,
            gpuHistogram.HitCount,
            metrics.ReferenceOccupiedBins,
            metrics.GpuOccupiedBins,
            metrics.SharedOccupiedBins,
            metrics.StarvedReferenceBins,
            metrics.OccupancyOverlapPercent,
            metrics.UnderrepresentedMassPercent,
            metrics.OversampledMassPercent,
            metrics.L1Distance,
            metrics.Rmse,
            metrics.CosineSimilarity,
            metrics.DistributionScorePercent);
    }

    private static void WriteVisualParityImages(
        ReceiptOptions options,
        string viewName,
        FractalPointHistogram referenceHistogram,
        FractalPointHistogram gpuHistogram)
    {
        if (options.VisualParityImageDirectory is null)
        {
            return;
        }

        Directory.CreateDirectory(options.VisualParityImageDirectory);
        var safeViewName = SanitizeFileSegment(viewName);
        var safePrefix = SanitizeFileSegment(options.VisualParityImagePrefix);
        WriteHistogramPpm(
            Path.Combine(options.VisualParityImageDirectory, $"{safePrefix}-{safeViewName}-reference.ppm"),
            referenceHistogram,
            new Vector3(0.45f, 0.86f, 1.0f));
        WriteHistogramPpm(
            Path.Combine(options.VisualParityImageDirectory, $"{safePrefix}-{safeViewName}-aquarium.ppm"),
            gpuHistogram,
            new Vector3(1.0f, 0.68f, 0.28f));
    }

    private static void WriteHistogramPpm(string path, FractalPointHistogram histogram, Vector3 color)
    {
        const int scale = 4;
        var width = histogram.Width * scale;
        var height = histogram.Height * scale;
        var max = histogram.Bins.Max();
        using var stream = File.Create(path);
        using var writer = new StreamWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        writer.Write($"P6\n{width} {height}\n255\n");
        writer.Flush();
        var row = new byte[width * 3];
        for (var y = 0; y < height; y++)
        {
            Array.Clear(row);
            var binY = histogram.Height - 1 - (y / scale);
            for (var x = 0; x < width; x++)
            {
                var binX = x / scale;
                var value = histogram.Bins[(binY * histogram.Width) + binX];
                var normalized = max <= 0 ? 0.0 : Math.Sqrt(Math.Log(1.0 + value) / Math.Log(1.0 + max));
                var offset = x * 3;
                row[offset] = (byte)Math.Clamp((int)(normalized * color.X * 255.0), 0, 255);
                row[offset + 1] = (byte)Math.Clamp((int)(normalized * color.Y * 255.0), 0, 255);
                row[offset + 2] = (byte)Math.Clamp((int)(normalized * color.Z * 255.0), 0, 255);
            }

            stream.Write(row);
        }
    }

    private static string SanitizeFileSegment(string value)
    {
        var chars = value.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-').ToArray();
        return new string(chars).Trim('-') is { Length: > 0 } sanitized ? sanitized : "view";
    }

    private static HistogramComparison CompareHistograms(FractalPointHistogram reference, FractalPointHistogram candidate)
    {
        if (reference.Width != candidate.Width || reference.Height != candidate.Height)
        {
            throw new ArgumentException("Histogram dimensions must match.");
        }

        var referenceTotal = Math.Max(reference.HitCount, 1);
        var candidateTotal = Math.Max(candidate.HitCount, 1);
        var l1 = 0.0;
        var squared = 0.0;
        var dot = 0.0;
        var referenceMagnitude = 0.0;
        var candidateMagnitude = 0.0;
        var referenceOccupied = 0;
        var candidateOccupied = 0;
        var sharedOccupied = 0;
        var starvedReferenceBins = 0;
        var underrepresentedMass = 0.0;
        var oversampledMass = 0.0;
        for (var index = 0; index < reference.Bins.Length; index++)
        {
            var referenceProbability = reference.Bins[index] / (double)referenceTotal;
            var candidateProbability = candidate.Bins[index] / (double)candidateTotal;
            var delta = referenceProbability - candidateProbability;
            l1 += Math.Abs(delta);
            underrepresentedMass += Math.Max(delta, 0.0);
            oversampledMass += Math.Max(-delta, 0.0);
            squared += delta * delta;
            dot += referenceProbability * candidateProbability;
            referenceMagnitude += referenceProbability * referenceProbability;
            candidateMagnitude += candidateProbability * candidateProbability;
            var referenceHas = reference.Bins[index] > 0;
            var candidateHas = candidate.Bins[index] > 0;
            referenceOccupied += referenceHas ? 1 : 0;
            candidateOccupied += candidateHas ? 1 : 0;
            sharedOccupied += referenceHas && candidateHas ? 1 : 0;
            starvedReferenceBins += referenceHas && !candidateHas ? 1 : 0;
        }

        var occupiedUnion = Math.Max(referenceOccupied + candidateOccupied - sharedOccupied, 1);
        var cosine = dot / Math.Max(Math.Sqrt(referenceMagnitude * candidateMagnitude), 1.0e-12);
        var rmse = Math.Sqrt(squared / reference.Bins.Length);
        return new HistogramComparison(
            referenceOccupied,
            candidateOccupied,
            sharedOccupied,
            starvedReferenceBins,
            sharedOccupied * 100.0 / occupiedUnion,
            underrepresentedMass * 100.0,
            oversampledMass * 100.0,
            l1,
            rmse,
            cosine,
            Math.Max(0.0, (1.0 - (l1 * 0.5)) * 100.0));
    }

    private void WaitForGpu()
    {
        fenceValue++;
        queue.Signal(fence, fenceValue);
        if (fence.CompletedValue < fenceValue)
        {
            fence.SetEventOnCompletion(fenceValue, fenceEvent.SafeWaitHandle.DangerousGetHandle());
            fenceEvent.WaitOne();
        }
    }

    private string ResolveAdapterName()
    {
        using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory6>();
        for (var index = 0u; factory.EnumAdapterByGpuPreference(index, GpuPreference.HighPerformance, out IDXGIAdapter1? adapter).Success && adapter is not null; index++)
        {
            using (adapter)
            {
                var description = adapter.Description1;
                if ((description.Flags & AdapterFlags.Software) == 0)
                {
                    return description.Description;
                }
            }
        }

        return "default D3D12 adapter";
    }

    private static ID3D12RootSignature CreateRootSignature(ID3D12Device device)
    {
        var rootParameters = new[]
        {
            new RootParameter(new RootConstants(0, 0, 8), ShaderVisibility.All),
            new RootParameter(RootParameterType.UnorderedAccessView, new RootDescriptor(0, 0), ShaderVisibility.All),
            new RootParameter(RootParameterType.UnorderedAccessView, new RootDescriptor(1, 0), ShaderVisibility.All),
            new RootParameter(RootParameterType.UnorderedAccessView, new RootDescriptor(2, 0), ShaderVisibility.All),
            new RootParameter(RootParameterType.UnorderedAccessView, new RootDescriptor(3, 0), ShaderVisibility.All),
            new RootParameter(RootParameterType.ShaderResourceView, new RootDescriptor(0, 0), ShaderVisibility.All),
        };
        var description = new RootSignatureDescription(RootSignatureFlags.None, rootParameters, []);
        return device.CreateRootSignature(0, in description, RootSignatureVersion.Version1);
    }

    private static ID3D12PipelineState CreatePipelineState(ID3D12Device device, ID3D12RootSignature rootSignature, string shaderSource, string entry)
    {
        var shader = Compiler.Compile(shaderSource, entry, "D3D12FractalReservoirCompute.hlsl", "cs_5_0", ShaderFlags.OptimizationLevel3, EffectFlags.None);
        return device.CreateComputePipelineState(new ComputePipelineStateDescription { RootSignature = rootSignature, ComputeShader = shader });
    }

    private static unsafe ulong Checksum(ID3D12Resource readback, int byteCount)
    {
        var data = (byte*)readback.Map<byte>(0);
        var hash = 1469598103934665603UL;
        for (var index = 0; index < byteCount; index++)
        {
            hash ^= data[index];
            hash *= 1099511628211UL;
        }

        readback.Unmap(0);
        return hash;
    }

}

internal sealed record ReceiptOptions(
    int SplatCount,
    int SplatUpdatesPerFrame,
    int WarmupFrames,
    int MeasuredFrames,
    int Depth,
    uint Seed,
    int CandidatesPerPass,
    int ReservoirUpdatesPerPass,
    int ProgramTransformCount,
    int ProgramMode,
    int ReadbackSplats,
    string ShaderPath,
    string OutputDirectory,
    string? FlamePath,
    string? ReferencePpmPath,
    string? ProgramFlamePath,
    int HistogramSamples,
    int HistogramBurnIn,
    int HistogramWidth,
    int HistogramHeight,
    Vector4 HistogramBounds,
    bool VisualParity,
    int VisualParityReferenceSamples,
    IReadOnlyList<VisualParityView> VisualParityViews,
    string? VisualParityImageDirectory,
    string VisualParityImagePrefix)
{
    public static ReceiptOptions Parse(string[] args)
    {
        var options = new ReceiptOptions(
            2_000_000,
            2_000_000,
            30,
            120,
            8,
            0xA17EA11u,
            2,
            50_000,
            0,
            0,
            64,
            Path.Combine("src", "Aquarium.Engine", "Render", "Shaders", "D3D12FractalReservoirCompute.hlsl"),
            Path.Combine("artifacts", "fractal-splat-receipts"),
            null,
            null,
            null,
            8192,
            64,
            64,
            64,
            new Vector4(-8.0f, -8.0f, 8.0f, 8.0f),
            false,
            1_000_000,
            [],
            null,
            "visual-parity");
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            string Next() => index + 1 < args.Length ? args[++index] : throw new ArgumentException($"Missing value for {arg}.");
            options = arg switch
            {
                "--splats" => options with { SplatCount = int.Parse(Next()) },
                "--splat-updates" => options with { SplatUpdatesPerFrame = int.Parse(Next()) },
                "--warmup" => options with { WarmupFrames = int.Parse(Next()) },
                "--frames" => options with { MeasuredFrames = int.Parse(Next()) },
                "--depth" => options with { Depth = int.Parse(Next()) },
                "--seed" => options with { Seed = ParseUInt32(Next()) },
                "--candidates" => options with { CandidatesPerPass = int.Parse(Next()) },
                "--reservoir-updates" => options with { ReservoirUpdatesPerPass = int.Parse(Next()) },
                "--program-transforms" => options with { ProgramTransformCount = int.Parse(Next()), ProgramMode = 1 },
                "--program-flame" => options with { ProgramFlamePath = Next(), ProgramMode = 2 },
                "--readback-splats" => options with { ReadbackSplats = int.Parse(Next()) },
                "--shader" => options with { ShaderPath = Next() },
                "--out" => options with { OutputDirectory = Next() },
                "--flame" => options with { FlamePath = Next() },
                "--reference-ppm" => options with { ReferencePpmPath = Next() },
                "--histogram-samples" => options with { HistogramSamples = int.Parse(Next()) },
                "--histogram-burn-in" => options with { HistogramBurnIn = int.Parse(Next()) },
                "--histogram-size" => ParseHistogramSize(options, Next()),
                "--histogram-bounds" => ParseHistogramBounds(options, Next()),
                "--visual-parity" => options with { VisualParity = true },
                "--visual-parity-reference-samples" => options with { VisualParityReferenceSamples = int.Parse(Next()) },
                "--visual-parity-view" => AddVisualParityView(options, Next()),
                "--visual-parity-image-dir" => options with { VisualParityImageDirectory = Next() },
                "--visual-parity-image-prefix" => options with { VisualParityImagePrefix = Next() },
                _ => throw new ArgumentException($"Unknown receipt option: {arg}"),
            };
        }

        if (options.ReferencePpmPath is not null)
        {
            if (!File.Exists(options.ReferencePpmPath))
            {
                throw new FileNotFoundException("Reference PPM file was not found.", options.ReferencePpmPath);
            }

            return options;
        }

        if (options.FlamePath is not null)
        {
            if (!File.Exists(options.FlamePath))
            {
                throw new FileNotFoundException("Receipt flame file was not found.", options.FlamePath);
            }

            if (options.HistogramSamples <= 0 || options.HistogramBurnIn < 0 || options.HistogramWidth <= 0 || options.HistogramHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(args), "Flame histogram samples and dimensions must be positive; burn-in must not be negative.");
            }

            return options;
        }

        if (options.ProgramFlamePath is not null && !File.Exists(options.ProgramFlamePath))
        {
            throw new FileNotFoundException("Receipt program flame file was not found.", options.ProgramFlamePath);
        }

        options = options.ProgramFlamePath is not null && options.ProgramTransformCount <= 0
            ? options with { ProgramTransformCount = int.MaxValue, ProgramMode = 2 }
            : options;
        options = options.ProgramMode == 0 && options.ProgramTransformCount > 0
            ? options with { ProgramMode = 1 }
            : options;

        if (!File.Exists(options.ShaderPath))
        {
            throw new FileNotFoundException("Receipt shader file was not found.", options.ShaderPath);
        }

        if (options.VisualParity && options.ProgramFlamePath is null)
        {
            throw new ArgumentException("Visual parity requires --program-flame so the GPU distribution has a CPU flame oracle.");
        }

        if (options.SplatCount <= 0 || options.SplatUpdatesPerFrame <= 0 || options.SplatUpdatesPerFrame > options.SplatCount || options.WarmupFrames < 0 || options.MeasuredFrames <= 0 || options.Depth <= 0 || options.CandidatesPerPass <= 0 || options.ReservoirUpdatesPerPass <= 0 || options.ReservoirUpdatesPerPass > options.SplatCount || options.ProgramTransformCount < 0 || options.ProgramMode < 0 || options.ProgramMode > 2 || options.ReadbackSplats < 0 || options.VisualParityReferenceSamples <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(args), "Splats, splat updates, frames, depth, candidates, and reservoir updates must be positive; splat/reservoir updates must not exceed splats; program transforms, warmup, and readback must not be negative.");
        }
        return options;
    }

    private static ReceiptOptions ParseHistogramSize(ReceiptOptions options, string value)
    {
        var parts = value.Split('x', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException("Histogram size must be WIDTHxHEIGHT.");
        }

        return options with { HistogramWidth = int.Parse(parts[0]), HistogramHeight = int.Parse(parts[1]) };
    }

    private static ReceiptOptions ParseHistogramBounds(ReceiptOptions options, string value)
    {
        return options with { HistogramBounds = ParseBounds(value, "Histogram bounds") };
    }

    private static ReceiptOptions AddVisualParityView(ReceiptOptions options, string value)
    {
        var separator = value.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0 || separator == value.Length - 1)
        {
            throw new ArgumentException("Visual parity view must be name:minX,minY,maxX,maxY.");
        }

        var name = value[..separator];
        var bounds = ParseBounds(value[(separator + 1)..], "Visual parity view bounds");
        return options with { VisualParityViews = [.. options.VisualParityViews, new VisualParityView(name, bounds)] };
    }

    private static Vector4 ParseBounds(string value, string label)
    {
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4)
        {
            throw new ArgumentException($"{label} must be minX,minY,maxX,maxY.");
        }

        return new Vector4(
            float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
    }

    private static uint ParseUInt32(string value)
    {
        return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? Convert.ToUInt32(value[2..], 16)
            : uint.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}

internal sealed record FlameHistogramReceipt(
    string SourcePath,
    string FlameName,
    int TransformCount,
    int SampleCount,
    int BurnIn,
    int Width,
    int Height,
    float[] Bounds,
    int HitCount,
    int OccupiedBins,
    uint Checksum);

internal sealed record ReferencePpmReceipt(
    string SourcePath,
    int Width,
    int Height,
    int MaxChannelValue,
    int PixelCount,
    int NonBlackPixelCount,
    ulong RgbChecksum,
    ulong LuminanceChecksum);

internal sealed record FlameImportReportReceipt(
    string SourcePath,
    FractalFlameImportReport Report);

internal readonly record struct VisualParityView(string Name, Vector4 Bounds);

internal sealed record VisualParityReceipt(
    int ComparedSamples,
    int ReferenceSamples,
    int Width,
    int Height,
    float[] Bounds,
    int ReferenceHitCount,
    int GpuHitCount,
    int ReferenceOccupiedBins,
    int GpuOccupiedBins,
    int SharedOccupiedBins,
    double OccupancyOverlapPercent,
    double L1Distance,
    double Rmse,
    double CosineSimilarity,
    double DistributionScorePercent,
    IReadOnlyList<VisualParityViewReceipt> Views);

internal sealed record VisualParityViewReceipt(
    string Name,
    float[] Bounds,
    int ReferenceHitCount,
    int GpuHitCount,
    int ReferenceOccupiedBins,
    int GpuOccupiedBins,
    int SharedOccupiedBins,
    int StarvedReferenceBins,
    double OccupancyOverlapPercent,
    double UnderrepresentedMassPercent,
    double OversampledMassPercent,
    double L1Distance,
    double Rmse,
    double CosineSimilarity,
    double DistributionScorePercent);

internal readonly record struct HistogramComparison(
    int ReferenceOccupiedBins,
    int GpuOccupiedBins,
    int SharedOccupiedBins,
    int StarvedReferenceBins,
    double OccupancyOverlapPercent,
    double UnderrepresentedMassPercent,
    double OversampledMassPercent,
    double L1Distance,
    double Rmse,
    double CosineSimilarity,
    double DistributionScorePercent);

internal sealed record GpuFractalSplatReceipt(
    string Adapter,
    int SplatCount,
    int SdfReservoirCount,
    int PbrReservoirCount,
    int RadiosityReservoirCount,
    int ProgramTransformCount,
    int ProgramMode,
    int SplatUpdatesPerFrame,
    int CandidatesPerPass,
    int ReservoirUpdatesPerPass,
    double ReservoirFullCoverageFrames,
    int MeasuredFrames,
    int BytesPerSplat,
    int BytesPerReservoir,
    double GpuMillisecondsPerFrame,
    double GpuEquivalentFps,
    double GpuSplatsPerSecond,
    double GpuReservoirCandidatesPerSecond,
    double CpuSubmitAndWaitMillisecondsPerFrame,
    ulong ReadbackChecksum,
    VisualParityReceipt? VisualParity);
