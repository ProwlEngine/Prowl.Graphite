import VertexInput;
import FragmentInput;
import ShaderVariables;

import ProwlFragmentInput;
import ProwlMeshInput;
import ProwlVariables;


module GradientSkybox;


public struct GradientSkyboxMaterial
{
    public float4 TopColor;
    public float4 BottomColor;
    public float  Exponent;
}
ParameterBlock<GradientSkyboxMaterial> Mat;


struct SkyboxVaryings
{
    float4 clipPos   : SV_Position;
    float3 direction : POSITION;
}


[shader("vertex")]
SkyboxVaryings GradientSkyboxVS(ProwlMeshInput mesh)
{
    float4x4 view = ProwlVariables::View();
    view[0][3] = 0.0;
    view[1][3] = 0.0;
    view[2][3] = 0.0;

    float4 pos = mul(ProwlVariables::Projection(), mul(view, float4(mesh.Position(), 1.0)));

    SkyboxVaryings o;
    o.clipPos   = pos.xyww;
    o.direction = normalize(mesh.Position());
    return o;
}

[shader("fragment")]
float4 GradientSkyboxFS(SkyboxVaryings fragInput)
{
    float t = pow(clamp(fragInput.direction.y * 0.5 + 0.5, 0.0, 1.0), Mat.Exponent);
    float3 color = lerp(Mat.BottomColor.rgb, Mat.TopColor.rgb, t);
    return float4(color, 1.0);
}
