import FragmentInput;
import VertexInput;
import Math;
import ShaderVariables;
import Material;

import ProwlSurface;
import ProwlFragmentInput;
import ProwlMeshInput;
import ProwlInstanceInput;
import ProwlVariables;
import ProwlLighting;
import ProwlBRDF;


module Unlit;


public struct UnlitMaterial
{
    public float4 MainColor;
    public float4 TilingOffset;
}
ParameterBlock<UnlitMaterial> Mat;

Texture2D<float4> _MainTex;
SamplerState      _MainTexSampler;

ParameterBlock<AmbientData> Ambient;


struct UnlitVaryings
{
    float4 clipPos  : SV_Position;
    float3 worldPos : POSITION;
    float2 uv0      : UV0;
    float4 color    : COLOR;
}

struct UnlitDepthNormalsVaryings
{
    float4 clipPos : SV_Position;
    float3 normal  : NORMAL;
}


float2 GetUV(float2 uv) { return uv * Mat.TilingOffset.xy + Mat.TilingOffset.zw; }


[shader("vertex")]
ProwlFragmentInput UnlitVS<Instance>(VertexInputs<ProwlVariables, ProwlMeshInput, Instance, void> vtx)
where optional Instance : IInstanceInput
{
    ProwlFragmentInput o;
    o.svPosition  = vtx.ClipPosition();
    o.position = vtx.WorldPosition();
    o.uv0      = GetUV(vtx.MeshInputs.UVChannel<0>());
    o.color    = vtx.GetInstanceColor();
    return o;
}

[shader("fragment")]
float4 UnlitFS(ProwlFragmentInput fragInput)
{
    SurfaceInfo surface = {};
    surface.FinalUV = fragInput.UVChannel<0>();

    StandardSurface standardSurface;
    standardSurface.GetSurface(fragInput, surface);

    StandardBRDF standard; // this BRDF isn't ever used by Unlit.Compute(), it just needs a placeholder.
    Unlit<ProwlVariables> unlit;
    unlit.Compute(fragInput, surface, standard);

    return float4(surface.LightingAccum, surface.BaseColor.a);
}


[shader("vertex")]
ProwlFragmentInput UnlitDS<Instance>(VertexInputs<ProwlVariables, ProwlMeshInput, Instance, void> vtx)
where optional Instance : IInstanceInput
{
    ProwlFragmentInput o;
    o.svPosition = vtx.ClipPosition();
    o.normal  = vtx.TransformDirection(vtx.MeshInputs.Normal());
    return o;
}

[shader("fragment")]
float4 UnlitDepthNormalsFS(ProwlFragmentInput fragInput)
{
    return EncodeViewNormal(normalize(fragInput.normal), ProwlVariables::View());
}
