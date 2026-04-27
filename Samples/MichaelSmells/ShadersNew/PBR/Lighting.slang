import FragmentInput;
import Material;


module Lighting;


public enum LightType
{
    Directional,
    Point,
    Spot,
}


public interface ILight
{
    public float3 Evaluate(IFragmentInput input, inout SurfaceInfo surface, IBRDF brdf);
}


public interface IShadow
{
    // Returns shadow occlusion in [0, 1]: 0 = fully lit, 1 = fully shadowed.
    // lightDir is the unit direction FROM the surface TO the light (for slope bias).
    public float Sample(float3 worldPos, float3 worldNormal, float3 lightDir);
}
