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
import ProwlLight;
import ProwlLighting;
import ProwlShadow;


module Grass;


// ============================================================
//  Grass uniforms
// ============================================================

public struct GrassData
{
    public float AlphaCutoff;
    public float WindStrength;
    public float WindSpeed;
    public float Billboard;
    public float AlignToNormal;
    public float GrassDistance;
    public float GrassFadeStart;
    public float3 TerrainUp;
    public float TerrainSize;
    public float TerrainHeight;
    public float4x4 TerrainWorldToLocal;
    public float4x4 TerrainLocalToWorld;
}
ParameterBlock<GrassData> GrassUniforms;

Texture2D<float4> _MainTex;       SamplerState _MainTexSampler;
Texture2D<float4> _Heightmap;     SamplerState _HeightmapSampler;

ParameterBlock<ForwardLit<ProwlVariables>> Lighting;


// ============================================================
//  Shared blade builder
// ============================================================

struct GrassBlade
{
    float3 worldPos;
    float3 terrainNormal;
}

float2 HMSampleUV(float2 uv)
{
    uint w, h, mips;
    _Heightmap.GetDimensions(0, w, h, mips);
    float2 s = float2(w, h);
    return uv * (s - 1.0) / s + 0.5 / s;
}

GrassBlade ComputeBlade(ProwlMeshInput mesh, ProwlInstanceInput instance, out float3 localOffset)
{
    float4x4 terrainToWorld = GrassUniforms.TerrainLocalToWorld;
    float3   localPosition  = instance.Row<3>().xyz;
    float3   bladePosition  = mul(terrainToWorld, float4(localPosition, 1.0)).xyz;

    float scaleX = length(instance.Row<0>().xyz);
    float scaleY = length(instance.Row<1>().xyz);

    // Distance fade
    float3 camFlat    = float3(ProwlVariables::WorldSpaceCameraPos().x, bladePosition.y, ProwlVariables::WorldSpaceCameraPos().z);
    float  camDist    = length(bladePosition - camFlat);
    float  fadeRange  = max(0.0001, GrassUniforms.GrassDistance - GrassUniforms.GrassFadeStart);
    float  fade       = 1.0 - clamp((camDist - GrassUniforms.GrassFadeStart) / fadeRange, 0.0, 1.0);
    scaleX *= fade;
    scaleY *= fade;

    // Terrain normal from heightmap
    float2 terrainUV = localPosition.xz / GrassUniforms.TerrainSize;
    uint   w, h, mips;
    _Heightmap.GetDimensions(0, w, h, mips);
    float hmSize   = float(w);
    float vertStep = hmSize > 1.0 ? (1.0 / (hmSize - 1.0)) : 0.001;

    float2 baseUV  = HMSampleUV(terrainUV);
    float2 stepUV  = float2(vertStep * (hmSize - 1.0) / hmSize, 0.0);
    float2 stepVV  = float2(0.0, vertStep * (hmSize - 1.0) / hmSize);
    float  hR = _Heightmap.SampleLevel(_HeightmapSampler, baseUV + stepUV, 0).r * GrassUniforms.TerrainHeight;
    float  hL = _Heightmap.SampleLevel(_HeightmapSampler, baseUV - stepUV, 0).r * GrassUniforms.TerrainHeight;
    float  hU = _Heightmap.SampleLevel(_HeightmapSampler, baseUV + stepVV, 0).r * GrassUniforms.TerrainHeight;
    float  hD = _Heightmap.SampleLevel(_HeightmapSampler, baseUV - stepVV, 0).r * GrassUniforms.TerrainHeight;

    float  wStep    = vertStep * GrassUniforms.TerrainSize;
    float3 localN   = normalize(float3(-(hR - hL) / (wStep * 2.0), 1.0, -(hU - hD) / (wStep * 2.0)));
    float3 terrainN = normalize(mul(float3x3(terrainToWorld), localN));

    float3 up = (GrassUniforms.AlignToNormal > 0.5) ? terrainN : GrassUniforms.TerrainUp;

    float4x4 view = ProwlVariables::View();
    if (GrassUniforms.Billboard > 0.5)
    {
        float3 cameraRight = float3(view[0][0], view[1][0], view[2][0]);
        cameraRight = normalize(cameraRight - up * dot(cameraRight, up));
        localOffset = cameraRight * mesh.Position().x * scaleX
                    + up          * mesh.Position().y * scaleY;
    }
    else
    {
        float3 right = normalize(mul(float3x3(terrainToWorld), normalize(instance.Row<0>().xyz)));
        right = normalize(right - up * dot(right, up));
        localOffset = right * mesh.Position().x * scaleX
                    + up    * mesh.Position().y * scaleY;
    }

    // Wind sway (top vertices only)
    float4 custom     = instance.CustomData();
    float  windPhase  = custom.x;
    float  bendFactor = custom.y;
    float  windAmount = max(0.0, mesh.Position().y);
    float  wind = sin(ProwlVariables::Time().y * GrassUniforms.WindSpeed
                      + bladePosition.x * 0.7 + bladePosition.z * 0.4 + windPhase)
                  * GrassUniforms.WindStrength * bendFactor;
    localOffset.x += wind * windAmount;
    localOffset.z += wind * windAmount * 0.3;

    GrassBlade blade;
    blade.worldPos      = bladePosition + localOffset + up * 0.01 * scaleY;
    blade.terrainNormal = terrainN;
    return blade;
}


// ============================================================
//  Forward lit pass
// ============================================================

[shader("vertex")]
ProwlFragmentInput GrassVS(ProwlMeshInput mesh, ProwlInstanceInput instance)
{
    float3   localOffset;
    GrassBlade blade = ComputeBlade(mesh, instance, localOffset);

    ProwlFragmentInput o;
    o.svPosition  = mul(ProwlVariables::ViewProjection(), float4(blade.worldPos, 1.0));
    o.uv0         = mesh.UVChannel<0>();
    o.color       = mesh.Color() * instance.InstanceColor();
    o.position    = blade.worldPos;
    o.normal      = blade.terrainNormal;
    return o;
}

[shader("fragment")]
float4 GrassFS(ProwlFragmentInput fragInput)
{
    float4 texColor   = _MainTex.Sample(_MainTexSampler, fragInput.UVChannel<0>());
    float4 finalColor = texColor * fragInput.Color();

    if (finalColor.a < GrassUniforms.AlphaCutoff)
        discard;

    SurfaceInfo surface = {};
    surface.FinalUV   = fragInput.UVChannel<0>();
    surface.BaseColor = float4(gammaToLinearSpace(finalColor.rgb), 1.0);
    surface.AO        = 1.0;
    surface.Roughness = 0.9;
    surface.Metallic  = 0.0;
    surface.Emission  = float3(0.0);

    StandardBRDF brdf;
    Lighting.Compute(fragInput, surface, brdf);

    return float4(surface.LightingAccum, 1.0);
}


// ============================================================
//  DepthNormals pass
// ============================================================

[shader("vertex")]
ProwlFragmentInput GrassDepthNormalsVS(ProwlMeshInput mesh, ProwlInstanceInput instance)
{
    float3   localOffset;
    GrassBlade blade = ComputeBlade(mesh, instance, localOffset);

    ProwlFragmentInput o;
    o.svPosition    = mul(ProwlVariables::ViewProjection(), float4(blade.worldPos, 1.0));
    o.uv0           = mesh.UVChannel<0>();
    o.color         = float4(1.0);
    o.position      = blade.worldPos;
    o.normal        = blade.terrainNormal;
    return o;
}

[shader("fragment")]
float4 GrassDepthNormalsFS(ProwlFragmentInput fragInput)
{
    float4 texColor = _MainTex.Sample(_MainTexSampler, fragInput.UVChannel<0>());
    if (texColor.a < GrassUniforms.AlphaCutoff)
        discard;

    return EncodeViewNormal(normalize(fragInput.Normal()), ProwlVariables::View());
}
