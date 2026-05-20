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

struct SdfEnvelopeReservoir
{
    float4 centerRadius;
    float4 radiiFalloff;
    float4 weightTargetCount;
    float4 validation;
};

struct PbrMaterialReservoir
{
    float4 baseColorRoughMetal;
    float4 normalVariance;
    float4 weightTargetCount;
    float4 validation;
};

struct RadiosityReservoir
{
    float4 radianceDistance;
    float4 directionOcclusion;
    float4 weightTargetCount;
    float4 validation;
};

StructuredBuffer<FractalSdfSplat> fractalSdfSplats : register(t38);
StructuredBuffer<SdfEnvelopeReservoir> sdfEnvelopeReservoirs : register(t39);
StructuredBuffer<PbrMaterialReservoir> pbrMaterialReservoirs : register(t40);
StructuredBuffer<RadiosityReservoir> radiosityReservoirs : register(t41);

struct FractalSplatVertexOut
{
    float4 position : SV_Position;
    nointerpolation uint splatIndex : TEXCOORD0;
    float2 quad : TEXCOORD1;
    float travel : TEXCOORD2;
    float reservoirConfidence : TEXCOORD3;
    float radiosityEnergy : TEXCOORD4;
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
    SdfEnvelopeReservoir sdf = sdfEnvelopeReservoirs[splatIndex];
    bool sdfResident = sdf.weightTargetCount.z > 0.5;
    float4 centerRadius = sdfResident ? sdf.centerRadius : splat.centerRadius;
    float3 forward;
    float3 right;
    float3 up;
    cameraBasis(cameraPosition, cameraTarget, forward, right, up);
    float worldScale = max(viewRadius * 0.16, 0.05);
    float3 center = cameraTarget + centerRadius.xyz * worldScale;
    float3 delta = center - cameraPosition;
    float z = max(dot(delta, forward), 0.0001);
    float2 projected = float2(dot(delta, right), dot(delta, up)) / z * 1.6;
    float clipAspect = resolution.x / max(resolution.y, 1.0);
    float boundRadius = max(centerRadius.w * worldScale * 1.8, 0.002 * viewRadius);
    float projectedRadius = boundRadius / z * 1.6 + 0.002;
    float2 clipCenter = float2(projected.x / clipAspect, projected.y);
    float2 clipRadius = float2(projectedRadius / clipAspect, projectedRadius);

    FractalSplatVertexOut output;
    output.position = float4(clipCenter + corners[vertexId] * clipRadius, 0.0, 1.0);
    output.splatIndex = splatIndex;
    output.quad = corners[vertexId];
    output.travel = z;
    output.reservoirConfidence = sdfResident ? saturate(sdf.validation.x) : saturate(splat.materialConfidence.w) * 0.25;
    output.radiosityEnergy = 0.0;
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
    SdfEnvelopeReservoir sdf = sdfEnvelopeReservoirs[input.splatIndex];
    PbrMaterialReservoir pbr = pbrMaterialReservoirs[input.splatIndex];
    RadiosityReservoir radiosity = radiosityReservoirs[input.splatIndex];

    bool sdfResident = sdf.weightTargetCount.z > 0.5;
    bool pbrResident = pbr.weightTargetCount.z > 0.5;
    bool radiosityResident = radiosity.weightTargetCount.z > 0.5;
    float falloff = sdfResident ? sdf.radiiFalloff.w : splat.radiiFalloff.w;
    float weight = pow(saturate(1.0 - r2), max(falloff * 0.35, 0.5));
    float material = saturate(splat.materialConfidence.x);
    float3 fallbackColor = lerp(float3(0.05, 0.42, 1.0), float3(1.0, 0.72, 0.18), material);
    float3 pbrColor = saturate(pbr.baseColorRoughMetal.rgb);
    float3 radiosityColor = saturate(radiosity.radianceDistance.rgb);
    float reservoirConfidence = min(
        sdfResident ? saturate(sdf.validation.x) : 0.0,
        min(
            pbrResident ? saturate(pbr.validation.x) : 0.0,
            radiosityResident ? saturate(radiosity.validation.x) : 0.0));
    float3 color = pbrResident ? pbrColor : fallbackColor;
    color += radiosityResident ? radiosityColor * (0.18 + reservoirConfidence * 0.35) : 0.0;
    float opacity = saturate(weight * (0.18 + reservoirConfidence * 0.28));

    SceneOut output;
    output.colorTravel = float4(color * opacity, min(input.travel, farDistance + 1.0));
    output.metadata = float4(FIELD_ID_FRACTAL_SPLAT_BASE, normalize(float3(input.quad, 1.0)));
    output.control = float4(opacity, reservoirConfidence, saturate((sdfResident ? sdf.centerRadius.w : splat.centerRadius.w) * 40.0), 0.0);
    output.reservoirGuide = float4(
        sdfResident ? saturate(sdf.validation.x) : 0.0,
        pbrResident ? saturate(pbr.validation.x) : 0.0,
        radiosityResident ? saturate(radiosity.validation.x) : 0.0,
        reservoirConfidence);
    output.depth = saturate(input.travel / max(farDistance, 0.0001));
    return output;
}
