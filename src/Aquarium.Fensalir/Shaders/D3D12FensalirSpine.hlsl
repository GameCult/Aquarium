static const int SDF_INDEX = 0;

#define SDF_TRACE_STEPS 192
#define SDF_TRACE_STEP_SCALE 0.34
#define SDF_TRACE_MAX_STEP_RADIUS_SCALE 0.018

#include "D3D12SdfCommon.hlsli"
#include "D3D12SdfMath.hlsli"

float sdRoundBox(float3 p, float3 b, float r)
{
    float3 q = abs(p) - b;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0) - r;
}

float sdCappedCylinderZ(float3 p, float radius, float halfHeight)
{
    float2 d = abs(float2(length(p.xy), p.z)) - float2(radius, halfHeight);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

float sdDiamond(float3 p, float3 radius)
{
    float3 q = abs(p) / max(radius, 0.001);
    return (q.x + q.y + q.z - 1.0) * min(radius.x, min(radius.y, radius.z));
}

float fensalirSpineDistance(float3 local)
{
    float core = sdCappedCylinderZ(local - float3(0.0, 0.0, 0.18), 0.045, 2.75);
    float diamonds = 999.0;

    [unroll]
    for (int index = 0; index < 8; index++)
    {
        float t = (float)index;
        float z = -1.34 + t * 0.47;
        float pulse = 0.5 + 0.5 * sin(timeSeconds * 0.42 + t * 1.7);
        float side = lerp(0.22, 0.46, smoothstep(0.0, 7.0, t)) * lerp(0.92, 1.08, pulse);
        float3 q = local - float3(0.0, 0.0, z);
        q.xy = mul(float2x2(cos(t * 0.42), -sin(t * 0.42), sin(t * 0.42), cos(t * 0.42)), q.xy);
        diamonds = min(diamonds, sdDiamond(q, float3(side, side * 0.58, 0.24)));
    }

    float haloGlass = sdCappedCylinderZ(local - float3(0.0, 0.0, 0.1), 0.18, 2.45);
    return min(min(core, diamonds), haloGlass);
}

float fensalirPillarDistance(float3 local)
{
    float pillars = 999.0;
    [unroll]
    for (int index = 0; index < 4; index++)
    {
        float side = index < 2 ? -1.0 : 1.0;
        float rank = (float)(index % 2);
        float x = side * (1.1 + rank * 0.82);
        float y = 0.34 + rank * 0.42;
        float3 p = local - float3(x, y, 0.0);
        float lean = side * (0.08 + rank * 0.04);
        p.x -= p.z * lean;
        pillars = min(pillars, sdRoundBox(p, float3(0.028 + rank * 0.012, 0.055, 2.55), 0.018));
    }

    return pillars;
}

float fensalirReedDistance(float3 local)
{
    float reeds = 999.0;
    [unroll]
    for (int index = 0; index < 6; index++)
    {
        float side = index < 3 ? -1.0 : 1.0;
        float rank = (float)(index % 3);
        float x = side * (3.35 + rank * 0.32);
        float y = -1.76 + rank * 0.18;
        float height = 0.7 + rank * 0.18;
        float3 a = float3(x, y, -2.05);
        float3 b = float3(x + side * (0.18 + rank * 0.04), y + 0.08, -2.05 + height);
        reeds = min(reeds, sdCapsuleSegment(local, a, b, 0.018));
    }

    return reeds;
}

float sdfDistance(float3 p, int sdfIndex)
{
    SdfObject scene = sdfObjects[sdfIndex];
    float3 local = p - scene.centerRadius.xyz;
    float spine = fensalirSpineDistance(local);
    float pillars = fensalirPillarDistance(local);
    float reeds = fensalirReedDistance(local);
    return min(spine, min(pillars, reeds));
}

SdfSurface sdfSurface(float3 p, int sdfIndex)
{
    SdfObject scene = sdfObjects[sdfIndex];
    float3 local = p - scene.centerRadius.xyz;
    float spine = fensalirSpineDistance(local);
    float pillars = fensalirPillarDistance(local);
    float reeds = fensalirReedDistance(local);
    float nearest = min(spine, min(pillars, reeds));

    float rune = pow(saturate(1.0 - abs(frac(local.z * 3.7 + local.x * 0.31) - 0.5) * 16.0), 2.6);
    float coreGlow = pow(saturate(1.0 - length(local.xy) * 5.2), 2.0);
    float magentaEdge = pow(saturate(abs(local.x) - 0.78), 2.0) * 0.05;

    SdfSurface surface;
    surface.baseColor = float3(0.02, 0.07, 0.08);
    surface.metallic = 0.0;
    surface.roughness = 0.22;
    surface.emission = 0.0;
    surface.temporalDetail = 0.35 + rune * 0.35;
    surface.reservoirConfidence = 1.0;

    if (nearest == spine)
    {
        surface.baseColor = float3(0.35, 0.86, 0.98);
        surface.roughness = 0.06;
        surface.emission = float3(0.35, 1.55, 2.45) * (1.2 + coreGlow * 4.0 + rune * 1.8);
    }
    else if (nearest == pillars)
    {
        surface.baseColor = float3(0.018, 0.030, 0.034);
        surface.metallic = 0.35;
        surface.roughness = 0.34;
        surface.emission = float3(0.75, 0.03, 0.48) * (0.08 + magentaEdge);
    }
    else
    {
        surface.baseColor = float3(0.006, 0.018, 0.014);
        surface.roughness = 0.72;
        surface.emission = float3(0.03, 0.18, 0.13);
    }

    return surface;
}

float3 shadeSdf(float2 uv, float travel, float3 p, float3 normal, int sdfIndex, SdfSurface surface)
{
    float3 viewDirection = normalize(cameraPosition - p);
    float rim = pow(1.0 - saturate(dot(normal, viewDirection)), 3.1);
    float3 pbr = shadeSdfPbr(p, normal, surface);
    float3 cyanRim = float3(0.22, 1.25, 2.4) * rim * 0.75;
    return pbr + cyanRim;
}

#include "D3D12SdfProxy.hlsli"
