import VertexInput;
import FragmentInput;
import Material;
import Math;


public struct SurfaceProperties
{
    public float4 MainColor;
    public float TransluscencyStrength;
    public float EmissionIntensity;
    public float  AlphaCutoff;
    public float  ScatteringPower;
    public float  ScatteringDistortion;
    public float  ScatteringScale;
    public float  Anisotropy;
}
ParameterBlock<SurfaceProperties> SurfaceData;


public Texture2D<float4> AlbedoTexture;
public SamplerState AlbedoSampler;

public Texture2D<float4> NormalTexture;
public SamplerState NormalSampler;

public Texture2D<float4> SurfaceTexture;
public SamplerState SurfaceSampler;

public Texture2D<float4> TransluscencyTexture;
public SamplerState TransluscencySampler;

public Texture2D<float4> EmissionTexture;
public SamplerState EmissionSampler;


public struct StandardSurface : ISurface
{
    public void GetSurface(IFragmentInput input, inout SurfaceInfo surface)
    {
        // --- Albedo ---
        float4 albedo = AlbedoTexture.Sample(AlbedoSampler, surface.FinalUV) * input.Color() * SurfaceData.MainColor;
        float4 baseColor = float4(gammaToLinearSpace(albedo.rgb), albedo.a);

        // --- Normal mapping ---
        float3 worldNormal = ApplyNormalMap(NormalTexture, NormalSampler, surface.FinalUV, input.Normal(), input.Tangent().xyz, input.Bitangent().xyz);

        // --- Surface: R = AO, G = Roughness, B = Metallic ---
        float4 surf = SurfaceTexture.Sample(SurfaceSampler, surface.FinalUV);
        float ao = 1.0 - surf.r;
        float roughness = surf.g;
        float metallic = surf.b;

        // --- Translucency map: G = extra occlusion, B = thickness ---
        float4 transOcc = TransluscencyTexture.Sample(TransluscencySampler, surface.FinalUV);
        ao *= transOcc.g; // Combine surface AO with translucency occlusion
        float translucency = transOcc.b * SurfaceData.TransluscencyStrength;

        // --- Emission ---
        float3 emission = EmissionTexture.Sample(EmissionSampler, surface.FinalUV).rgb * SurfaceData.EmissionIntensity;

        surface.AO = ao;
        surface.BaseColor = baseColor;
        surface.Emission = emission;
        surface.Metallic = metallic;
        surface.Roughness = roughness;
        surface.Translucency = translucency;
    }


    public float3 Normal(IFragmentInput input, float2 surface)
    {
        // --- Normal mapping ---
        return ApplyNormalMap(NormalTexture, NormalSampler, surface, input.Normal(), input.Tangent().xyz, input.Bitangent().xyz);
    }


    public bool GetCutoff(float2 uv)
    {
        // Alpha cutoff
        float alpha = AlbedoTexture.Sample(AlbedoSampler, uv).a * SurfaceData.MainColor.a;
        if (alpha < SurfaceData.AlphaCutoff)
            discard;
    }
}


public struct StandardSurfaceSimple : ISurface
{
    public void GetSurface(IFragmentInput input, inout SurfaceInfo surface)
    {
        // --- Albedo ---
        float4 albedo = AlbedoTexture.Sample(AlbedoSampler, surface.FinalUV) * input.Color() * SurfaceData.MainColor;
        float4 baseColor = float4(gammaToLinearSpace(albedo.rgb), albedo.a);

        // --- Normal mapping ---
        float3 worldNormal = Normal(input, surface.FinalUV);

        // --- Surface: R = AO, G = Roughness, B = Metallic ---
        float4 surf = SurfaceTexture.Sample(SurfaceSampler, surface.FinalUV);
        float ao = 1.0 - surf.r;
        float roughness = surf.g;
        float metallic = surf.b;

        // --- Emission ---
        float3 emission = EmissionTexture.Sample(EmissionSampler, surface.FinalUV).rgb * SurfaceData.EmissionIntensity;

        surface.AO = ao;
        surface.BaseColor = baseColor;
        surface.Emission = emission;
        surface.Metallic = metallic;
        surface.Roughness = roughness;
    }


    public float3 Normal(IFragmentInput input, float2 uv)
    {
        // --- Normal mapping ---
        return ApplyNormalMap(NormalTexture, NormalSampler, uv, input.Normal(), input.Tangent().xyz, input.Bitangent().xyz);
    }


    public bool GetCutoff(float2 uv)
    {
        // Alpha cutoff
        float alpha = AlbedoTexture.Sample(AlbedoSampler, uv).a * SurfaceData.MainColor.a;
        if (alpha < SurfaceData.AlphaCutoff)
            discard;
    }
}
