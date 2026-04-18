using System;

using Silk.NET.OpenGL;

using GLStencilOp = Silk.NET.OpenGL.StencilOp;

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


    public static TriangleFace ToGLCullFace(this CullFace face) => face switch
    {
        CullFace.Front => TriangleFace.Front,
        CullFace.Back => TriangleFace.Back,
        CullFace.Front | CullFace.Back => TriangleFace.FrontAndBack,
        _ => TriangleFace.Back,
    };

    public static FrontFaceDirection ToGLFrontFace(this FrontFace face) => face switch
    {
        FrontFace.Clockwise => FrontFaceDirection.CW,
        FrontFace.CounterClockwise => FrontFaceDirection.Ccw,
        _ => FrontFaceDirection.Ccw,
    };

    public static DepthFunction ToGLDepthFunc(this DepthFunc func) => func switch
    {
        DepthFunc.Never => DepthFunction.Never,
        DepthFunc.Less => DepthFunction.Less,
        DepthFunc.Equal => DepthFunction.Equal,
        DepthFunc.LessEqual => DepthFunction.Lequal,
        DepthFunc.Greater => DepthFunction.Greater,
        DepthFunc.NotEqual => DepthFunction.Notequal,
        DepthFunc.GreaterEqual => DepthFunction.Gequal,
        DepthFunc.Always => DepthFunction.Always,
        DepthFunc.Disabled => throw new Exception("Flag 'Disabled' cannot be converted to GL depth function"),
        _ => DepthFunction.Less,
    };

    public static StencilFunction ToGLStencilFunc(this StencilFunc func) => func switch
    {
        StencilFunc.Never => StencilFunction.Never,
        StencilFunc.Less => StencilFunction.Less,
        StencilFunc.Equal => StencilFunction.Equal,
        StencilFunc.LessEqual => StencilFunction.Lequal,
        StencilFunc.Greater => StencilFunction.Greater,
        StencilFunc.NotEqual => StencilFunction.Notequal,
        StencilFunc.GreaterEqual => StencilFunction.Gequal,
        StencilFunc.Always => StencilFunction.Always,
        _ => StencilFunction.Always,
    };

    public static GLStencilOp ToGLStencilOp(this StencilOp op) => op switch
    {
        StencilOp.Keep => GLStencilOp.Keep,
        StencilOp.Zero => GLStencilOp.Zero,
        StencilOp.Replace => GLStencilOp.Replace,
        StencilOp.Increment => GLStencilOp.Incr,
        StencilOp.Decrement => GLStencilOp.Decr,
        StencilOp.Invert => GLStencilOp.Invert,
        StencilOp.IncrementWrap => GLStencilOp.IncrWrap,
        StencilOp.DecrementWrap => GLStencilOp.DecrWrap,
        _ => GLStencilOp.Keep,
    };

    public static BlendEquationModeEXT ToGLBlendEquation(this BlendEquation eq) => eq switch
    {
        BlendEquation.Add => BlendEquationModeEXT.FuncAdd,
        BlendEquation.Subtract => BlendEquationModeEXT.FuncSubtract,
        BlendEquation.ReverseSubtract => BlendEquationModeEXT.FuncReverseSubtract,
        BlendEquation.Min => BlendEquationModeEXT.Min,
        BlendEquation.Max => BlendEquationModeEXT.Max,
        _ => BlendEquationModeEXT.FuncAdd,
    };

    public static BlendingFactor ToGLBlendFactor(this BlendFactor factor) => factor switch
    {
        BlendFactor.Zero => BlendingFactor.Zero,
        BlendFactor.One => BlendingFactor.One,
        BlendFactor.SrcColor => BlendingFactor.SrcColor,
        BlendFactor.OneMinusSrcColor => BlendingFactor.OneMinusSrcColor,
        BlendFactor.SrcAlpha => BlendingFactor.SrcAlpha,
        BlendFactor.OneMinusSrcAlpha => BlendingFactor.OneMinusSrcAlpha,
        BlendFactor.DstAlpha => BlendingFactor.DstAlpha,
        BlendFactor.OneMinusDstAlpha => BlendingFactor.OneMinusDstAlpha,
        BlendFactor.DstColor => BlendingFactor.DstColor,
        BlendFactor.OneMinusDstColor => BlendingFactor.OneMinusDstColor,
        BlendFactor.SrcAlphaSaturate => BlendingFactor.SrcAlphaSaturate,
        BlendFactor.ConstantColor => BlendingFactor.ConstantColor,
        BlendFactor.OneMinusConstantColor => BlendingFactor.OneMinusConstantColor,
        BlendFactor.ConstantAlpha => BlendingFactor.ConstantAlpha,
        BlendFactor.OneMinusConstantAlpha => BlendingFactor.OneMinusConstantAlpha,
        BlendFactor.Src1Color => BlendingFactor.Src1Color,
        BlendFactor.OneMinusSrc1Color => BlendingFactor.OneMinusSrc1Color,
        BlendFactor.Src1Alpha => BlendingFactor.Src1Alpha,
        BlendFactor.OneMinusSrc1Alpha => BlendingFactor.OneMinusSrc1Alpha,
        _ => BlendingFactor.Zero,
    };
}
