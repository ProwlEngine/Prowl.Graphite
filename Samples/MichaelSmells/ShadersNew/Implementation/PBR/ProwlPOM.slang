import Material;
import ShaderVariables;
import FragmentInput;


module ProwlPOM;


public struct POMData
{
    public float ParallaxHeight;
    public int ParallaxSteps;
}
ParameterBlock<POMData> PomData;

public Texture2D<float4> ParallaxMap;
public SamplerState ParallaxSampler;


public struct POMRemapper<Variables : IShaderVariables> : IUVStage
{
    public void Apply(IFragmentInput input, inout SurfaceInfo surface)
    {
        if (PomData.ParallaxHeight > 0.0 && PomData.ParallaxSteps > 0)
        {
            float3 N = normalize(input.Normal());
            float3 T = normalize(input.Tangent());
            float3 B = normalize(input.Bitangent());
            float3x3 TBN = float3x3(T, B, N);

            float3 viewDirWorld = Variables::WorldSpaceCameraPos().xyz - input.Position();
            // Transform to tangent space, then normalize — preserves direction accuracy
            float3 viewDirTS = normalize(mul(transpose(TBN), viewDirWorld));

            surface.FinalUV = ParallaxOcclusionMapping(input.UVChannel<0>(), viewDirTS);
        }
    }


    // =============================================================
    //  Parallax Occlusion Mapping (POM)
    //  Linear ray march + secant-method binary refinement.
    //
    //  Usage:
    //    float2 pomUV = ParallaxOcclusionMapping(heightTex, uv, viewDirTS, heightScale, steps);
    //
    //  viewDirTS must be in tangent space (use transpose(TBN) * viewDir).
    //  Height is sampled from the G channel of the height texture.
    // =============================================================

    float2 ParallaxOcclusionMapping(float2 uv, float3 viewDirTS)
    {
        // Slope damping: reduce parallax at grazing angles
        float slopeDamp = 1.0 - clamp(dot(viewDirTS, float3(0.0, 0.0, 1.0)), 0.0, 1.0);

        // Maximum UV offset based on view angle and height scale
        float2 vMaxOffset = (viewDirTS.xy / -viewDirTS.z) * PomData.ParallaxHeight * (1.0 - slopeDamp * slopeDamp);

        float fStepSize = 1.0 / float(PomData.ParallaxSteps);
        float2 finalStepSize = fStepSize * vMaxOffset;

        // Pre-compute texture derivatives for correct mip selection
        float2 dx = ddx(uv);
        float2 dy = ddy(uv);

        // Linear ray march from surface (1.0) downward
        float fCurrRayHeight = 1.0;
        float2 vCurrOffset = float2(0.0);
        float2 vLastOffset = float2(0.0);
        float fLastSampledHeight = 1.0;
        float fCurrSampledHeight = 1.0;

        for (int i = 0; i < PomData.ParallaxSteps; i++)
        {
            fCurrSampledHeight = ParallaxMap.SampleGrad(ParallaxSampler, uv + vCurrOffset, dx, dy).g;

            if (fCurrSampledHeight > fCurrRayHeight)
                break;

            fCurrRayHeight -= fStepSize;
            vLastOffset = vCurrOffset;
            vCurrOffset += finalStepSize;
            fLastSampledHeight = fCurrSampledHeight;
        }

        // Secant-method binary refinement (3 iterations)
        float pt0 = fCurrRayHeight + fStepSize;
        float pt1 = fCurrRayHeight;
        float delta0 = pt0 - fLastSampledHeight;
        float delta1 = pt1 - fCurrSampledHeight;

        for (int j = 0; j < 3; j++)
        {
            float intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
            vCurrOffset = (1.0 - intersectionHeight) * finalStepSize * float(PomData.ParallaxSteps);

            fCurrSampledHeight = ParallaxMap.SampleGrad(ParallaxSampler, uv + vCurrOffset, dx, dy).g;

            float delta = intersectionHeight - fCurrSampledHeight;
            if (delta < 0.0)
            {
                delta1 = delta;
                pt1 = intersectionHeight;
            }
            else
            {
                delta0 = delta;
                pt0 = intersectionHeight;
            }
        }

        return uv + vCurrOffset;
    }
}
