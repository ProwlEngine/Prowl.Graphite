import VertexInput;


module ProwlSkinnedInput;


public struct BoneData
{
    public Texture2D<float4> prowl_BoneMatrixTexture;
	public int prowl_BoneCount;
};
ParameterBlock<BoneData> Bones;


// Optional skinning data
public struct ProwlSkinnedInput : ISkinningInput
{
    const int MAX_BONE_INFLUENCE = 4;

    uint4 _boneIndices : PROWL_BONEINDICES;
    float4 _boneWeights : PROWL_BONEWEIGHTS;


    public static const float4x4 GetBoneMatrix(int boneIndex)
    {
		int texOffset = boneIndex * 4;
		float4 col0 = Bones.prowl_BoneMatrixTexture.Load(int3(texOffset + 0, 0, 0), 0);
		float4 col1 = Bones.prowl_BoneMatrixTexture.Load(int3(texOffset + 1, 0, 0), 0);
		float4 col2 = Bones.prowl_BoneMatrixTexture.Load(int3(texOffset + 2, 0, 0), 0);
		float4 col3 = Bones.prowl_BoneMatrixTexture.Load(int3(texOffset + 3, 0, 0), 0);
		return float4x4(col0, col1, col2, col3);
	}

    public uint4 BoneIndices() { return _boneIndices; }
    public float4 BoneWeights() { return _boneWeights; }


	public float4 GetSkinnedPosition(float3 position)
	{
		float4 skinnedPos = float4(0.0);
        float4 weight = BoneWeights();
        uint4 indices = BoneIndices();

		for (int i = 0; i < MAX_BONE_INFLUENCE; i++)
		{
			int boneIndex = int(indices[i]);
			float weight = weight[i];
			if (boneIndex > 0 && weight > 0.0 && boneIndex <= Bones.prowl_BoneCount)
			{
				float4x4 boneTransform = GetBoneMatrix(boneIndex - 1);
				skinnedPos += mul(boneTransform, float4(position, 1.0)) * weight;
			}
		}

		float totalWeight = weight.x + weight.y + weight.z + weight.w;

        if (totalWeight < 0.01)
			skinnedPos = float4(position, 1.0);

		return skinnedPos;
	}


	public float3 GetSkinnedNormal(float3 normal)
	{
		float3 skinnedNormal = float3(0.0);
        float4 weight = BoneWeights();
        uint4 indices = BoneIndices();

		for (int i = 0; i < MAX_BONE_INFLUENCE; i++)
		{
			int boneIndex = int(indices[i]);
			float weight = weight[i];
			if (boneIndex > 0 && weight > 0.0 && boneIndex <= Bones.prowl_BoneCount)
            {
				float4x4 boneTransform = GetBoneMatrix(boneIndex - 1);
				skinnedNormal += mul(float3x3(boneTransform), normal) * weight;
			}
		}

		float totalWeight = weight.x + weight.y + weight.z + weight.w;

		if (totalWeight < 0.01)
			skinnedNormal = normal;

        return normalize(skinnedNormal);
	}
}
