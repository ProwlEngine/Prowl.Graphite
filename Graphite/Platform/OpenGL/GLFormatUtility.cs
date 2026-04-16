using System;

using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


public static class GLFormatUtility
{
    public static GLEnum ToGLEnum(this VertexInputFormat format) => format switch
    {
        VertexInputFormat.Float1 => GLEnum.Float,
        VertexInputFormat.Float2 => GLEnum.Float,
        VertexInputFormat.Float3 => GLEnum.Float,
        VertexInputFormat.Float4 => GLEnum.Float,
        VertexInputFormat.Byte2_Norm => GLEnum.UnsignedByte,
        VertexInputFormat.Byte2 => GLEnum.UnsignedByte,
        VertexInputFormat.Byte4_Norm => GLEnum.UnsignedByte,
        VertexInputFormat.Byte4 => GLEnum.UnsignedByte,
        VertexInputFormat.SByte2_Norm => GLEnum.Byte,
        VertexInputFormat.SByte2 => GLEnum.Byte,
        VertexInputFormat.SByte4_Norm => GLEnum.Byte,
        VertexInputFormat.SByte4 => GLEnum.Byte,
        VertexInputFormat.UShort2_Norm => GLEnum.UnsignedShort,
        VertexInputFormat.UShort2 => GLEnum.UnsignedShort,
        VertexInputFormat.UShort4_Norm => GLEnum.UnsignedShort,
        VertexInputFormat.UShort4 => GLEnum.UnsignedShort,
        VertexInputFormat.Short2_Norm => GLEnum.Short,
        VertexInputFormat.Short2 => GLEnum.Short,
        VertexInputFormat.Short4_Norm => GLEnum.Short,
        VertexInputFormat.Short4 => GLEnum.Short,
        VertexInputFormat.UInt1 => GLEnum.UnsignedInt,
        VertexInputFormat.UInt2 => GLEnum.UnsignedInt,
        VertexInputFormat.UInt3 => GLEnum.UnsignedInt,
        VertexInputFormat.UInt4 => GLEnum.UnsignedInt,
        VertexInputFormat.Int1 => GLEnum.Int,
        VertexInputFormat.Int2 => GLEnum.Int,
        VertexInputFormat.Int3 => GLEnum.Int,
        VertexInputFormat.Int4 => GLEnum.Int,
        VertexInputFormat.Half1 => GLEnum.HalfFloat,
        VertexInputFormat.Half2 => GLEnum.HalfFloat,
        VertexInputFormat.Half4 => GLEnum.HalfFloat,
        _ => throw new Exception("Unknown vertex input format")
    };
}
