import VertexInput;
import FragmentInput;
import ShaderVariables;
import Math;

import ProwlFragmentInput;
import ProwlMeshInput;
import ProwlInstanceInput;
import ProwlVariables;


module Particle;


public struct ParticleMaterial
{
    public float4 MainColor;
    public float  SoftParticlesFactor;
}
ParameterBlock<ParticleMaterial> Mat;

Texture2D<float4> _MainTex;
SamplerState      _MainTexSampler;


struct ParticleVaryings
{
    float4 clipPos : SV_Position;
    float2 uv0     : UV0;
    float4 color   : COLOR;
}


[shader("vertex")]
ParticleVaryings ParticleVS(ProwlMeshInput mesh, ProwlInstanceInput instance)
{
    // Extract instance data
    float3 particlePos = instance.Row<3>().xyz;
    float  scaleX      = length(instance.Row<0>().xyz);
    float  scaleY      = length(instance.Row<1>().xyz);

    float3 matRight = instance.Row<0>().xyz / scaleX;
    float  rotAngle = atan2(matRight.y, matRight.x);

    // Billboard using camera right/up from view matrix
    float4x4 view = ProwlVariables::View();
    float3 camRight = float3(view[0][0], view[1][0], view[2][0]);
    float3 camUp    = float3(view[0][1], view[1][1], view[2][1]);

    float cosR = cos(rotAngle);
    float sinR = sin(rotAngle);
    float2 rv  = float2(
        mesh.Position().x * cosR - mesh.Position().y * sinR,
        mesh.Position().x * sinR + mesh.Position().y * cosR);

    float3 worldPos = particlePos
        + camRight * rv.x * scaleX
        + camUp    * rv.y * scaleY;

    // UV animation from custom instance data: x=lifetime, y=uvOffsetX, z=uvOffsetY, w=uvScale
    float4 custom  = instance.CustomData();
    float2 uvOffset = custom.yz;
    float  uvScale  = custom.w;
    float2 uv = mesh.UVChannel<0>() * uvScale + uvOffset;

    ParticleVaryings o;
    o.clipPos = mul(ProwlVariables::ViewProjection(), float4(worldPos, 1.0));
    o.uv0     = uv;
    o.color   = mesh.Color() * instance.InstanceColor();
    return o;
}


[shader("fragment")]
float4 ParticleFS(ParticleVaryings fragInput)
{
    float4 albedo = _MainTex.Sample(_MainTexSampler, fragInput.uv0) * fragInput.color * Mat.MainColor;

    if (albedo.a < 0.01)
        discard;

    float3 baseColor = gammaToLinearSpace(albedo.rgb);
    return float4(baseColor, albedo.a);
}
