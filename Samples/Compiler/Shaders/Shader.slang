// Per-view uniform block
struct Block0
{
    float4x4 matrix0_block0;
    float3 vector1_block0;
};

// Per-instance uniform block
struct Block1
{
    float4x4 matrix0_block1;
    float vector1_block1;
    float3 vector2_block1;
};


ParameterBlock<Block0> block0; // uniform buffer with data ordered like PerView
ParameterBlock<Block1> block1; // uniform buffer with dat ordered like PerModel


float4 globalData;
float3 moreGlobalData;


struct VertexData
{
    float3 position : POSITION; // Reads from CPU buffer mapped to "POSITION"
    float2 uv       : UV; // Reads from CPU buffer mapped to "UV"
};


struct VertexOutput
{
    float4 sv_position : SV_Position;
    float2 uv       : UV;
};


[attribute("custom")]
[shader("vertex")]
VertexOutput vertex(VertexData input)
{
    VertexOutput output;

    output.sv_position = UseAllTheBSData(mul(block0.matrix0_block0, float4(input.position, 1.0)));
    output.uv       = input.uv;

    return output;
}


float4 UseAllTheBSData(float4 input)
{
    return globalData * float4(moreGlobalData, 1.0) * mul(block0.matrix0_block0, float4(block0.vector1_block0, 1.0)) * mul(block1.matrix0_block1, block1.vector1_block1) * float4(block1.vector2_block1, 1.0);
}


[attribute("custom")]
[shader("fragment")]
float4 fragment(VertexOutput input) : SV_Target
{
    return UseAllTheBSData(float4(input.uv, 1, 1));
}
