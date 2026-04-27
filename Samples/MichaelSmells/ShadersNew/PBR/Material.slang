import FragmentInput;


module Material;


public struct SurfaceInfo
{
    public float2 FinalUV;

    public float3 ViewDir;

    public float4 BaseColor;
    public float Metallic;
    public float Roughness;
    public float AO;

    public float3 Emission;

    // Optional extensions
    public float Translucency;
    public float Thickness;
    public float ScatteringPower;
    public float ScatteringDistortion;
    public float ScatteringScale;

    public float3 LightingAccum;
}


public interface ISurface
{
    public void GetSurface(IFragmentInput input, inout SurfaceInfo surface);

    public float3 Normal(IFragmentInput input, float2 uv);

    public bool GetCutoff(float2 uv);
}


public interface IUVStage
{
    public void Apply(IFragmentInput input, inout SurfaceInfo surface);
}


public interface IBRDF
{
    public float3 Eval(IFragmentInput input, in const SurfaceInfo surface, float3 L, float3 V, float3 N);
}


public interface ILighting
{
    public void Compute(IFragmentInput input, inout SurfaceInfo surface, IBRDF brdf);
}
