import ShaderVariables;
import FragmentInput;
import Material;
import Lighting;


import ProwlVariables;


module ProwlShadow;


// Precomputed Poisson disk offsets for rotated PCF sampling
public static const float2 POISSON_DISK_8[8] = float2[](
    float2(-0.613392,  0.617481),
    float2( 0.170019, -0.040254),
    float2(-0.299417, -0.792901),
    float2( 0.645680, -0.530998),
    float2( 0.454148,  0.516511),
    float2(-0.507431,  0.281182),
    float2(-0.177186, -0.283153),
    float2( 0.100558,  0.765839)
);


public struct ProwlShadow : IShadow
{
    public LightType Type;
    public bool  ShadowEnabled;
    public int   ShadowSlot;         // -1 if no shadow slot assigned
    public float3 LightPosition;     // world-space position; needed for point shadow face selection
    public float ShadowBias;
    public float ShadowNormalBias;
    public float ShadowStrength;
    public float ShadowQuality;


    public float Sample(float3 worldPos, float3 worldNormal, float3 lightDir)
    {
        if (!ShadowEnabled) return 0.0;

        if (Type == LightType.Directional)
            return SampleDirectional(worldPos, worldNormal, lightDir);

        if (ShadowSlot < 0) return 0.0;

        if (Type == LightType.Point)
            return SamplePoint(worldPos, worldNormal, lightDir);

        return SampleSpot(worldPos, worldNormal, lightDir);
    }


    private float SampleDirectional(float3 worldPos, float3 worldNormal, float3 lightDir)
    {
        if (Cascades._Count == 0) return 0.0;

        float worldDistance = distance(worldPos, ProwlVariables::WorldSpaceCameraPos()) * 2.0;

        float4x4 cascadeMatrix;
        float4   cascadeParams;

        if      (Cascades._Count >= 1 && worldDistance <= Cascades._AtlasParams0.w) { cascadeMatrix = Cascades._ShadowMatrix0; cascadeParams = Cascades._AtlasParams0; }
        else if (Cascades._Count >= 2 && worldDistance <= Cascades._AtlasParams1.w) { cascadeMatrix = Cascades._ShadowMatrix1; cascadeParams = Cascades._AtlasParams1; }
        else if (Cascades._Count >= 3 && worldDistance <= Cascades._AtlasParams2.w) { cascadeMatrix = Cascades._ShadowMatrix2; cascadeParams = Cascades._AtlasParams2; }
        else if (Cascades._Count >= 4 && worldDistance <= Cascades._AtlasParams3.w) { cascadeMatrix = Cascades._ShadowMatrix3; cascadeParams = Cascades._AtlasParams3; }
        else
        {
            if      (Cascades._Count == 1) { cascadeMatrix = Cascades._ShadowMatrix0; cascadeParams = Cascades._AtlasParams0; }
            else if (Cascades._Count == 2) { cascadeMatrix = Cascades._ShadowMatrix1; cascadeParams = Cascades._AtlasParams1; }
            else if (Cascades._Count == 3) { cascadeMatrix = Cascades._ShadowMatrix2; cascadeParams = Cascades._AtlasParams2; }
            else                           { cascadeMatrix = Cascades._ShadowMatrix3; cascadeParams = Cascades._AtlasParams3; }
        }

        if (cascadeParams.z <= 0.0) return 0.0;

        float3 worldPosBiased = worldPos + normalize(worldNormal) * ShadowNormalBias;
        float4 lightSpacePos  = mul(cascadeMatrix, float4(worldPosBiased, 1.0));
        float3 projCoords     = lightSpacePos.xyz / lightSpacePos.w;
        projCoords = projCoords * 0.5 + 0.5;

        if (projCoords.z > 1.0 || any(projCoords.xy < 0.0) || any(projCoords.xy > 1.0))
            return 0.0;

        uint atlasW, atlasH, numMips;
        _ShadowAtlas.GetDimensions(0, atlasW, atlasH, numMips);
        float atlasSize = (float)atlasW;

        float2 atlasCoords, shadowMin, shadowMax;
        GetAtlasCoordinates(projCoords, cascadeParams, atlasSize, atlasCoords, shadowMin, shadowMax);

        float slopeBias      = CalculateSlopeBias(worldNormal, lightDir, ShadowBias);
        float texelWorldSize = ((cascadeParams.w * 4.0) / (cascadeParams.z * atlasSize)) * 8.0;
        float currentDepth   = projCoords.z - (slopeBias + texelWorldSize);

        return SampleShadowPCF(_ShadowAtlas, _ShadowAtlasSampler, atlasCoords, shadowMin, shadowMax,
                               currentDepth, ShadowQuality, ShadowStrength);
    }


    private float SamplePoint(float3 worldPos, float3 worldNormal, float3 lightDir)
    {
        float3 lightToFrag = worldPos - LightPosition;
        float3 absDir      = abs(lightToFrag);

        int faceIndex;
        if      (absDir.x >= absDir.y && absDir.x >= absDir.z) faceIndex = lightToFrag.x > 0.0 ? 0 : 1;
        else if (absDir.y >= absDir.x && absDir.y >= absDir.z) faceIndex = lightToFrag.y > 0.0 ? 2 : 3;
        else                                                    faceIndex = lightToFrag.z > 0.0 ? 4 : 5;

        int      idx          = ShadowSlot * 6 + faceIndex;
        float4x4 shadowMatrix = _PointShadowMatrices[idx];
        float4   faceParams   = _PointShadowFaceParams[idx];

        float3 worldPosBiased = worldPos + normalize(worldNormal) * ShadowNormalBias;
        float4 lightSpacePos  = mul(shadowMatrix, float4(worldPosBiased, 1.0));
        float3 projCoords     = lightSpacePos.xyz / lightSpacePos.w;
        projCoords = projCoords * 0.5 + 0.5;

        if (projCoords.z > 1.0 || any(projCoords.xy < 0.0) || any(projCoords.xy > 1.0))
            return 0.0;

        uint atlasW, atlasH, numMips;
        _ShadowAtlas.GetDimensions(0, atlasW, atlasH, numMips);
        float atlasSize = (float)atlasW;

        float2 atlasCoords, shadowMin, shadowMax;
        GetAtlasCoordinates(projCoords, faceParams, atlasSize, atlasCoords, shadowMin, shadowMax);

        float finalBias    = CalculateSlopeBias(worldNormal, normalize(lightToFrag), ShadowBias);
        float currentDepth = projCoords.z - finalBias;

        return SampleShadowPCF(_ShadowAtlas, _ShadowAtlasSampler, atlasCoords, shadowMin, shadowMax,
                               currentDepth, ShadowQuality, ShadowStrength);
    }


    private float SampleSpot(float3 worldPos, float3 worldNormal, float3 lightDir)
    {
        float4 atlasParams = _SpotShadowAtlasParams[ShadowSlot];
        if (atlasParams.z <= 0.0) return 0.0;

        float3 worldPosBiased = worldPos + normalize(worldNormal) * ShadowNormalBias;
        float4 lightSpacePos  = mul(_SpotShadowMatrices[ShadowSlot], float4(worldPosBiased, 1.0));
        float3 projCoords     = lightSpacePos.xyz / lightSpacePos.w;
        projCoords = projCoords * 0.5 + 0.5;

        if (projCoords.z > 1.0 || any(projCoords.xy < 0.0) || any(projCoords.xy > 1.0))
            return 0.0;

        uint atlasW, atlasH, numMips;
        _ShadowAtlas.GetDimensions(0, atlasW, atlasH, numMips);
        float atlasSize = (float)atlasW;

        float2 atlasCoords, shadowMin, shadowMax;
        GetAtlasCoordinates(projCoords, atlasParams, atlasSize, atlasCoords, shadowMin, shadowMax);

        float finalBias    = CalculateSlopeBias(worldNormal, lightDir, ShadowBias);
        float currentDepth = projCoords.z - finalBias;

        return SampleShadowPCF(_ShadowAtlas, _ShadowAtlasSampler, atlasCoords, shadowMin, shadowMax,
                               currentDepth, ShadowQuality, ShadowStrength);
    }
}


// ============================================================
//  Shadow atlas
// ============================================================

uniform Texture2D<float4> _ShadowAtlas;
uniform SamplerState _ShadowAtlasSampler;

// ============================================================
//  Directional light cascade shadow data
// ============================================================

struct CascadeData
{
    int      _Count;
    float4x4 _ShadowMatrix0;
    float4x4 _ShadowMatrix1;
    float4x4 _ShadowMatrix2;
    float4x4 _ShadowMatrix3;
    float4   _AtlasParams0;
    float4   _AtlasParams1;
    float4   _AtlasParams2;
    float4   _AtlasParams3;
}
ParameterBlock<CascadeData> Cascades;

// ============================================================
//  Additional shadow-casting point/spot lights (max 2 slots each)
// ============================================================

uniform float4x4 _PointShadowMatrices[12];   // 6 faces * 2 lights
uniform float4   _PointShadowFaceParams[12];  // xy: atlasPos, z: faceSize, w: farPlane

uniform float4x4 _SpotShadowMatrices[2];
uniform float4   _SpotShadowAtlasParams[2];   // xy: atlasPos, z: atlasSize, w: unused


// ============================================================
//  Module-level helpers
// ============================================================

public float InterleavedGradientNoise(float2 position)
{
    float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
    return fract(magic.z * fract(dot(position, magic.xy)));
}

public float3 WorldPosFromDepth(float depth, float2 texCoord, float4x4 invViewProjection)
{
    float z = depth * 2.0 - 1.0;
    float4 clipSpacePosition = float4(texCoord * 2.0 - 1.0, z, 1.0);
    float4 worldSpacePosition = mul(invViewProjection, clipSpacePosition);
    worldSpacePosition /= worldSpacePosition.w;
    return worldSpacePosition.xyz;
}

float SampleShadowPCF(
    Texture2D<float4> shadowAtlas,
    SamplerState shadowSampler,
    float2 atlasCoords,
    float2 shadowMin,
    float2 shadowMax,
    float currentDepth,
    float shadowQuality,
    float shadowStrength)
{
    float shadow = 0.0;

    uint atlasW, atlasH, numMips;
    shadowAtlas.GetDimensions(0, atlasW, atlasH, numMips);
    float2 texelSize = float2(1.0) / float2(atlasW, atlasH);

    if (shadowQuality < 0.5)
    {
        float closestDepth = shadowAtlas.Sample(shadowSampler, atlasCoords).r;
        shadow = currentDepth > closestDepth ? 1.0 : 0.0;
    }
    else
    {
        float filterRadius = 1.5;
        float randomRotation = InterleavedGradientNoise(gl_FragCoord.xy) * 6.283185;
        float s = sin(randomRotation);
        float c = cos(randomRotation);
        float2x2 rotMat = float2x2(c, -s, s, c);

        for (int i = 0; i < 8; i++)
        {
            float2 offset = mul(rotMat, POISSON_DISK_8[i]) * texelSize * filterRadius;
            float2 sampleCoords = clamp(atlasCoords + offset, shadowMin, shadowMax);
            float pcfDepth = shadowAtlas.Sample(shadowSampler, sampleCoords).r;
            shadow += currentDepth > pcfDepth ? 1.0 : 0.0;
        }
        shadow /= 8.0;
    }

    return shadow * shadowStrength;
}

float CalculateSlopeBias(float3 worldNormal, float3 lightDirection, float baseBias)
{
    float cosTheta = clamp(dot(normalize(worldNormal), normalize(lightDirection)), 0.0, 1.0);
    float slopeScaleBias = baseBias * tan(acos(cosTheta));
    slopeScaleBias = clamp(slopeScaleBias, 0.0, baseBias * 2.0);
    return baseBias + slopeScaleBias;
}

void GetAtlasCoordinates(float3 projCoords, float4 atlasParams, float atlasSize, out float2 atlasCoords, out float2 shadowMin, out float2 shadowMax)
{
    atlasCoords.x = atlasParams.x + (projCoords.x * atlasParams.z);
    atlasCoords.y = atlasParams.y + (projCoords.y * atlasParams.z);

    float2 texelSize = float2(1.0) / atlasSize;
    shadowMin = float2(atlasParams.x, atlasParams.y) / atlasSize + texelSize * 0.5;
    shadowMax = float2(atlasParams.x + atlasParams.z, atlasParams.y + atlasParams.z) / atlasSize - texelSize * 0.5;

    atlasCoords /= atlasSize;
}
