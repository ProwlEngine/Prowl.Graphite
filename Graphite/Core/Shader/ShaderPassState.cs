using System;

namespace Prowl.Graphite;


public class ShaderPassState
{
    public bool EnableCulling;
    public CullFace CullFace;
    public FrontFace FrontFace;

    public bool EnablePolygonOffsetFill;
    public float PolygonOffsetFactor;
    public float PolygonOffsetUnits;

    // -------------------- Depth --------------------

    public bool EnableDepthTest => DepthFunc != DepthFunc.Disabled;
    public DepthFunc DepthFunc;
    public bool DepthWriteMask;
    public bool EnableDepthClamp;

    // -------------------- Stencil --------------------

    public bool EnableStencilTest;
    public StencilFunc StencilFrontFunc;
    public int StencilFrontRef;
    public uint StencilFrontReadMask;
    public StencilOp StencilFrontFailOp;
    public StencilOp StencilFrontDepthFailOp;
    public StencilOp StencilFrontPassOp;
    public uint StencilFrontWriteMask;

    public StencilFunc StencilBackFunc;
    public int StencilBackRef;
    public uint StencilBackReadMask;
    public StencilOp StencilBackFailOp;
    public StencilOp StencilBackDepthFailOp;
    public StencilOp StencilBackPassOp;
    public uint StencilBackWriteMask;

    // -------------------- Blending (equation / factors) --------------------
    public bool EnableBlend;
    public BlendEquation BlendEquationRgb;
    public BlendEquation BlendEquationAlpha;
    public BlendFactor BlendSrcRgb;
    public BlendFactor BlendDstRgb;
    public BlendFactor BlendSrcAlpha;
    public BlendFactor BlendDstAlpha;

    // -------------------- Multisampling --------------------

    // Was EnableMultisample
    public bool AlphaToMask;

    // -------------------- Color Write Mask --------------------
    public ColorWriteMask WriteMask;



    public static ShaderPassState Default() =>
    new()
    {
        EnableCulling = true,
        CullFace = CullFace.Back,
        FrontFace = FrontFace.Clockwise,
        EnablePolygonOffsetFill = false,
        PolygonOffsetFactor = 1,
        PolygonOffsetUnits = 1,
        DepthFunc = DepthFunc.LessEqual,
        DepthWriteMask = true,
        EnableDepthClamp = false,
        EnableStencilTest = false,
        EnableBlend = false,
        AlphaToMask = false,
        WriteMask = ColorWriteMask.All,
    };
}
