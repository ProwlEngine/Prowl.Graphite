import ShaderVariables;
import FragmentInput;


module ProwlFragmentInput;


public struct ProwlFragmentInput : IFragmentInput
{
    public float4 svPosition : SV_Position;

    public float3 position : POSITION;
    public float2 uv0 : UV0;
    public float3 normal : NORMAL;
    public float4 tangent : TANGENT;
    public float4 bitangent : BITANGENT;
    public float4 color : COLOR;


    public float4 ClipPosition() { return svPosition; }
    public float3 Position() { return position; }
    public float2 UVChannel<int index>() { return uv0; }
    public float3 Normal() { return normal; }
    public float4 Tangent() { return tangent; }
    public float4 Bitangent() { return bitangent; }
    public float4 Color() { return color; }
}
