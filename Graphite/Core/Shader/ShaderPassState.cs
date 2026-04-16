using System;

namespace Prowl.Graphite;


public struct ShaderPassState
{
    public bool EnableCulling;
    public CullFace CullFace;
    public FrontFace FrontFace;

    public bool EnablePolygonOffsetFill;
    public float PolygonOffsetFactor;
    public float PolygonOffsetUnits;

    // -------------------- Depth --------------------

    public bool EnableDepthTest;
    public DepthFunc DepthFunc;
    public bool DepthWriteMask;

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
    public bool ColorMaskR;
    public bool ColorMaskG;
    public bool ColorMaskB;
    public bool ColorMaskA;
}
