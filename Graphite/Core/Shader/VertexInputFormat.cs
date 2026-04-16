using System;


namespace Prowl.Graphite;


public enum VertexInputFormat : byte
{
    Float1,
    Float2,
    Float3,
    Float4,
    Byte2_Norm,
    Byte2,
    Byte4_Norm,
    Byte4,
    SByte2_Norm,
    SByte2,
    SByte4_Norm,
    SByte4,
    UShort2_Norm,
    UShort2,
    UShort4_Norm,
    UShort4,
    Short2_Norm,
    Short2,
    Short4_Norm,
    Short4,
    UInt1,
    UInt2,
    UInt3,
    UInt4,
    Int1,
    Int2,
    Int3,
    Int4,
    Half1,
    Half2,
    Half4,
}



internal static class FormatExtensions
{
    // Can't tell if the sizeof() is more readable or not compared to constants but I like all the red in my IDE and it looks more complicated.
    public static int Size(this VertexInputFormat format) => format switch
    {
        VertexInputFormat.Float1 => sizeof(float),
        VertexInputFormat.Float2 => sizeof(float) * 2,
        VertexInputFormat.Float3 => sizeof(float) * 3,
        VertexInputFormat.Float4 => sizeof(float) * 4,
        VertexInputFormat.Byte2_Norm => sizeof(byte) * 2,
        VertexInputFormat.Byte2 => sizeof(byte) * 2,
        VertexInputFormat.Byte4_Norm => sizeof(byte) * 4,
        VertexInputFormat.Byte4 => sizeof(byte) * 4,
        VertexInputFormat.SByte2_Norm => sizeof(sbyte) * 2,
        VertexInputFormat.SByte2 => sizeof(sbyte) * 2,
        VertexInputFormat.SByte4_Norm => sizeof(sbyte) * 4,
        VertexInputFormat.SByte4 => sizeof(sbyte) * 4,
        VertexInputFormat.UShort2_Norm => sizeof(ushort) * 2,
        VertexInputFormat.UShort2 => sizeof(ushort) * 2,
        VertexInputFormat.UShort4_Norm => sizeof(ushort) * 4,
        VertexInputFormat.UShort4 => sizeof(ushort) * 4,
        VertexInputFormat.Short2_Norm => sizeof(short) * 2,
        VertexInputFormat.Short2 => sizeof(short) * 2,
        VertexInputFormat.Short4_Norm => sizeof(short) * 4,
        VertexInputFormat.Short4 => sizeof(short) * 4,
        VertexInputFormat.UInt1 => sizeof(uint),
        VertexInputFormat.UInt2 => sizeof(uint) * 2,
        VertexInputFormat.UInt3 => sizeof(uint) * 3,
        VertexInputFormat.UInt4 => sizeof(uint) * 4,
        VertexInputFormat.Int1 => sizeof(int),
        VertexInputFormat.Int2 => sizeof(int) * 2,
        VertexInputFormat.Int3 => sizeof(int) * 3,
        VertexInputFormat.Int4 => sizeof(int) * 4,
        VertexInputFormat.Half1 => 2,
        VertexInputFormat.Half2 => 2 * 2,
        VertexInputFormat.Half4 => 2 * 4,
        _ => throw new Exception("Unknown vertex input format")
    };


    public static int Dimension(this VertexInputFormat format) => format switch
    {
        VertexInputFormat.Float1 => 1,
        VertexInputFormat.Float2 => 2,
        VertexInputFormat.Float3 => 3,
        VertexInputFormat.Float4 => 4,
        VertexInputFormat.Byte2_Norm => 2,
        VertexInputFormat.Byte2 => 2,
        VertexInputFormat.Byte4_Norm => 4,
        VertexInputFormat.Byte4 => 4,
        VertexInputFormat.SByte2_Norm => 2,
        VertexInputFormat.SByte2 => 2,
        VertexInputFormat.SByte4_Norm => 4,
        VertexInputFormat.SByte4 => 4,
        VertexInputFormat.UShort2_Norm => 2,
        VertexInputFormat.UShort2 => 2,
        VertexInputFormat.UShort4_Norm => 4,
        VertexInputFormat.UShort4 => 4,
        VertexInputFormat.Short2_Norm => 2,
        VertexInputFormat.Short2 => 2,
        VertexInputFormat.Short4_Norm => 4,
        VertexInputFormat.Short4 => 4,
        VertexInputFormat.UInt1 => 1,
        VertexInputFormat.UInt2 => 2,
        VertexInputFormat.UInt3 => 3,
        VertexInputFormat.UInt4 => 4,
        VertexInputFormat.Int1 => 1,
        VertexInputFormat.Int2 => 2,
        VertexInputFormat.Int3 => 3,
        VertexInputFormat.Int4 => 4,
        VertexInputFormat.Half1 => 1,
        VertexInputFormat.Half2 => 2,
        VertexInputFormat.Half4 => 4,
        _ => throw new Exception("Unknown vertex input format")
    };
}
