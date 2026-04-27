import Material;
import FragmentInput;
import Math;


module ProwlBRDF;



// =============================================================
//  Standard Isotropic BRDF (GGX + Smith + Disney Diffuse)
// =============================================================

public struct StandardBRDF : IBRDF
{
    // GGX/Trowbridge-Reitz Normal Distribution Function
    // Input roughness is perceptual roughness [0,1] — squared internally to get alpha.
    private static float DistributionGGX(float3 N, float3 H, float roughness)
    {
        float a2     = roughness * roughness;
        float NdotH  = max(dot(N, H), 0.0);
        float NdotH2 = NdotH * NdotH;

        float denom = (NdotH2 * (a2 - 1.0) + 1.0);
        denom = PI * denom * denom;

        return a2 / denom;
    }

    // Schlick-GGX Geometry Function (single direction)
    // Uses k = (roughness + 1)² / 8 for direct/analytical lights.
    private static float GeometrySchlickGGX(float NdotV, float roughness)
    {
        float r = (roughness + 1.0);
        float k = (r * r) / 8.0;

        return NdotV / (NdotV * (1.0 - k) + k);
    }

    // Smith Geometry Function (both directions combined)
    // Uses abs(NdotV) to handle backface normals gracefully.
    private static float GeometrySmith(float3 N, float3 V, float3 L, float roughness)
    {
        float NdotV = abs(dot(N, V));
        float NdotL = max(dot(N, L), 0.0);
        float ggx2  = GeometrySchlickGGX(NdotV, roughness);
        float ggx1  = GeometrySchlickGGX(NdotL, roughness);

        return ggx1 * ggx2;
    }

    // Schlick Fresnel Approximation (fast exp2 variant)
    private static float3 FresnelSchlick(float cosTheta, float3 F0)
    {
        return F0 + (1.0 - F0) * exp2(-9.28 * cosTheta);
    }

    // Fresnel with roughness for IBL/indirect specular (Lazarov Environmental BRDF)
    private static float3 FresnelSchlickRoughness(float cosTheta, float3 F0, float roughness)
    {
        float3 F90 = max(float3(1.0 - roughness), F0);
        return F0 + (F90 - F0) * exp2(-9.28 * cosTheta);
    }

    // Disney Diffuse (Burley 2012) — roughness-dependent, darkens at grazing angles
    // for rough surfaces. More physically accurate than Lambertian.
    private static float DisneyDiffuse(float NdotV, float NdotL, float LdotH, float perceptualRoughness)
    {
        float fd90        = 0.5 + 2.0 * LdotH * LdotH * perceptualRoughness;
        float lightScatter = 1.0 + (fd90 - 1.0) * exp2(-8.35 * NdotL);
        float viewScatter  = 1.0 + (fd90 - 1.0) * exp2(-8.35 * NdotV);
        return lightScatter * viewScatter / PI;
    }

    // Returns (diffuse + specular) * NdotL * AO for a single analytical light.
    // Radiance, attenuation, and shadow are ILighting concerns — multiply them in externally.
    public float3 Eval(IFragmentInput input, in const SurfaceInfo surface, float3 L, float3 V, float3 N)
    {
        float3 H    = normalize(L + V);
        float NdotL = max(dot(N, L), 0.0);
        float NdotV = abs(dot(N, V));
        float LdotH = max(dot(L, H), 0.0);

        float3 F0 = lerp(float3(0.04), surface.BaseColor, surface.Metallic);

        float  D = DistributionGGX(N, H, surface.Roughness);
        float  G = GeometrySmith(N, V, L, surface.Roughness);
        float3 F = FresnelSchlick(LdotH, F0);

        float3 kD       = (float3(1.0) - F) * (1.0 - surface.Metallic);
        float3 specular = (D * G * F) / (4.0 * NdotV * NdotL + 0.0001);
        float3 diffuse  = kD * surface.BaseColor * DisneyDiffuse(NdotV, NdotL, LdotH, surface.Roughness);

        return (diffuse + specular) * NdotL * surface.AO;
    }
}


// =============================================================
//  Anisotropic BRDF (Far Cry 4 / GGX Aniso, Lux convention)
//  For brushed metal, hair, and directional surfaces.
// =============================================================

public struct AnisotropicBRDF : IBRDF
{
    float _anisotropy = 0.5; // [-1, 1]: negative = bitangent-aligned, positive = tangent-aligned
    float _mt;
    float _mb;

    public __init(float anisotropy, float roughness)
    {
        _anisotropy = anisotropy;

        // Split roughness along tangent/bitangent axes using anisotropy
        float roughnessT = roughness * (1.0 + _anisotropy);
        float roughnessB = roughness * (1.0 - _anisotropy);
        _mt         = roughnessT * roughnessT;
        _mb         = roughnessB * roughnessB;
    }


    // Anisotropic GGX NDF
    // mt and mb are SQUARED roughness values (mt = roughnessT², mb = roughnessB²)
    private static float DistributionGGXAniso(float TdotH, float BdotH, float NdotH, float mt, float mb)
    {
        float d = TdotH * TdotH / (mt * mt) + BdotH * BdotH / (mb * mb) + NdotH * NdotH;
        return 1.0 / (PI * mt * mb * d * d);
    }

    // Anisotropic Smith Joint Visibility (Far Cry 4, Lux convention)
    // Return value already includes the 1/(4*NdotV*NdotL) denominator — no separate divide needed.
    // mt and mb are SQUARED roughness values (mt = roughnessT², mb = roughnessB²)
    private static float GeometrySmithAniso(
        float TdotV, float BdotV, float NdotV,
        float TdotL, float BdotL, float NdotL,
        float mt, float mb)
    {
        float lambdaV = NdotL * sqrt(mt * TdotV * TdotV + mb * BdotV * BdotV + NdotV * NdotV);
        float lambdaL = NdotV * sqrt(mt * TdotL * TdotL + mb * BdotL * BdotL + NdotL * NdotL);
        return 0.5 / max(1e-5, lambdaV + lambdaL);
    }

    private static float3 FresnelSchlick(float cosTheta, float3 F0)
    {
        return F0 + (1.0 - F0) * exp2(-9.28 * cosTheta);
    }

    // Disney Diffuse (Burley 2012)
    private static float DisneyDiffuse(float NdotV, float NdotL, float LdotH, float perceptualRoughness)
    {
        float fd90        = 0.5 + 2.0 * LdotH * LdotH * perceptualRoughness;
        float lightScatter = 1.0 + (fd90 - 1.0) * exp2(-8.35 * NdotL);
        float viewScatter  = 1.0 + (fd90 - 1.0) * exp2(-8.35 * NdotV);
        return lightScatter * viewScatter / PI;
    }

    // Returns (kD * diffuse + specular * F) * AO for a single analytical light (Lux convention).
    // Radiance, attenuation, and shadow are ILighting concerns — multiply them in externally.
    public float3 Eval(IFragmentInput input, in const SurfaceInfo surface, float3 L, float3 V, float3 N)
    {
        float3 T = input.Tangent().xyz;
        float3 B = input.Bitangent().xyz;

        float3 H    = normalize(L + V);
        float NdotL = max(dot(N, L), 0.0);
        float NdotV = abs(dot(N, V));
        float NdotH = max(dot(N, H), 0.0);
        float LdotH = max(dot(L, H), 0.0);
        float TdotH = dot(T, H);
        float BdotH = dot(B, H);
        float TdotV = dot(T, V);
        float BdotV = dot(B, V);
        float TdotL = dot(T, L);
        float BdotL = dot(B, L);

        float3 F0 = lerp(float3(0.04), surface.BaseColor, surface.Metallic);
        float3 F  = FresnelSchlick(LdotH, F0);

        float  D            = DistributionGGXAniso(TdotH, BdotH, NdotH, _mt, _mb);
        float  G            = GeometrySmithAniso(TdotV, BdotV, NdotV, TdotL, BdotL, NdotL, _mt, _mb);
        float  specularTerm = max(0.0, G * D * PI * NdotL);
        float3 kD           = (float3(1.0) - F) * (1.0 - surface.Metallic);
        float  diffuseTerm  = DisneyDiffuse(NdotV, NdotL, LdotH, surface.Roughness) * NdotL;

        return (kD * surface.BaseColor * diffuseTerm + specularTerm * F) * surface.AO;
    }
}
