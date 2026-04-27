module Random;


public static uint triple32(uint x)
{
	// https://nullprogram.com/blog/2018/07/31/
	x ^= x >> 17;
	x *= 0xed5ad4bbu;
	x ^= x >> 11;
	x *= 0xac4c1b51u;
	x ^= x >> 15;
	x *= 0x31848babu;
	x ^= x >> 14;
	return uint(x);
}


// Simple hash-based random for per-pixel variation
public static float hash1(float2 p)
{
	float3 p3 = fract(float3(p.xyx) * 443.897);
	p3 += dot(p3, p3.zyx + 19.19);
	return fract((p3.x + p3.y) * p3.z);
}


public static float2 hash2(float2 p)
{
	float3 p3 = fract(float3(p.xyx) * float3(443.897, 441.423, 437.195));
	p3 += dot(p3, p3.yzx + 19.19);
	return fract((p3.xx + p3.yz) * p3.zy);
}


public static float randF (uint seed) { return float(triple32(seed)) / float(0xffffffffu); }

public static float2 rand2F(uint2 seed) { return float2(triple32(seed.x), triple32(seed.y)) / float(0xffffffffu); }


public struct NoiseGenerator
{
	uint currentNum;


    public static NoiseGenerator Create(float4 glFragCoord, float screenWidth)
    {
        // Create seed from pixel coordinates
        uint x = uint(glFragCoord.x);
        uint y = uint(glFragCoord.y);
        uint seed = triple32(x + y * uint(screenWidth));

        NoiseGenerator gen;
        gen.currentNum = seed;
        return gen;
    }


    public static NoiseGenerator Create(float4 glFragCoord, uint frameIndex, float screenWidth)
    {
        // Create seed from pixel coordinates + frame index for temporal variation
        uint x = uint(glFragCoord.x);
        uint y = uint(glFragCoord.y);
        uint seed = triple32(x + y * uint(screenWidth) + frameIndex * 1973u);

        NoiseGenerator gen;
        gen.currentNum = seed;
        return gen;
    }


    [mutating]
    public uint RandNext() { return (currentNum = triple32(currentNum)); }

    [mutating]
    public uint2 RandNext2() { return uint2(RandNext(), RandNext()); }

    [mutating]
    public uint3 RandNext3() { return uint3(RandNext2(), RandNext()); }

    [mutating]
    public uint4 RandNext4() { return uint4(RandNext3(), RandNext()); }

    [mutating]
    public float RandNextF() { return float(RandNext()) / float(0xffffffffu); }

    [mutating]
    public float2 RandNext2F() { return float2(RandNext2()) / float(0xffffffffu); }

    [mutating]
    public float3 RandNext3F() { return float3(RandNext3()) / float(0xffffffffu); }

    [mutating]
    public float4 RandNext4F() { return float4(RandNext4()) / float(0xffffffffu); }
}
