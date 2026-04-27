import FragmentInput;
import Material;
import Lighting;
import ShaderVariables;
import ProwlVariables;


module ProwlLight;


public struct ProwlLight<Shadow> : ILight where optional Shadow : IShadow
{
    public LightType Type;
    public float3 LightPosition;
    public float3 LightDirection;
    public float3 LightColor;
    public float LightIntensity;
    public float LightRange;
    public float LightSpotAngle;
    public float LightInnerSpotAngle;
    public Shadow ShadowData;


    public float3 Evaluate(IFragmentInput input, inout SurfaceInfo surface, IBRDF brdf)
    {
        float3 worldPos    = input.Position();
        float3 worldNormal = input.Normal();
        float3 viewDir     = normalize(ProwlVariables::WorldSpaceCameraPos() - worldPos);

        float3 lightDir;
        float  attenuation = 1.0;

        if (Type == LightType.Directional)
        {
            lightDir = normalize(LightDirection);
        }
        else
        {
            float3 lightToPixel = worldPos - LightPosition;
            float  dist         = length(lightToPixel);
            lightDir = normalize(-lightToPixel);

            float distAtten  = 1.0 / (dist * dist + 1.0);
            float rangeAtten = 1.0 - smoothstep(LightRange * 0.8, LightRange, dist);
            attenuation = distAtten * rangeAtten;

            if (Type == LightType.Spot)
            {
                float outerCos      = cos(radians(LightSpotAngle));
                float innerCos      = cos(radians(LightInnerSpotAngle));
                float lightAngleCos = dot(normalize(LightDirection), normalize(lightToPixel));
                attenuation *= smoothstep(outerCos, innerCos, lightAngleCos);
            }

            if (attenuation <= 0.0001) return float3(0.0);
        }

        float shadowFactor = 1.0;

        if (Shadow is IShadow)
            shadowFactor = 1.0 - ShadowData.Sample(worldPos, worldNormal, lightDir);

        float3 radiance = LightColor * LightIntensity * attenuation;
        return brdf.Eval(input, surface, lightDir, viewDir, worldNormal) * radiance * shadowFactor;
    }
}
