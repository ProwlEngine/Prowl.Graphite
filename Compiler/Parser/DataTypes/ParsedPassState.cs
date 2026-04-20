using System;

namespace Prowl.Graphite;


public class ParsedPassState
{
    public bool? EnableCulling;
    public CullFace? CullFace;
    public FrontFace? FrontFace;

    public bool? EnablePolygonOffsetFill;
    public float? PolygonOffsetFactor;
    public float? PolygonOffsetUnits;

    // -------------------- Depth --------------------

    public DepthFunc? DepthFunc;
    public bool? DepthWriteMask;
    public bool? EnableDepthClamp;

    // -------------------- Stencil --------------------

    public bool? EnableStencilTest;
    public StencilFunc? StencilFrontFunc;
    public int? StencilFrontRef;
    public uint? StencilFrontReadMask;
    public StencilOp? StencilFrontFailOp;
    public StencilOp? StencilFrontDepthFailOp;
    public StencilOp? StencilFrontPassOp;
    public uint? StencilFrontWriteMask;

    public StencilFunc? StencilBackFunc;
    public int? StencilBackRef;
    public uint? StencilBackReadMask;
    public StencilOp? StencilBackFailOp;
    public StencilOp? StencilBackDepthFailOp;
    public StencilOp? StencilBackPassOp;
    public uint? StencilBackWriteMask;

    // -------------------- Blending (equation / factors) --------------------
    public bool? EnableBlend;
    public BlendEquation? BlendEquationRgb;
    public BlendEquation? BlendEquationAlpha;
    public BlendFactor? BlendSrcRgb;
    public BlendFactor? BlendDstRgb;
    public BlendFactor? BlendSrcAlpha;
    public BlendFactor? BlendDstAlpha;

    // -------------------- Multisampling --------------------

    public bool? AlphaToMask;

    // -------------------- Color Write Mask --------------------
    public ColorWriteMask? WriteMask;


    public ParsedPassState Apply(ParsedPassState other)
    {
        return new()
        {
            EnableCulling = EnableCulling ?? other.EnableCulling,
            CullFace = CullFace ?? other.CullFace,
            FrontFace = FrontFace ?? other.FrontFace,
            EnablePolygonOffsetFill = EnablePolygonOffsetFill ?? other.EnablePolygonOffsetFill,
            PolygonOffsetFactor = PolygonOffsetFactor ?? other.PolygonOffsetFactor,
            PolygonOffsetUnits = PolygonOffsetUnits ?? other.PolygonOffsetUnits,
            DepthFunc = DepthFunc ?? other.DepthFunc,
            DepthWriteMask = DepthWriteMask ?? other.DepthWriteMask,
            EnableDepthClamp = EnableDepthClamp ?? other.EnableDepthClamp,
            EnableStencilTest = EnableStencilTest ?? other.EnableStencilTest,
            StencilFrontFunc = StencilFrontFunc ?? other.StencilFrontFunc,
            StencilFrontRef = StencilFrontRef ?? other.StencilFrontRef,
            StencilFrontReadMask = StencilFrontReadMask ?? other.StencilFrontReadMask,
            StencilFrontFailOp = StencilFrontFailOp ?? other.StencilFrontFailOp,
            StencilFrontDepthFailOp = StencilFrontDepthFailOp ?? other.StencilFrontDepthFailOp,
            StencilFrontPassOp = StencilFrontPassOp ?? other.StencilFrontPassOp,
            StencilFrontWriteMask = StencilFrontWriteMask ?? other.StencilFrontWriteMask,
            StencilBackFunc = StencilBackFunc ?? other.StencilBackFunc,
            StencilBackRef = StencilBackRef ?? other.StencilBackRef,
            StencilBackReadMask = StencilBackReadMask ?? other.StencilBackReadMask,
            StencilBackFailOp = StencilBackFailOp ?? other.StencilBackFailOp,
            StencilBackDepthFailOp = StencilBackDepthFailOp ?? other.StencilBackDepthFailOp,
            StencilBackPassOp = StencilBackPassOp ?? other.StencilBackPassOp,
            StencilBackWriteMask = StencilBackWriteMask ?? other.StencilBackWriteMask,
            EnableBlend = EnableBlend ?? other.EnableBlend,
            BlendEquationRgb = BlendEquationRgb ?? other.BlendEquationRgb,
            BlendEquationAlpha = BlendEquationAlpha ?? other.BlendEquationAlpha,
            BlendSrcRgb = BlendSrcRgb ?? other.BlendSrcRgb,
            BlendDstRgb = BlendDstRgb ?? other.BlendDstRgb,
            BlendSrcAlpha = BlendSrcAlpha ?? other.BlendSrcAlpha,
            BlendDstAlpha = BlendDstAlpha ?? other.BlendDstAlpha,
            AlphaToMask = AlphaToMask ?? other.AlphaToMask,
            WriteMask = WriteMask ?? other.WriteMask,
        };
    }


    public ParsedPassState ApplySeveral(ParsedPassState[] others)
        => Apply(FromSeveral(others));


    public static ParsedPassState FromSeveral(ParsedPassState[] others)
    {
        ParsedPassState value = others[0];

        for (int i = 1; i < others.Length; i++)
            value = value.Apply(others[i]);

        return value;
    }


    public ShaderPassState ToShaderPassState()
    {
        ShaderPassState @default = ShaderPassState.Default;

        return new()
        {
            EnableCulling = EnableCulling ?? @default.EnableCulling,
            CullFace = CullFace ?? @default.CullFace,
            FrontFace = FrontFace ?? @default.FrontFace,
            EnablePolygonOffsetFill = EnablePolygonOffsetFill ?? @default.EnablePolygonOffsetFill,
            PolygonOffsetFactor = PolygonOffsetFactor ?? @default.PolygonOffsetFactor,
            PolygonOffsetUnits = PolygonOffsetUnits ?? @default.PolygonOffsetUnits,
            DepthFunc = DepthFunc ?? @default.DepthFunc,
            DepthWriteMask = DepthWriteMask ?? @default.DepthWriteMask,
            EnableDepthClamp = EnableDepthClamp ?? @default.EnableDepthClamp,
            EnableStencilTest = EnableStencilTest ?? @default.EnableStencilTest,
            StencilFrontFunc = StencilFrontFunc ?? @default.StencilFrontFunc,
            StencilFrontRef = StencilFrontRef ?? @default.StencilFrontRef,
            StencilFrontReadMask = StencilFrontReadMask ?? @default.StencilFrontReadMask,
            StencilFrontFailOp = StencilFrontFailOp ?? @default.StencilFrontFailOp,
            StencilFrontDepthFailOp = StencilFrontDepthFailOp ?? @default.StencilFrontDepthFailOp,
            StencilFrontPassOp = StencilFrontPassOp ?? @default.StencilFrontPassOp,
            StencilFrontWriteMask = StencilFrontWriteMask ?? @default.StencilFrontWriteMask,
            StencilBackFunc = StencilBackFunc ?? @default.StencilBackFunc,
            StencilBackRef = StencilBackRef ?? @default.StencilBackRef,
            StencilBackReadMask = StencilBackReadMask ?? @default.StencilBackReadMask,
            StencilBackFailOp = StencilBackFailOp ?? @default.StencilBackFailOp,
            StencilBackDepthFailOp = StencilBackDepthFailOp ?? @default.StencilBackDepthFailOp,
            StencilBackPassOp = StencilBackPassOp ?? @default.StencilBackPassOp,
            StencilBackWriteMask = StencilBackWriteMask ?? @default.StencilBackWriteMask,
            EnableBlend = EnableBlend ?? @default.EnableBlend,
            BlendEquationRgb = BlendEquationRgb ?? @default.BlendEquationRgb,
            BlendEquationAlpha = BlendEquationAlpha ?? @default.BlendEquationAlpha,
            BlendSrcRgb = BlendSrcRgb ?? @default.BlendSrcRgb,
            BlendDstRgb = BlendDstRgb ?? @default.BlendDstRgb,
            BlendSrcAlpha = BlendSrcAlpha ?? @default.BlendSrcAlpha,
            BlendDstAlpha = BlendDstAlpha ?? @default.BlendDstAlpha,
            AlphaToMask = AlphaToMask ?? @default.AlphaToMask,
            WriteMask = WriteMask ?? @default.WriteMask,
        };
    }
}
