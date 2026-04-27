import VertexInput;
import FragmentInput;
import ShaderVariables;

import ProwlFragmentInput;
import ProwlMeshInput;
import ProwlInstanceInput;
import ProwlVariables;


module CubemapSkybox;


public struct SkyboxMaterial
{
    public float4 Tint;
    public float  Exposure;
}
ParameterBlock<SkyboxMaterial> Mat;

Texture2D<float4> _CubeRight;   SamplerState _CubeRightSampler;
Texture2D<float4> _CubeLeft;    SamplerState _CubeLeftSampler;
Texture2D<float4> _CubeTop;     SamplerState _CubeTopSampler;
Texture2D<float4> _CubeBottom;  SamplerState _CubeBottomSampler;
Texture2D<float4> _CubeFront;   SamplerState _CubeFrontSampler;
Texture2D<float4> _CubeBack;    SamplerState _CubeBackSampler;


struct SkyboxVaryings
{
    float4 clipPos    : SV_Position;
    float3 direction  : POSITION;
}


[shader("vertex")]
SkyboxVaryings CubemapSkyboxVS(ProwlMeshInput mesh)
{
    float4x4 view = ProwlVariables::View();

    // Strip translation so the skybox stays centred on the camera
    view[0][3] = 0.0;
    view[1][3] = 0.0;
    view[2][3] = 0.0;

    float4 pos = mul(ProwlVariables::Projection(), mul(view, float4(mesh.Position(), 1.0)));

    SkyboxVaryings o;
    o.clipPos   = pos.xyww;   // depth = 1.0 (far plane)
    o.direction = mesh.Position();
    return o;
}

float4 SampleCubeFace(float3 dir)
{
    float3 a = abs(dir);

    if (a.x >= a.y && a.x >= a.z)
    {
        float2 uv = (dir.x > 0.0)
            ? float2(-dir.z, -dir.y) / a.x * 0.5 + 0.5
            : float2( dir.z, -dir.y) / a.x * 0.5 + 0.5;
        return (dir.x > 0.0)
            ? _CubeRight.Sample(_CubeRightSampler, uv)
            : _CubeLeft.Sample(_CubeLeftSampler, uv);
    }
    else if (a.y >= a.x && a.y >= a.z)
    {
        float2 uv = (dir.y > 0.0)
            ? float2( dir.x,  dir.z) / a.y * 0.5 + 0.5
            : float2( dir.x, -dir.z) / a.y * 0.5 + 0.5;
        return (dir.y > 0.0)
            ? _CubeTop.Sample(_CubeTopSampler, uv)
            : _CubeBottom.Sample(_CubeBottomSampler, uv);
    }
    else
    {
        float2 uv = (dir.z > 0.0)
            ? float2( dir.x, -dir.y) / a.z * 0.5 + 0.5
            : float2(-dir.x, -dir.y) / a.z * 0.5 + 0.5;
        return (dir.z > 0.0)
            ? _CubeFront.Sample(_CubeFrontSampler, uv)
            : _CubeBack.Sample(_CubeBackSampler, uv);
    }
}

[shader("fragment")]
float4 CubemapSkyboxFS(SkyboxVaryings fragInput)
{
    float3 dir   = normalize(fragInput.direction);
    float4 color = SampleCubeFace(dir);
    color.rgb   *= Mat.Tint.rgb * Mat.Exposure;
    return float4(color.rgb, 1.0);
}
