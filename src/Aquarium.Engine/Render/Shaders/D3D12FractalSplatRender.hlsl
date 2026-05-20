cbuffer AquariumFrame : register(b0)
{
    float2 resolution;
    float timeSeconds;
    float viewRadius;
    float3 cameraPosition;
    float farDistance;
    float3 cameraTarget;
    float sceneFlags;
    float2 viewCenter;
    float frameIndex;
    float previousTimeSeconds;
    float3 previousCameraPosition;
    float previousViewRadius;
    float3 previousCameraTarget;
    float previousSceneFlags;
    float2 previousViewCenter;
    float2 jitterPixels;
    float2 previousJitterPixels;
    float renderDebugMode;
    float exposure;
    float bloomIntensity;
    float bloomVeilIntensity;
    float4 cursorWorlds;
    float4 temporalGaussianInfo;
    float4 gpuFusionInfo;
    float4 fractalReservoirInfo;
};

struct FractalSdfSplat
{
    float4 centerRadius;
    float4 orientation;
    float4 radiiFalloff;
    float4 materialConfidence;
    float4 key;
};

StructuredBuffer<FractalSdfSplat> fractalSdfSplats : register(t25);

struct FractalSplatVertexOut
{
    float4 position : SV_Position;
    nointerpolation uint splatIndex : TEXCOORD0;
    float2 quad : TEXCOORD1;
    float travel : TEXCOORD2;
};

struct SceneOut
{
    float4 colorTravel : SV_Target0;
    float4 metadata : SV_Target1;
    float4 control : SV_Target2;
    float4 reservoirGuide : SV_Target3;
    float depth : SV_Depth;
};

static const float FIELD_ID_FRACTAL_SPLAT_BASE = 4000.0;

void cameraBasis(float3 camera, float3 target, out float3 forward, out float3 right, out float3 up)
{
    forward = normalize(target - camera);
    right = normalize(cross(forward, float3(0.0, 0.0, 1.0)));
    up = cross(right, forward);
}

uint VisibleSplatIndex(uint instanceId)
{
    uint splatCount = max((uint)fractalReservoirInfo.x, 1u);
    uint visibleCount = max((uint)fractalReservoirInfo.y, 1u);
    return min((instanceId * splatCount) / visibleCount, splatCount - 1u);
}

FractalSplatVertexOut D3D12FractalSplatVS(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    float2 corners[6] =
    {
        float2(-1.0, -1.0),
        float2(1.0, -1.0),
        float2(1.0, 1.0),
        float2(-1.0, -1.0),
        float2(1.0, 1.0),
        float2(-1.0, 1.0),
    };

    uint splatIndex = VisibleSplatIndex(instanceId);
    FractalSdfSplat splat = fractalSdfSplats[splatIndex];
    float3 forward;
    float3 right;
    float3 up;
    cameraBasis(cameraPosition, cameraTarget, forward, right, up);
    float worldScale = max(viewRadius * 0.16, 0.05);
    float3 center = cameraTarget + splat.centerRadius.xyz * worldScale;
    float3 delta = center - cameraPosition;
    float z = max(dot(delta, forward), 0.0001);
    float2 projected = float2(dot(delta, right), dot(delta, up)) / z * 1.6;
    float clipAspect = resolution.x / max(resolution.y, 1.0);
    float boundRadius = max(splat.centerRadius.w * worldScale * 1.8, 0.002 * viewRadius);
    float projectedRadius = boundRadius / z * 1.6 + 0.002;
    float2 clipCenter = float2(projected.x / clipAspect, projected.y);
    float2 clipRadius = float2(projectedRadius / clipAspect, projectedRadius);

    FractalSplatVertexOut output;
    output.position = float4(clipCenter + corners[vertexId] * clipRadius, 0.0, 1.0);
    output.splatIndex = splatIndex;
    output.quad = corners[vertexId];
    output.travel = z;
    return output;
}

SceneOut D3D12FractalSplatPS(FractalSplatVertexOut input)
{
    float r2 = dot(input.quad, input.quad);
    if (r2 >= 1.0)
    {
        discard;
    }

    FractalSdfSplat splat = fractalSdfSplats[input.splatIndex];
    float weight = pow(saturate(1.0 - r2), max(splat.radiiFalloff.w * 0.35, 0.5));
    float material = saturate(splat.materialConfidence.x);
    float3 color = lerp(float3(0.05, 0.42, 1.0), float3(1.0, 0.72, 0.18), material);
    float opacity = saturate(weight * 0.34);

    SceneOut output;
    output.colorTravel = float4(color * opacity, min(input.travel, farDistance + 1.0));
    output.metadata = float4(FIELD_ID_FRACTAL_SPLAT_BASE, normalize(float3(input.quad, 1.0)));
    output.control = float4(opacity, saturate(splat.materialConfidence.w), saturate(splat.centerRadius.w * 40.0), 0.0);
    output.reservoirGuide = float4(saturate(splat.materialConfidence.w), 0.0, 1.0, 0.0);
    output.depth = saturate(input.travel / max(farDistance, 0.0001));
    return output;
}
