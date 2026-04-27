import FragmentInput;
import Material;
import ShaderVariables;
import Lighting;
import Math;

import ProwlLight;
import ProwlShadow;


module ProwlLighting;


#define SPEC_AA_VARIANCE 0.5
#define SPEC_AA_THRESHOLD 0.1


public struct AmbientData
{
    // ============================================================
    //  Fog uniforms
    // ============================================================

    public float4 _FogColor;
    public float4 _FogParams;  // x: density/sqrt(ln(2)) for Exp2, y: density/ln(2) for Exp, z: -1/(end-start) for Linear, w: end/(end-start) for Linear
    public float3 _FogStates;  // x: linear enabled, y: exp enabled, z: exp2 enabled

    // ============================================================
    //  Ambient lighting uniforms
    // ============================================================

    public float2 _AmbientMode;  // x: uniform enabled, y: hemisphere enabled
    public float4 _AmbientColor;
    public float4 _AmbientSkyColor;
    public float4 _AmbientGroundColor;
    public float _AmbientStrength;
}
public ParameterBlock<AmbientData> Ambient;


static float ApplySpecularAA(float roughness, float3 worldNormal)
{
    float3 ddxN = ddx(worldNormal);
    float3 ddyN = ddy(worldNormal);
    float variance = SPEC_AA_VARIANCE * (dot(ddxN, ddxN) + dot(ddyN, ddyN));
    float kernelRoughnessSq = min(2.0 * variance, SPEC_AA_THRESHOLD);
    float squaredRoughness = clamp(roughness * roughness + kernelRoughnessSq, 0.0, 1.0);
    return sqrt(squaredRoughness);
}


static float3 CalculateTranslucency(float3 lightDir, float3 viewDir, float3 normal, float translucency, float scatteringPower, float distortion, float scale, float3 lightColor)
{
    if (translucency <= 0.0)
        return float3(0.0);

    float3 lightScattering = float3(0.0);

    if (scatteringPower < 0.001)
    {
        float wrap = 0.5;
        float wrappedNdotL = clamp((dot(-normal, lightDir) + wrap) / ((1.0 + wrap) * (1.0 + wrap)), 0.0, 1.0);

        float VdotL = clamp(dot(viewDir, -lightDir), 0.0, 1.0);
        float a2 = 0.49;
        float d = (VdotL * a2 - VdotL) * VdotL + 1.0;
        float GGX = (a2 / PI) / (d * d);

        lightScattering = wrappedNdotL * GGX * translucency * lightColor;
    }
    else
    {
        float3 transLightDir = lightDir + normal * distortion;
        float transDot = dot(-transLightDir, viewDir);
        transDot = exp2(clamp(transDot, 0.0, 1.0) * scatteringPower - scatteringPower) * translucency;

        lightScattering = transDot * lightColor;
    }

    return lightScattering * scale;
}


// ============================================================
//  Ambient lighting
// ============================================================

static float3 CalculateAmbient(float3 worldNormal)
{
    float3 ambient = float3(0.0);
    ambient += Ambient._AmbientColor.rgb * Ambient._AmbientMode.x;

    float upDot = dot(worldNormal, float3(0.0, 1.0, 0.0));
    ambient += lerp(Ambient._AmbientGroundColor.rgb, Ambient._AmbientSkyColor.rgb, upDot * 0.5 + 0.5) * Ambient._AmbientMode.y;

    return ambient;
}


// ============================================================
//  Fog
// ============================================================

static float3 ApplyFog(float3 color, float3 worldPos, float3 cameraPos)
{
    float3 fogStates = Ambient._FogStates;
    float4 fogParams = Ambient._FogParams;

    if (fogStates.x + fogStates.y + fogStates.z < 0.5)
        return color;

    float fogCoord = length(worldPos - cameraPos.xyz);
    float prowlFog = 0.0;
    prowlFog += (fogCoord * fogParams.z + fogParams.w) * fogStates.x;
    prowlFog += exp2(-fogCoord * fogParams.y) * fogStates.y;
    prowlFog += exp2(-fogCoord * fogCoord * fogParams.x * fogParams.x) * fogStates.z;
    return lerp(Ambient._FogColor.rgb, color, clamp(prowlFog, 0.0, 1.0));
}


#define MAX_FORWARD_LIGHTS 8

// ============================================================
//  Main forward lighting entry point
// ============================================================

public struct Unlit<Variables : IShaderVariables> : ILighting
{
    public void Compute(IFragmentInput input, inout SurfaceInfo surface, IBRDF brdf)
    {
        float3 diffuseColor = surface.BaseColor.xyz * (1.0 - surface.Metallic);
        float3 ambient      = CalculateAmbient(input.Normal()) * diffuseColor * surface.AO * Ambient._AmbientStrength;
        surface.LightingAccum = ApplyFog(ambient + surface.Emission, input.Position(), Variables::WorldSpaceCameraPos());
    }
}


// ============================================================
//  Main forward lighting entry point
// ============================================================

public struct ForwardLit<Variables : IShaderVariables> : ILighting
{
    public ProwlLight<ProwlShadow> Lights[MAX_FORWARD_LIGHTS];
    public int LightCount;


    public void Compute(IFragmentInput input, inout SurfaceInfo surface, IBRDF brdf)
    {
        surface.Roughness = ApplySpecularAA(surface.Roughness, input.Normal());

        float3 totalLight = float3(0.0);
        for (int i = 0; i < LightCount && i < MAX_FORWARD_LIGHTS; i++)
            totalLight += Lights[i].Evaluate(input, surface, brdf);

        float3 diffuseColor = surface.BaseColor.xyz * (1.0 - surface.Metallic);
        float3 ambient      = CalculateAmbient(input.Normal()) * diffuseColor * surface.AO * Ambient._AmbientStrength;
        surface.LightingAccum = ApplyFog(ambient + totalLight + surface.Emission, input.Position(), Variables::WorldSpaceCameraPos());
    }
}


public struct ForwardLitTranslucent<Variables : IShaderVariables> : ILighting
{
    public ProwlLight<ProwlShadow> Lights[MAX_FORWARD_LIGHTS];
    public int LightCount;

    public void Compute(IFragmentInput input, inout SurfaceInfo surface, IBRDF brdf)
    {
        surface.Roughness = ApplySpecularAA(surface.Roughness, input.Normal());

        float3 viewDir    = normalize(Variables::WorldSpaceCameraPos().xyz - input.Position());
        float3 totalLight = float3(0.0);

        for (int i = 0; i < LightCount && i < MAX_FORWARD_LIGHTS; i++)
            totalLight += Lights[i].Evaluate(input, surface, brdf);

        if (surface.Translucency > 0.0)
        {
            for (int i = 0; i < LightCount && i < MAX_FORWARD_LIGHTS; i++)
            {
                float3 lightDir = Lights[i].Type == LightType.Directional
                    ? normalize(Lights[i].LightDirection)
                    : normalize(Lights[i].LightPosition - input.Position());

                float3 lightColor = Lights[i].LightColor * Lights[i].LightIntensity;
                totalLight += CalculateTranslucency(lightDir, viewDir, input.Normal(),
                    surface.Translucency, surface.ScatteringPower, surface.ScatteringDistortion,
                    surface.ScatteringScale, lightColor) * surface.BaseColor.xyz;
            }
        }

        float3 diffuseColor = surface.BaseColor.xyz * (1.0 - surface.Metallic);
        float3 ambient      = CalculateAmbient(input.Normal()) * diffuseColor * surface.AO * Ambient._AmbientStrength;
        surface.LightingAccum = ApplyFog(ambient + totalLight + surface.Emission, input.Position(), Variables::WorldSpaceCameraPos());
    }
}
