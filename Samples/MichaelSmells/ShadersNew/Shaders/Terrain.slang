import VertexInput;
import FragmentInput;
import Material;
import Lighting;
import ShaderVariables;
import Math;

import ProwlFragmentInput;
import ProwlMeshInput;
import ProwlInstanceInput;
import ProwlVariables;
import ProwlBRDF;
import ProwlLighting;
import ProwlShadow;


module Terrain;


// ============================================================
//  Terrain uniforms
// ============================================================

public struct TerrainData
{
    public float TerrainSize;
    public float TerrainHeight;
    public float4x4 TerrainWorldToLocal;
    public float4x4 TerrainLocalToWorld;

    public float4 Layer0Params;  // x=Tiling, y=Roughness, z=Metallic
    public float4 Layer1Params;
    public float4 Layer2Params;
    public float4 Layer3Params;

    public float2 BrushPosition;
    public float  BrushRadius;
    public float  BrushFalloff;
    public float  BrushVisible;
}
ParameterBlock<TerrainData> TerrainUniforms;

Texture2D<float4> _Heightmap;   SamplerState _HeightmapSampler;
Texture2D<float4> _Splatmap;    SamplerState _SplatmapSampler;

Texture2D<float4> _Layer0;       SamplerState _Layer0Sampler;
Texture2D<float4> _Layer0Normal; SamplerState _Layer0NormalSampler;
Texture2D<float4> _Layer1;       SamplerState _Layer1Sampler;
Texture2D<float4> _Layer1Normal; SamplerState _Layer1NormalSampler;
Texture2D<float4> _Layer2;       SamplerState _Layer2Sampler;
Texture2D<float4> _Layer2Normal; SamplerState _Layer2NormalSampler;
Texture2D<float4> _Layer3;       SamplerState _Layer3Sampler;
Texture2D<float4> _Layer3Normal; SamplerState _Layer3NormalSampler;

ParameterBlock<ForwardLit<ProwlVariables>> Lighting;


// ============================================================
//  Varyings
// ============================================================

struct TerrainVaryings
{
    float4 clipPos     : SV_Position;
    float2 terrainUV   : UV0;
    float3 worldPos    : POSITION;
    float3 worldNormal : NORMAL;
}

struct TerrainShadowVaryings
{
    float4 clipPos : SV_Position;
}

struct TerrainDepthNormalsVaryings
{
    float4 clipPos     : SV_Position;
    float3 worldNormal : NORMAL;
}


// ============================================================
//  Shared helpers
// ============================================================

// Remap vertex UV → texel-centre UV so GPU heightmap samples match CPU TerrainData.GetInterpolatedHeight.
float2 HMSampleUV(float2 uv)
{
    uint w, h, mips;
    _Heightmap.GetDimensions(0, w, h, mips);
    float2 s = float2(w, h);
    return uv * (s - 1.0) / s + 0.5 / s;
}

struct TerrainDisplaced
{
    float3 worldPos;
    float3 worldNormal;
    float2 terrainUV;
}

TerrainDisplaced DisplaceVertex(ProwlInstanceInput instance, float3 meshPos)
{
    float4x4 instanceModel = float4x4(
        instance.Row<0>(), instance.Row<1>(),
        instance.Row<2>(), instance.Row<3>());

    float4 worldPos4    = mul(instanceModel, float4(meshPos, 1.0));
    float3 terrainLocal = mul(TerrainUniforms.TerrainWorldToLocal, worldPos4).xyz;
    float2 terrainUV    = terrainLocal.xz / TerrainUniforms.TerrainSize;

    float height = _Heightmap.SampleLevel(_HeightmapSampler, HMSampleUV(terrainUV), 0).r * TerrainUniforms.TerrainHeight;

    float3 displacedLocal = float3(terrainLocal.x, height, terrainLocal.z);
    float3 worldPosition  = mul(TerrainUniforms.TerrainLocalToWorld, float4(displacedLocal, 1.0)).xyz;

    // Normal via central differences
    uint w, h, mips;
    _Heightmap.GetDimensions(0, w, h, mips);
    float hmSize   = float(w);
    float vertStep = hmSize > 1.0 ? (1.0 / (hmSize - 1.0)) : 0.001;

    float hR = _Heightmap.SampleLevel(_HeightmapSampler, HMSampleUV(terrainUV + float2(vertStep, 0.0)), 0).r * TerrainUniforms.TerrainHeight;
    float hL = _Heightmap.SampleLevel(_HeightmapSampler, HMSampleUV(terrainUV - float2(vertStep, 0.0)), 0).r * TerrainUniforms.TerrainHeight;
    float hU = _Heightmap.SampleLevel(_HeightmapSampler, HMSampleUV(terrainUV + float2(0.0, vertStep)), 0).r * TerrainUniforms.TerrainHeight;
    float hD = _Heightmap.SampleLevel(_HeightmapSampler, HMSampleUV(terrainUV - float2(0.0, vertStep)), 0).r * TerrainUniforms.TerrainHeight;

    float wStep    = vertStep * TerrainUniforms.TerrainSize;
    float slopeX   = (hR - hL) / (wStep * 2.0);
    float slopeZ   = (hU - hD) / (wStep * 2.0);
    float3 localN  = normalize(float3(-slopeX, 1.0, -slopeZ));
    float3 worldN  = normalize(mul(float3x3(TerrainUniforms.TerrainLocalToWorld), localN));

    TerrainDisplaced result;
    result.worldPos    = worldPosition;
    result.worldNormal = worldN;
    result.terrainUV   = terrainUV;
    return result;
}

float3 UnpackNormal(float4 packed)
{
    float3 n;
    n.xy = packed.rg * 2.0 - 1.0;
    n.z  = sqrt(max(0.0, 1.0 - dot(n.xy, n.xy)));
    return n;
}


// ============================================================
//  Forward lit pass
// ============================================================

[shader("vertex")]
TerrainVaryings TerrainVS(ProwlMeshInput mesh, ProwlInstanceInput instance)
{
    TerrainDisplaced d = DisplaceVertex(instance, mesh.Position());

    TerrainVaryings o;
    o.clipPos     = mul(ProwlVariables::ViewProjection(), float4(d.worldPos, 1.0));
    o.terrainUV   = d.terrainUV;
    o.worldPos    = d.worldPos;
    o.worldNormal = d.worldNormal;
    return o;
}

[shader("fragment")]
float4 TerrainFS(ProwlFragmentInput fragInput)
{
    float2 tc = fragInput.uv0;  // terrainUV passed via UV0 semantic

    float4 sw = _Splatmap.Sample(_SplatmapSampler, tc);
    float  ws = sw.r + sw.g + sw.b + sw.a;
    if (ws > 0.0) sw /= ws;

    float2 uv0 = tc * TerrainUniforms.Layer0Params.x;
    float2 uv1 = tc * TerrainUniforms.Layer1Params.x;
    float2 uv2 = tc * TerrainUniforms.Layer2Params.x;
    float2 uv3 = tc * TerrainUniforms.Layer3Params.x;

    float3 albedo =
          _Layer0.Sample(_Layer0Sampler, uv0).rgb * sw.r
        + _Layer1.Sample(_Layer1Sampler, uv1).rgb * sw.g
        + _Layer2.Sample(_Layer2Sampler, uv2).rgb * sw.b
        + _Layer3.Sample(_Layer3Sampler, uv3).rgb * sw.a;

    float3 baseColor = gammaToLinearSpace(albedo);

    // Blended tangent-space normals
    float3 blendedNormalTS = normalize(
          UnpackNormal(_Layer0Normal.Sample(_Layer0NormalSampler, uv0)) * sw.r
        + UnpackNormal(_Layer1Normal.Sample(_Layer1NormalSampler, uv1)) * sw.g
        + UnpackNormal(_Layer2Normal.Sample(_Layer2NormalSampler, uv2)) * sw.b
        + UnpackNormal(_Layer3Normal.Sample(_Layer3NormalSampler, uv3)) * sw.a);

    float roughness =
          TerrainUniforms.Layer0Params.y * sw.r
        + TerrainUniforms.Layer1Params.y * sw.g
        + TerrainUniforms.Layer2Params.y * sw.b
        + TerrainUniforms.Layer3Params.y * sw.a;

    float metallic =
          TerrainUniforms.Layer0Params.z * sw.r
        + TerrainUniforms.Layer1Params.z * sw.g
        + TerrainUniforms.Layer2Params.z * sw.b
        + TerrainUniforms.Layer3Params.z * sw.a;

    // Build TBN from world normal (no vertex tangents for terrain — derive from N)
    float3 N = normalize(fragInput.normal);
    float3 T = normalize(cross(N, float3(0.0, 0.0, 1.0)));
    float3 B = cross(N, T);
    float3 finalNormal = normalize(mul(float3x3(T, B, N), blendedNormalTS));

    // Brush visualisation
    if (TerrainUniforms.BrushVisible > 0.5 && TerrainUniforms.BrushRadius > 0.0)
    {
        float dist = length(tc - TerrainUniforms.BrushPosition);
        if (dist < TerrainUniforms.BrushRadius)
        {
            float t     = dist / TerrainUniforms.BrushRadius;
            float alpha = 1.0 - smoothstep(1.0 - TerrainUniforms.BrushFalloff, 1.0, t);
            baseColor   = lerp(baseColor, float3(0.2, 0.8, 0.6), alpha * 0.3);
        }
    }

    fragInput.normal = finalNormal;

    SurfaceInfo surface = {};
    surface.FinalUV   = tc;
    surface.BaseColor = float4(baseColor, 1.0);
    surface.AO        = 1.0;
    surface.Roughness = roughness;
    surface.Metallic  = metallic;
    surface.Emission  = float3(0.0);

    StandardBRDF brdf;
    Lighting.Compute(fragInput, surface, brdf);

    return float4(surface.LightingAccum, 1.0);
}


// ============================================================
//  Shadow caster pass
// ============================================================

[shader("vertex")]
TerrainShadowVaryings TerrainShadowVS(ProwlMeshInput mesh, ProwlInstanceInput instance)
{
    TerrainDisplaced d = DisplaceVertex(instance, mesh.Position());

    TerrainShadowVaryings o;
    o.clipPos = mul(ProwlVariables::ViewProjection(), float4(d.worldPos, 1.0));
    return o;
}

[shader("fragment")]
void TerrainShadowFS(ProwlFragmentInput fragInput)
{
}


// ============================================================
//  DepthNormals pass
// ============================================================

[shader("vertex")]
TerrainDepthNormalsVaryings TerrainDepthNormalsVS(ProwlMeshInput mesh, ProwlInstanceInput instance)
{
    TerrainDisplaced d = DisplaceVertex(instance, mesh.Position());

    TerrainDepthNormalsVaryings o;
    o.clipPos     = mul(ProwlVariables::ViewProjection(), float4(d.worldPos, 1.0));
    o.worldNormal = d.worldNormal;
    return o;
}

[shader("fragment")]
float4 TerrainDepthNormalsFS(ProwlFragmentInput fragInput)
{
    return EncodeViewNormal(normalize(fragInput.normal), ProwlVariables::View());
}
