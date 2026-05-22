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
};

Texture2D<float4> heightFieldTexture : register(t0);
SamplerState linearSampler : register(s0);

static const float FIELD_ID_HEIGHT_FIELD = 4.0;
static const float HEIGHT_FIELD_TEXEL_COUNT = 128.0;

struct VertexOut
{
    float4 position : SV_Position;
    float2 uv : TEXCOORD0;
};

struct SceneOut
{
    float4 colorTravel : SV_Target0;
    float4 metadata : SV_Target1;
    float4 control : SV_Target2;
    float4 reservoirGuide : SV_Target3;
    float depth : SV_Depth;
};

struct RayMarchResult
{
    float3 color;
    float travel;
    float fieldId;
    float3 normal;
    float coverage;
    float stepCount;
};

VertexOut FullscreenTriangleVS(uint vertexId : SV_VertexID)
{
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    VertexOut output;
    output.position = float4(uv * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
    output.uv = uv;
    return output;
}

void cameraBasis(float3 camera, float3 target, out float3 forward, out float3 right, out float3 up)
{
    forward = normalize(target - camera);
    right = normalize(cross(forward, float3(0.0, 0.0, 1.0)));
    up = cross(right, forward);
}

float3 rayDirectionForPixel(float2 pixel, float2 jitter, float3 camera, float3 target)
{
    float2 ndc = ((pixel + jitter) * 2.0 - resolution) / resolution.y;
    float3 forward;
    float3 right;
    float3 up;
    cameraBasis(camera, target, forward, right, up);
    return normalize(forward * 1.6 + right * ndc.x + up * ndc.y);
}

float2 viewLocal(float2 p)
{
    return (p - viewCenter) / max(viewRadius, 0.001);
}

float2 viewUv(float2 p)
{
    return viewLocal(p) * 0.5 + 0.5;
}

float terrainHeight(float2 p)
{
    return heightFieldTexture.SampleLevel(linearSampler, saturate(viewUv(p)), 0.0).r;
}

float2 terrainGradient(float2 p)
{
    float2 uv = saturate(viewUv(p));
    float2 texel = 1.0 / HEIGHT_FIELD_TEXEL_COUNT;
    float texelWorld = max((viewRadius * 2.0) / HEIGHT_FIELD_TEXEL_COUNT, 0.001);

    float hLeft = heightFieldTexture.SampleLevel(linearSampler, uv - float2(texel.x, 0.0), 0.0).r;
    float hRight = heightFieldTexture.SampleLevel(linearSampler, uv + float2(texel.x, 0.0), 0.0).r;
    float hDown = heightFieldTexture.SampleLevel(linearSampler, uv - float2(0.0, texel.y), 0.0).r;
    float hUp = heightFieldTexture.SampleLevel(linearSampler, uv + float2(0.0, texel.y), 0.0).r;
    return float2(hRight - hLeft, hUp - hDown) / (texelWorld * 2.0);
}

bool traceHeightFieldSurfaceDirect(float3 origin, float3 direction, float intervalStart, float intervalEnd, out float3 hitPosition, out float travel)
{
    travel = max(intervalStart, 0.0);
    float previousTravel = travel;
    hitPosition = origin + direction * travel;
    float previousGap = hitPosition.z - terrainHeight(hitPosition.xy);

    [loop]
    for (int stepIndex = 0; stepIndex < 96; stepIndex++)
    {
        hitPosition = origin + direction * travel;
        float2 local = viewLocal(hitPosition.xy);
        if (length(local) > 1.08 && hitPosition.z < 4.0)
        {
            return false;
        }

        float gap = hitPosition.z - terrainHeight(hitPosition.xy);
        float hitEpsilon = max(0.002, travel * 0.00035);
        if (length(local) <= 1.0 && (abs(gap) <= hitEpsilon || (previousGap > 0.0 && gap <= 0.0)))
        {
            float alpha = previousGap / max(previousGap - gap, 0.0001);
            travel = lerp(previousTravel, travel, saturate(alpha));
            hitPosition = origin + direction * travel;
            return travel > intervalStart && travel < intervalEnd && travel < farDistance;
        }

        float2 slope = terrainGradient(hitPosition.xy);
        float terrainRate = abs(direction.z - dot(slope, direction.xy));
        float terrainStep = gap > 0.0 ? gap / max(terrainRate, 0.22) : 0.026;
        terrainStep = min(terrainStep * 0.62, max(viewRadius * 0.08, 0.026));
        previousTravel = travel;
        previousGap = gap;
        travel += max(terrainStep, 0.026);
        if (travel > intervalEnd || travel > farDistance)
        {
            return false;
        }
    }

    return false;
}

float hash31(float3 p)
{
    return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453);
}

float valueNoise3(float3 p)
{
    float3 cell = floor(p);
    float3 local = frac(p);
    local = local * local * (3.0 - 2.0 * local);

    float c000 = hash31(cell);
    float c100 = hash31(cell + float3(1.0, 0.0, 0.0));
    float c010 = hash31(cell + float3(0.0, 1.0, 0.0));
    float c110 = hash31(cell + float3(1.0, 1.0, 0.0));
    float c001 = hash31(cell + float3(0.0, 0.0, 1.0));
    float c101 = hash31(cell + float3(1.0, 0.0, 1.0));
    float c011 = hash31(cell + float3(0.0, 1.0, 1.0));
    float c111 = hash31(cell + float3(1.0, 1.0, 1.0));

    float x00 = lerp(c000, c100, local.x);
    float x10 = lerp(c010, c110, local.x);
    float x01 = lerp(c001, c101, local.x);
    float x11 = lerp(c011, c111, local.x);
    return lerp(lerp(x00, x10, local.y), lerp(x01, x11, local.y), local.z);
}

float fractalNoise3(float3 p)
{
    float sum = 0.0;
    float amplitude = 0.5;
    [unroll]
    for (int octave = 0; octave < 4; octave++)
    {
        sum += valueNoise3(p) * amplitude;
        p = p * 2.03 + float3(5.7, 2.1, 9.3);
        amplitude *= 0.52;
    }

    return sum;
}

float sdSegment2(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / max(dot(ba, ba), 0.0001));
    return length(pa - ba * h);
}

float lineGlow(float2 p, float2 a, float2 b, float thickness)
{
    return exp(-sdSegment2(p, a, b) / max(thickness, 0.0001));
}

float diamondOutline(float2 p, float2 center, float2 radius, float thickness)
{
    float2 q = abs(p - center) / max(radius, 0.001);
    float d = abs(q.x + q.y - 1.0) * min(radius.x, radius.y);
    return exp(-d / max(thickness, 0.0001));
}

float verticalPane(float2 p, float x, float z0, float z1, float width)
{
    float heightMask = smoothstep(z0 - 0.02, z0 + 0.02, p.y) * smoothstep(z1 + 0.02, z1 - 0.02, p.y);
    return exp(-abs(p.x - x) / max(width, 0.0001)) * heightMask;
}

float reedSilhouette(float2 p, float x, float tilt, float height)
{
    float2 a = float2(x, -0.42);
    float2 b = float2(x + tilt, -0.42 + height);
    return exp(-sdSegment2(p, a, b) / 0.004) * smoothstep(0.32, -0.28, p.y);
}

float3 cathedralRadiance(float3 direction)
{
    float2 p = float2(direction.x * 1.22, direction.z);
    float verticalMask = smoothstep(-0.22, 0.08, p.y) * smoothstep(0.98, 0.68, p.y);
    float horizonMask = exp(-abs(p.y + 0.115) * 42.0);

    float centerAxis = exp(-abs(p.x) / 0.004) * smoothstep(-0.22, 0.72, p.y);
    float diamonds = 0.0;
    diamonds += diamondOutline(p, float2(0.0, 0.00), float2(0.15, 0.12), 0.004);
    diamonds += diamondOutline(p, float2(0.0, 0.20), float2(0.19, 0.15), 0.0045);
    diamonds += diamondOutline(p, float2(0.0, 0.43), float2(0.24, 0.18), 0.005);
    diamonds += diamondOutline(p, float2(0.0, 0.70), float2(0.15, 0.15), 0.004);

    float ribs = 0.0;
    ribs += lineGlow(p, float2(-0.15, -0.05), float2(0.0, 0.11), 0.006);
    ribs += lineGlow(p, float2(0.15, -0.05), float2(0.0, 0.11), 0.006);

    float panes = smoothstep(0.07, 0.0, abs(p.x)) * verticalMask * 0.35;
    panes += smoothstep(0.23, 0.0, abs(p.x)) * smoothstep(-0.18, 0.14, p.y) * smoothstep(0.62, 0.24, p.y) * 0.20;

    float sideGlass = 0.0;
    [unroll]
    for (int index = 0; index < 8; index++)
    {
        float t = (float)index;
        float spread = 0.30 + t * 0.075;
        float height = 0.14 + t * 0.07;
        float fade = 1.0 - t * 0.075;
        sideGlass += verticalPane(p, -spread, -0.18, height, 0.0025 + t * 0.00045) * fade;
        sideGlass += verticalPane(p, spread, -0.18, height, 0.0025 + t * 0.00045) * fade;
        sideGlass += lineGlow(p, float2(-spread, -0.16), float2(-spread * 0.74, height - 0.08), 0.0028) * fade * 0.18;
        sideGlass += lineGlow(p, float2(spread, -0.16), float2(spread * 0.74, height - 0.08), 0.0028) * fade * 0.18;
    }

    float patchField = max(0.0, abs(terrainHeight(float2(direction.x * 8.0, direction.z * 5.8 + 1.1))) - 0.022);
    float patchSpark = smoothstep(0.002, 0.018, patchField) * smoothstep(-0.2, 0.72, p.y);
    float dustNoise = fractalNoise3(float3(p * 16.0, 9.0));
    float goldDust = pow(saturate(dustNoise - 0.48), 7.0) * (sideGlass + patchSpark) * 0.9;

    float sideMagenta = (verticalPane(p, -0.46, -0.18, 0.18, 0.02) + verticalPane(p, 0.46, -0.18, 0.18, 0.02)) * 0.45;
    float cyanCore = centerAxis * 1.35 + diamonds * 0.86 + ribs * 0.30 + panes * 0.40;
    float3 color = float3(0.18, 1.35, 2.05) * cyanCore;
    color += float3(0.05, 0.52, 0.78) * sideGlass * 0.025;
    color += float3(1.25, 0.12, 0.74) * (sideMagenta * 0.20 + horizonMask * 0.10);
    color += float3(1.0, 0.68, 0.24) * goldDust;
    return color;
}

float3 screenCathedralRadiance(float2 uv)
{
    float2 p = float2(uv.x - 0.5, uv.y);
    float3 color = 0.0;

    float horizon = exp(-abs(p.y - 0.345) * 82.0);
    color += float3(1.12, 0.07, 0.68) * horizon * smoothstep(0.48, 0.08, abs(p.x)) * 0.42;

    float axis = exp(-abs(p.x) / 0.0018) * smoothstep(0.16, 0.38, p.y) * smoothstep(1.03, 0.52, p.y);
    float diamond = 0.0;
    diamond += diamondOutline(p, float2(0.0, 0.39), float2(0.070, 0.075), 0.0022);
    diamond += diamondOutline(p, float2(0.0, 0.52), float2(0.082, 0.085), 0.0023);
    diamond += diamondOutline(p, float2(0.0, 0.66), float2(0.115, 0.105), 0.0024);
    diamond += diamondOutline(p, float2(0.0, 0.80), float2(0.070, 0.078), 0.0020);
    color += float3(0.18, 1.55, 2.3) * (axis * 0.70 + diamond * 0.65);

    float paneFill = smoothstep(0.11, 0.0, abs(p.x)) * smoothstep(0.30, 0.48, p.y) * smoothstep(0.90, 0.58, p.y);
    color += float3(0.06, 0.55, 0.72) * paneFill * 0.18;

    [unroll]
    for (int index = 0; index < 10; index++)
    {
        float t = (float)index;
        float x = 0.105 + t * 0.034;
        float top = 0.52 + t * 0.032;
        float fade = saturate(1.0 - t * 0.055);
        float left = verticalPane(p, -x, 0.31, top, 0.0016) * fade;
        float right = verticalPane(p, x, 0.31, top, 0.0016) * fade;
        color += float3(0.05, 0.52, 0.70) * (left + right) * 0.28;
        color += float3(1.0, 0.10, 0.68) * (left + right) * smoothstep(0.14, 0.34, abs(p.x)) * 0.20;
    }

    float magentaGlass = 0.0;
    magentaGlass += lineGlow(p, float2(-0.36, 0.31), float2(-0.22, 0.43), 0.0022);
    magentaGlass += lineGlow(p, float2(0.36, 0.31), float2(0.22, 0.43), 0.0022);
    magentaGlass += lineGlow(p, float2(-0.27, 0.38), float2(-0.16, 0.50), 0.0019);
    magentaGlass += lineGlow(p, float2(0.27, 0.38), float2(0.16, 0.50), 0.0019);
    color += float3(1.15, 0.12, 0.72) * magentaGlass * 0.26;

    float ripple = exp(-abs(p.x) / 0.028) * smoothstep(0.31, 0.16, p.y) * smoothstep(0.02, 0.13, p.y);
    ripple *= 0.42 + 0.58 * pow(saturate(fractalNoise3(float3(p * 38.0, 2.0)) - 0.20), 2.0);
    color += float3(0.11, 1.25, 2.2) * ripple * 0.46;

    float goldNoise = fractalNoise3(float3(p * 54.0, 5.0));
    float dust = pow(saturate(goldNoise - 0.56), 12.0) * smoothstep(0.25, 0.42, p.y) * smoothstep(0.82, 0.52, p.y);
    color += float3(1.0, 0.72, 0.28) * dust * 0.38;
    return color;
}

float3 backgroundRadiance(float3 direction)
{
    float horizon = smoothstep(-0.28, 0.34, direction.z);
    float verticalAxis = pow(saturate(1.0 - abs(direction.x) * 7.8), 3.0) * smoothstep(-0.18, 0.76, direction.z);
    float hall = pow(saturate(1.0 - abs(abs(direction.x) - 0.24) * 11.0), 2.0) * smoothstep(-0.10, 0.62, direction.z);
    float fog = pow(saturate(fractalNoise3(direction * 4.1 + float3(0.0, timeSeconds * 0.015, 0.0)) - 0.18), 2.2);
    float2 hallPoint = float2(direction.x * 8.0, direction.z * 5.8 + 1.1);
    float cathedralField = max(0.0, abs(terrainHeight(hallPoint)) - 0.022);
    float cathedralPane = smoothstep(0.001, 0.016, cathedralField) * smoothstep(-0.22, 0.74, direction.z);
    float verticalShard = pow(saturate(1.0 - abs(frac((direction.x + 0.5) * 28.0) - 0.5) * 2.0), 22.0);
    float horizonLine = exp(-abs(direction.z + 0.095) * 34.0);
    float runeDust = pow(saturate(fractalNoise3(float3(hallPoint * 4.0, 3.0)) - 0.54), 5.0) * cathedralPane;

    float3 voidColor = lerp(float3(0.001, 0.006, 0.009), float3(0.018, 0.055, 0.062), horizon);
    float3 cyan = float3(0.50, 1.55, 2.25) * verticalAxis * 0.62;
    float3 magenta = float3(1.25, 0.12, 0.82) * (hall * 0.16 + horizonLine * 0.11);
    float3 mist = float3(0.12, 0.36, 0.38) * fog * (0.18 + horizon * 0.24);
    float3 cathedral = float3(0.10, 0.66, 0.88) * cathedralPane * (0.22 + verticalShard * 0.35);
    cathedral += float3(1.15, 0.16, 0.72) * cathedralPane * smoothstep(0.12, 0.52, abs(direction.x)) * 0.16;
    cathedral += float3(1.0, 0.74, 0.28) * runeDust * 0.22;
    return voidColor + cyan + magenta + mist + cathedral + cathedralRadiance(direction) * 0.08;
}

float3 surfaceMirrorRadiance(float3 p, float3 direction, out float3 normal)
{
    float2 gradient = terrainGradient(p.xy);
    normal = normalize(float3(-gradient.x, -gradient.y, 1.0));
    float3 reflectionDirection = reflect(direction, normal);
    float3 reflected = backgroundRadiance(reflectionDirection);

    float spineReflection = exp(-abs(p.x) * 2.7) * smoothstep(3.4, -2.6, p.y);
    float magentaLeft = exp(-abs(p.x + 2.35) * 3.2) * smoothstep(2.4, -2.2, p.y);
    float magentaRight = exp(-abs(p.x - 2.35) * 3.2) * smoothstep(2.4, -2.2, p.y);
    float rippleSparkle = pow(saturate(fractalNoise3(float3(p.xy * 5.2, timeSeconds * 0.22)) - 0.47), 5.0);
    float waterline = exp(-abs(p.y - 0.95) * 2.1) * smoothstep(4.8, 0.2, abs(p.x));
    float reedMask = 0.0;
    reedMask += reedSilhouette(float2(p.x * 0.22 - 1.18, p.y * 0.16 - 0.34), -0.05, -0.10, 0.45);
    reedMask += reedSilhouette(float2(p.x * 0.20 + 1.18, p.y * 0.16 - 0.34), 0.02, 0.08, 0.36);
    float fresnel = pow(1.0 - saturate(dot(normal, -direction)), 3.0);

    float3 baseWater = float3(0.002, 0.014, 0.017);
    float3 cyan = float3(0.35, 1.40, 2.35) * spineReflection * (0.26 + rippleSparkle * 0.32);
    float3 magenta = float3(1.10, 0.08, 0.74) * ((magentaLeft + magentaRight) * 0.08 + waterline * 0.055);
    float3 gold = float3(1.0, 0.72, 0.26) * (rippleSparkle * 0.032 + waterline * rippleSparkle * 0.08);
    float3 water = baseWater + reflected * (0.18 + fresnel * 0.32) + cyan + magenta + gold;
    return lerp(water, float3(0.0, 0.002, 0.002), saturate(reedMask));
}

RayMarchResult traverseRay(float3 origin, float3 direction)
{
    RayMarchResult result;
    result.color = backgroundRadiance(direction);
    result.travel = farDistance + 1.0;
    result.fieldId = 0.0;
    result.normal = 0.0;
    result.coverage = 0.0;
    result.stepCount = 0.0;

    float3 surfacePosition;
    float surfaceTravel;
    bool surfaceHit = traceHeightFieldSurfaceDirect(origin, direction, 0.0, farDistance, surfacePosition, surfaceTravel);
    if (surfaceHit)
    {
        float3 surfaceNormal;
        result.color = surfaceMirrorRadiance(surfacePosition, direction, surfaceNormal);
        result.travel = surfaceTravel;
        result.fieldId = FIELD_ID_HEIGHT_FIELD;
        result.normal = surfaceNormal;
        result.coverage = 1.0;
    }

    return result;
}

SceneOut D3D12ScenePS(VertexOut input)
{
    float2 screenUv = float2(input.uv.x, 1.0 - input.uv.y);
    float2 pixel = screenUv * resolution;
    float3 rayDirection = rayDirectionForPixel(pixel, jitterPixels, cameraPosition, cameraTarget);

    RayMarchResult result = traverseRay(cameraPosition, rayDirection);
    result.color += screenCathedralRadiance(screenUv);

    SceneOut output;
    output.colorTravel = float4(result.color, min(result.travel, farDistance + 1.0));
    output.metadata = float4(result.fieldId, result.normal);
    output.control = float4(result.coverage, result.stepCount / 96.0, 0.0, 0.0);
    output.reservoirGuide = float4(1.0, 0.0, 1.0, 0.0);
    output.depth = saturate(result.travel / max(farDistance, 0.001));
    return output;
}
