using System.Collections.Generic;

using Superpower;
using Superpower.Model;
using Superpower.Parsers;


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


    static ParsedPassState FromSeveral(ParsedPassState[] others)
    {
        if (others.Length == 0)
            return new();

        ParsedPassState value = others[0];

        for (int i = 1; i < others.Length; i++)
            value = value.Apply(others[i]);

        return value;
    }


    static Dictionary<string, bool> OnStateMap = new()
    {
        { "On", true },
        { "Off", false }
    };


    static Dictionary<string, CullFace> CullFaceMap = new()
    {
        { "Back", Graphite.CullFace.Back },
        { "Front", Graphite.CullFace.Front },
        { "Off", 0 }
    };


    static ColorWriteMask ParseMask(Token<ShaderToken> maskToken)
    {
        string tokenString = maskToken.ToStringValue();

        ColorWriteMask result = 0;

        foreach (char c in tokenString)
        {
            result |= c switch
            {
                'R' => ColorWriteMask.R,
                'G' => ColorWriteMask.G,
                'B' => ColorWriteMask.B,
                'A' => ColorWriteMask.A,
                _ => throw new ParseException($"Invalid channel {c}. Expected any of [R, G, B, A]", maskToken.Position)
            };
        }

        return result;
    }


    static readonly Dictionary<string, TokenListParser<ShaderToken, ParsedPassState>> StencilStateCommands = new()
    {
        ["Ref"] =
            from stencilref in ParserUtility.Integer
            select new ParsedPassState() { StencilBackRef = stencilref, StencilFrontRef = stencilref },

        ["ReadMask"] = from readmask in ParserUtility.Integer
                       select new ParsedPassState() { StencilBackReadMask = (uint)readmask, StencilFrontReadMask = (uint)readmask },
        ["WriteMask"] = from writemask in ParserUtility.Integer
                        select new ParsedPassState() { StencilBackWriteMask = (uint)writemask, StencilFrontWriteMask = (uint)writemask },

        ["Comp"] = from v in ParserUtility.Keywords<StencilFunc>()
                   select new ParsedPassState() { StencilBackFunc = v, StencilFrontFunc = v, },
        ["CompBack"] = from v in ParserUtility.Keywords<StencilFunc>()
                       select new ParsedPassState() { StencilBackFunc = v },
        ["CompFront"] = from v in ParserUtility.Keywords<StencilFunc>()
                        select new ParsedPassState() { StencilFrontFunc = v },

        ["Pass"] = from v in ParserUtility.Keywords<StencilOp>()
                   select new ParsedPassState() { StencilBackPassOp = v, StencilFrontPassOp = v },
        ["PassBack"] = from v in ParserUtility.Keywords<StencilOp>()
                       select new ParsedPassState() { StencilBackPassOp = v },
        ["PassFront"] = from v in ParserUtility.Keywords<StencilOp>()
                        select new ParsedPassState() { StencilFrontPassOp = v },

        ["Fail"] = from v in ParserUtility.Keywords<StencilOp>()
                   select new ParsedPassState() { StencilBackFailOp = v, StencilFrontFailOp = v },
        ["FailBack"] = from v in ParserUtility.Keywords<StencilOp>()
                       select new ParsedPassState() { StencilBackFailOp = v },
        ["FailFront"] = from v in ParserUtility.Keywords<StencilOp>()
                        select new ParsedPassState() { StencilFrontFailOp = v },

        ["ZFail"] = from v in ParserUtility.Keywords<StencilOp>()
                    select new ParsedPassState() { StencilBackDepthFailOp = v, StencilFrontDepthFailOp = v },
        ["ZFailBack"] = from v in ParserUtility.Keywords<StencilOp>()
                        select new ParsedPassState() { StencilBackDepthFailOp = v },
        ["ZFailFront"] = from v in ParserUtility.Keywords<StencilOp>()
                         select new ParsedPassState() { StencilFrontDepthFailOp = v },
    };


    static readonly Dictionary<string, TokenListParser<ShaderToken, ParsedPassState>> RenderStateCommands = new()
    {
        ["AlphaToMask"] =
            from mask in ParserUtility.Keywords(OnStateMap)
            select new ParsedPassState() { AlphaToMask = mask },

        ["BlendOp"] =
            from blendop in ParserUtility.Keywords<BlendEquation>()
            select new ParsedPassState() { BlendEquationRgb = blendop, BlendEquationAlpha = blendop },

        ["Cull"] =
            from cull in ParserUtility.Keywords(CullFaceMap)
            select new ParsedPassState() { CullFace = cull },

        ["ZClip"] =
            from zclip in ParserUtility.Keywords(OnStateMap)
            select new ParsedPassState() { EnableDepthClamp = !zclip },

        ["ZTest"] =
            from ztest in ParserUtility.Keywords<DepthFunc>()
            select new ParsedPassState() { DepthFunc = ztest },

        ["ZWrite"] =
            from zwrite in ParserUtility.Keywords(OnStateMap)
            select new ParsedPassState() { DepthWriteMask = zwrite },

        ["ColorMask"] =
            from mask in Token.EqualTo(ShaderToken.Identifier)
            select new ParsedPassState() { WriteMask = ParseMask(mask) },

        ["Offset"] =
            from factor in ParserUtility.Float
            from units in ParserUtility.Float
            select new ParsedPassState()
            {
                EnablePolygonOffsetFill = true,
                PolygonOffsetFactor = factor,
                PolygonOffsetUnits = units
            },

        ["Blend"] =
            from src in ParserUtility.Keywords<BlendFactor>()
            from dst in ParserUtility.Keywords<BlendFactor>()
            select new ParsedPassState()
            {
                BlendSrcRgb = src,
                BlendSrcAlpha = src,
                BlendDstRgb = dst,
                BlendDstAlpha = dst
            },

        ["BlendRGB"] =
            from srcRgb in ParserUtility.Keywords<BlendFactor>()
            from dstRgb in ParserUtility.Keywords<BlendFactor>()
            select new ParsedPassState() { BlendSrcRgb = srcRgb, BlendDstRgb = dstRgb },

        ["BlendAlpha"] =
            from srcA in ParserUtility.Keywords<BlendFactor>()
            from dstA in ParserUtility.Keywords<BlendFactor>()
            select new ParsedPassState() { BlendSrcAlpha = srcA, BlendDstAlpha = dstA },

        ["Stencil"] =
            from _open in Token.EqualTo(ShaderToken.OpenBrace)
            from stencilStates in ParserUtility.MatchCommand(StencilStateCommands!).Many()
            from _close in Token.EqualTo(ShaderToken.CloseBrace)
            select FromSeveral(stencilStates)
    };


    static TokenListParser<ShaderToken, ParsedPassState> PassStateParser =
        from passStates in ParserUtility.MatchCommand(RenderStateCommands).Many()
        select FromSeveral(passStates);


    public static TokenListParser<ShaderToken, ParsedPassState> Parse() =>
        PassStateParser;
}
