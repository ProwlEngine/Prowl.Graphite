import ShaderVariables;


module FragmentInput;


public interface IFragmentInput
{
    public float4 ClipPosition();
    public float3 Position();
    public float2 UVChannel<int index>();
    public float3 Normal();
    public float4 Tangent();
    public float4 Bitangent();
    public float4 Color();
}
