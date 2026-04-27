import VertexInput;


module ProwlMeshInput;


// Default values for mesh inputs
// Having the vertex inputs available, if unused/unbound does not affect performance in any way,
// so there's no need to put them behind compiler defines.
public struct ProwlMeshInput : IMeshInput
{
    float3 _position : PROWL_POSITION;
    float2 _uv0 : PROWL_TEXCOORD0;
    float2 _uv1 : PROWL_TEXCOORD1;
    float3 _normal : PROWL_NORMAL;
    float4 _tangent : PROWL_TANGENT;
    float4 _color : PROWL_COLOR;

    public float3 Position() { return _position; }
    public float3 Normal() { return _normal; }
    public float4 Tangent() { return _tangent; }
    public float4 Color() { return _color; }

    // Generics allow this to compile to a static branch
    public float2 UVChannel<int index>()
    {
        if (index == 1)
            return _uv1;

        return _uv0;
    }
}
