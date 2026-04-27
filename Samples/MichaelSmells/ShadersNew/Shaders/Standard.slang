import FragmentInput;
import VertexInput;
import Material;
import Lighting;
import ShaderVariables;
import Math;

import ProwlFragmentInput;
import ProwlMeshInput;
import ProwlInstanceInput;
import ProwlVariables;
import ProwlBRDF;
import ProwlSurface;
import ProwlPOM;
import ProwlLighting;
import ProwlShadow;


module Standard;


// ============================================================
//  Material uniforms
// ============================================================

public struct StandardMaterial
{
    public float4 TilingOffset;
    public float  Anisotropy;
}

ParameterBlock<StandardMaterial> Mat;


ParameterBlock<ForwardLitTranslucent<ProwlVariables>> Lighting;


// ============================================================
//  Vertex output structs
// ============================================================

struct StandardShadowVaryings
{
    float4 clipPos : SV_Position;
    float2 uv0     : UV0;
}


// ============================================================
//  Shared helpers
// ============================================================

float2 GetUV(float2 uv)
{
    return uv * Mat.TilingOffset.xy + Mat.TilingOffset.zw;
}

float3 BuildBitangent(float3 normal, float3 tangent)
{
    float3 b = cross(normal, tangent);
    if (dot(b, b) < 0.000001)
    {
        float3 alt = abs(normal.y) < 0.999 ? float3(0, 1, 0) : float3(1, 0, 0);
        tangent = normalize(cross(normal, alt));
        b = cross(normal, tangent);
    }
    return b;
}


// ============================================================
//  Forward lit vertex shader (shared by isotropic + aniso)
// ============================================================

[shader("vertex")]
ProwlFragmentInput StandardVS<Instance>(VertexInputs<ProwlVariables, ProwlMeshInput, Instance, void> vtx)
where optional Instance : IInstanceInput
{
    float3 worldPos = vtx.WorldPosition();
    float3 normal   = vtx.TransformDirection(vtx.MeshInputs.Normal());
    float3 tangent  = vtx.TransformDirection(vtx.MeshInputs.Tangent().xyz);
    float3 bitangent = BuildBitangent(normal, tangent);

    ProwlFragmentInput o;
    o.svPosition   = vtx.ClipPosition();
    o.position  = worldPos;
    o.uv0       = GetUV(vtx.MeshInputs.UVChannel<0>());
    o.normal    = normal;
    o.tangent   = float4(tangent, 0.0);
    o.bitangent = float4(bitangent, 0.0);
    o.color     = vtx.GetInstanceColor();

    return o;
}


// ============================================================
//  Forward lit fragment — isotropic PBR
// ============================================================

[shader("fragment")]
float4 StandardFS(ProwlFragmentInput fragInput)
{
    // Apply POM if enabled
    SurfaceInfo surface = {};

    POMRemapper<ProwlVariables> pom;
    pom.Apply(fragInput, surface);

    StandardSurface standardSurface;

    if (standardSurface.GetCutoff(surface.FinalUV))
        discard;

    standardSurface.GetSurface(fragInput, surface);

    StandardBRDF brdf;
    Lighting.Compute(fragInput, surface, brdf);

    return float4(surface.LightingAccum, surface.BaseColor.a);
}


// ============================================================
//  Forward lit fragment — anisotropic PBR
// ============================================================

[shader("fragment")]
float4 StandardAnisoFS(ProwlFragmentInput fragInput)
{
    SurfaceInfo surface = {};

    POMRemapper<ProwlVariables> pom;
    pom.Apply(fragInput, surface);

    StandardSurface standardSurface;

    if (standardSurface.GetCutoff(surface.FinalUV))
        discard;

    standardSurface.GetSurface(fragInput, surface);

    AnisotropicBRDF brdf = AnisotropicBRDF(Mat.Anisotropy, surface.Roughness);
    Lighting.Compute(fragInput, surface, brdf);

    return float4(surface.LightingAccum, surface.BaseColor.a);
}


// ============================================================
//  DepthNormals pass
// ============================================================

[shader("vertex")]
ProwlFragmentInput StandardDepthNormalsVS(ProwlMeshInput mesh, ProwlInstanceInput instance)
{
    VertexInputs<ProwlVariables, ProwlMeshInput, ProwlInstanceInput, void> vtx;
    vtx.MeshInputs     = mesh;
    vtx.InstanceInputs = instance;

    float3 normal    = vtx.TransformDirection(mesh.Normal());
    float3 tangent   = vtx.TransformDirection(mesh.Tangent().xyz);
    float3 bitangent = BuildBitangent(normal, tangent);

    ProwlFragmentInput o;
    o.svPosition   = vtx.TransformClip(mesh.Position());
    o.uv0       = GetUV(mesh.UVChannel<0>());
    o.normal    = normal;
    o.tangent   = float4(tangent, 0.0);
    o.bitangent = float4(bitangent, 0.0);
    return o;
}

[shader("fragment")]
float4 StandardDepthNormalsFS(ProwlFragmentInput fragInput)
{
    StandardSurface standardSurface;

    if (standardSurface.GetCutoff(fragInput.UVChannel<0>()))
        discard;

    float3 worldNormal = standardSurface.Normal(fragInput, fragInput.UVChannel<0>());

    return EncodeViewNormal(worldNormal, ProwlVariables::View());
}


// ============================================================
//  ShadowCaster pass
// ============================================================

[shader("vertex")]
StandardShadowVaryings StandardShadowVS<Instance>(VertexInputs<ProwlVariables, ProwlMeshInput, Instance, void> vtx)
where optional Instance : IInstanceInput
{
    StandardShadowVaryings o;
    o.clipPos = vtx.ClipPosition();
    o.uv0     = GetUV(vtx.MeshInputs.UVChannel<0>());
    return o;
}

[shader("fragment")]
void StandardShadowFS(ProwlFragmentInput fragInput)
{
    StandardSurface surface;
    if (surface.GetCutoff(fragInput.UVChannel<0>()))
        discard;
}
