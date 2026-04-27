import ShaderVariables;


module VertexInput;


public interface IMeshInput
{
    public float3 Position();
    public float2 UVChannel<int index>();
    public float3 Normal();
    public float4 Tangent();
    public float4 Color();
}


// Optional instance data
public interface IInstanceInput
{
    public float4 Row<int index>();
    public float4 InstanceColor();
    public float4 CustomData();
}


public interface ISkinningInput
{
    public uint4 BoneIndices();
    public float4 BoneWeights();

    public static const float4x4 GetBoneMatrix(int boneIndex);

    public float4 GetSkinnedPosition(float3 position);
    public float3 GetSkinnedNormal(float3 normal);
}


public struct VertexInputs<Variables, Mesh, Instance, Skin>
    where Variables : IShaderVariables
    where Mesh : IMeshInput
    where optional Instance : IInstanceInput
    where optional Skin : ISkinningInput
{
    public Mesh MeshInputs;
    public Instance InstanceInputs;
    public Skin SkinningInputs;


    public float4x4 GetModelMatrix()
    {
        if (Instance is IInstanceInput)
    	    return float4x4(
                InstanceInputs.Row<0>(),
                InstanceInputs.Row<1>(),
                InstanceInputs.Row<2>(),
                InstanceInputs.Row<3>());

    	return Variables::ObjectToWorld();
    }


    // Returns the Model-View-Projection matrix, accounting for GPU instancing.
    public float4x4 GetMVPMatrix()
    {
        if (Instance is IInstanceInput)
    	    return Variables::ViewProjection() * GetModelMatrix();

    	return Variables::MVP();
    }


    public float3 WorldPosition()
    {
        return TransformPosition(MeshInputs.Position());
    }


    // Transform a position to world space (applies skinning if active, then model matrix).
    public float3 TransformPosition(float3 position)
    {
    	float4x4 model = GetModelMatrix();

        if (Skin is ISkinningInput)
    	    return mul(model, SkinningInputs.GetSkinnedPosition(position)).xyz;

    	return mul(model, float4(position, 1.0)).xyz;
    }


    public float4 ClipPosition()
    {
        return TransformClip(MeshInputs.Position());
    }


    // Transform a position to clip space (applies skinning if active, then MVP matrix).
    public float4 TransformClip(float3 position)
    {
    	float4x4 mvp = GetMVPMatrix();

        if (Skin is ISkinningInput)
    	    position = SkinningInputs.GetSkinnedPosition(position).xyz;

    	return mul(mvp, float4(position, 1.0));
    }


    // Transform a direction/normal to world space (applies skinning if active, then model rotation).
    public float3 TransformDirection(float3 dir)
    {
    	float4x4 model = GetModelMatrix();

        if (Skin is ISkinningInput)
    	    dir = SkinningInputs.GetSkinnedNormal(dir);

    	return normalize(mul(float3x3(model), dir));
    }


    // Get vertex color with per-instance tint applied.
    public float4 GetInstanceColor()
    {
        if (Instance is IInstanceInput)
        	return MeshInputs.Color() * InstanceInputs.InstanceColor();

    	return MeshInputs.Color();
    }


    // Get per-instance custom data (available only with GPU instancing).
    public float4 GetInstanceCustomData()
    {
        if (Instance is IInstanceInput)
    	    return InstanceInputs.CustomData();

    	return float4(0.0);
    }
}
