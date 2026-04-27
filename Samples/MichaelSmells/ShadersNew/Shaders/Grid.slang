import VertexInput;
import FragmentInput;
import ShaderVariables;
import Math;

import ProwlFragmentInput;
import ProwlMeshInput;
import ProwlInstanceInput;
import ProwlVariables;


module Grid;


public struct GridMaterial
{
    public float4 GridColor;
    public float  PrimaryGridSize;
    public float  SecondaryGridSize;
    public float  LineWidth;
    public float  Falloff;
    public float  MaxDist;
}
ParameterBlock<GridMaterial> Mat;

Texture2D<float>  _CameraDepthTexture;
SamplerState      _CameraDepthSampler;


struct GridVaryings
{
    float4 clipPos  : SV_Position;
    float3 worldPos : POSITION;
    float3 viewPos  : TEXCOORD0;
    float2 uv       : UV0;
}


[shader("vertex")]
GridVaryings GridVS<Instance>(VertexInputs<ProwlVariables, ProwlMeshInput, Instance, void> vtx)
where optional Instance : IInstanceInput
{
    GridVaryings o;
    o.worldPos = vtx.WorldPosition();
    o.clipPos  = mul(ProwlVariables::ViewProjection(), float4(o.worldPos, 1));
    o.viewPos  = mul(ProwlVariables::View(), float4(o.worldPos, 1)).xyz;
    o.uv       = o.worldPos.xz;
    return o;
}


// Pristine anti-aliased grid (bgolus.medium.com/the-best-darn-grid-shader-yet)
float PristineGrid(float2 uv, float2 lineWidth)
{
    lineWidth = clamp(lineWidth, float2(0.0), float2(0.5));

    float4 uvDDXY    = float4(ddx(uv), ddy(uv));
    float2 uvDeriv   = float2(length(uvDDXY.xz), length(uvDDXY.yw));

    bool2 invertLine = lineWidth > float2(0.5);

    float2 targetWidth = float2(
        invertLine.x ? 1.0 - lineWidth.x : lineWidth.x,
        invertLine.y ? 1.0 - lineWidth.y : lineWidth.y);

    float2 drawWidth = clamp(targetWidth, uvDeriv, float2(0.5));
    float2 lineAA    = max(uvDeriv, float2(0.000001)) * 1.5;
    float2 gridUV    = abs(frac(uv) * 2.0 - 1.0);

    gridUV.x = invertLine.x ? gridUV.x : 1.0 - gridUV.x;
    gridUV.y = invertLine.y ? gridUV.y : 1.0 - gridUV.y;

    float2 grid2 = smoothstep(drawWidth + lineAA, drawWidth - lineAA, gridUV);
    grid2 *= clamp(targetWidth / drawWidth, float2(0.0), float2(1.0));
    grid2  = lerp(grid2, targetWidth, clamp(uvDeriv * 2.0 - 1.0, float2(0.0), float2(1.0)));

    grid2.x = invertLine.x ? 1.0 - grid2.x : grid2.x;
    grid2.y = invertLine.y ? 1.0 - grid2.y : grid2.y;

    return lerp(grid2.x, 1.0, grid2.y);
}

float LinearizeDepthFromProjection(float depth)
{
    float4 p = ProwlVariables::ProjectionParams();
    return p.y * p.z / (p.z + depth * (p.y - p.z));
}


[shader("fragment")]
float4 GridFS(GridVaryings fragInput)
{
    float2 screenUV      = fragInput.clipPos.xy / ProwlVariables::ScreenParams().xy;
    float  sceneDepthRaw = _CameraDepthTexture.Sample(_CameraDepthSampler, screenUV);
    float  sceneDepthLin = LinearizeDepthFromProjection(sceneDepthRaw);
    float  gridDepthLin  = LinearizeDepthFromProjection(fragInput.clipPos.z);

    float depthDiff = abs(sceneDepthLin - gridDepthLin);
    float threshold = gridDepthLin * 0.005;
    if (depthDiff < threshold)
        discard;

    float sg = PristineGrid(fragInput.uv * Mat.PrimaryGridSize,   float2(Mat.LineWidth));
    float bg = PristineGrid(fragInput.uv * Mat.SecondaryGridSize, float2(Mat.LineWidth));
    float gridAlpha = max(sg, bg);

    float3 color = Mat.GridColor.rgb;

    float dzPerPx = length(float2(ddx(fragInput.uv.y), ddy(fragInput.uv.y)));
    float dxPerPx = length(float2(ddx(fragInput.uv.x), ddy(fragInput.uv.x)));

    float xAxis = 1.0 - smoothstep(0.0, dzPerPx * 1.5, abs(fragInput.uv.y));
    float zAxis = 1.0 - smoothstep(0.0, dxPerPx * 1.5, abs(fragInput.uv.x));

    color     = lerp(color, float3(0.9, 0.2, 0.2), xAxis);
    gridAlpha = max(gridAlpha, xAxis * 0.9);
    color     = lerp(color, float3(0.2, 0.4, 0.9), zAxis);
    gridAlpha = max(gridAlpha, zAxis * 0.9);

    float  dist    = length(fragInput.viewPos);
    float  fade    = 1.0 - pow(clamp(dist / Mat.MaxDist, 0.0, 1.0), Mat.Falloff);

    return float4(color, gridAlpha * Mat.GridColor.a * fade);
}
