using Aquarium.Engine.Render;

namespace Aquarium.Fensalir;

public static class FensalirRenderPlan
{
    public const int SpineIndex = 0;
    public const int SdfObjectCount = 1;

    public static AquariumRenderPlan Create()
    {
        var app = new AquariumApp();
        app.Shaders
            .Core("D3D12HeightField.hlsl", ShaderPath("D3D12FensalirScene.hlsl"), "D3D12Post.hlsl")
            .SdfShader(ShaderPath("D3D12FensalirSpine.hlsl"));

        var heightField = app.RenderTargets.Create("height-field")
            .FixedSize(128, 128)
            .Format(RenderFormat.R16Float)
            .Register();
        var scene = app.RenderTargets.Hdr("scene");
        app.Cameras.Orbit("main");
        app.Graph.Pass("height-field").Fullscreen();
        app.Graph.Pass("scene").Fullscreen();
        app.Graph.Pass("sdf-proxies").Proxy();
        app.Features.Bloom(scene.Color);
        app.Features.Presentation(scene.Color);
        app.Features.DirectWriteOverlay();
        app.Debug.View("Height Field", heightField).View("Scene", scene.Color);
        return app.Plan;
    }

    private static string ShaderPath(string fileName)
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(FensalirRenderPlan).Assembly.Location)
            ?? AppContext.BaseDirectory;
        return Path.Combine(assemblyDirectory, "Render", "Shaders", fileName);
    }
}
