using System;
using System.Collections.Generic;
using System.Linq;

using Prowl.Vector;

using Superpower;
using Superpower.Parsers;
using Superpower.Model;
using Superpower.Tokenizers;


namespace Prowl.Graphite;


public static class ShaderParser
{
    public enum ShaderToken
    {
        HLSLInclude,
        HLSLProgram,

        // Simple token types
        Identifier,
        String,

        // Character token types
        OpenBrace,
        CloseBrace,
        OpenParen,
        CloseParen,
        Comma,
        Equals,
        NewLine,

        // Digit token types
        Decimal,
    }


    // Cuts out single-line comments like the one you're reading now
    public static TextParser<Unit> SingleLineComment =
        from _ in Character.EqualTo('/')
        from __ in Character.EqualTo('/')
        from content in Character.Except('\n').Many()
        select Unit.Value;

    /*
        Cuts out multiline comments like the one you're reading now
    */
    public static TextParser<Unit> MultiLineComment =
        from _ in Character.EqualTo('/')
        from __ in Character.EqualTo('*')
        from content in Character.Except('*')
            .Or(Character.EqualTo('*').Where(_ => false))
            .Many()
        from ___ in Character.EqualTo('*')
        from ____ in Character.EqualTo('/')
        select Unit.Value;


    // Tokenizes the top-level ShaderLab-inspired synax of a shader
    public static Tokenizer<ShaderToken> ShaderTokenizer { get; } =
        new TokenizerBuilder<ShaderToken>()
            .Ignore(Span.WhiteSpace)
            .Ignore(SingleLineComment)
            .Ignore(MultiLineComment)

            // Symbols
            .Match(Character.EqualTo('{'), ShaderToken.OpenBrace)
            .Match(Character.EqualTo('}'), ShaderToken.CloseBrace)
            .Match(Character.EqualTo('('), ShaderToken.OpenParen)
            .Match(Character.EqualTo(')'), ShaderToken.CloseParen)
            .Match(Character.EqualTo('='), ShaderToken.Equals)
            .Match(Character.EqualTo(','), ShaderToken.Comma)

            // String literal
            .Match(Span.Regex("\".*?\""), ShaderToken.String)

            // HLSL blocks
            .Match(
                Span.Regex(@"HLSLINCLUDE[\s\S]*?ENDHLSL"),
                ShaderToken.HLSLInclude
            )
            .Match(
                Span.Regex(@"HLSLPROGRAM[\s\S]*?ENDHLSL"),
                ShaderToken.HLSLProgram
            )

            .Match(Identifier.CStyle, ShaderToken.Identifier)

            // Numbers
            .Match(Numerics.Decimal, ShaderToken.Decimal)

            .Build();


    // ---------------------- Primitive Type Parsers ----------------------


    // Shorthand for a token-string comparison
    public static TokenListParser<ShaderToken, Token<ShaderToken>> Keyword(string expected, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        return Token.EqualTo(ShaderToken.Identifier)
            .Where(t =>
                t.ToStringValue().Equals(expected, comparison));
    }


    // Shorthand for a token-dictionary lookup
    public static TokenListParser<ShaderToken, T> Keywords<T>(Dictionary<string, T> values) =>
        Token.EqualTo(ShaderToken.Identifier).Select(token =>
            {
                var value = token.ToStringValue();

                if (values.TryGetValue(value, out T? result))
                    return result;

                throw Exceptions.ExpectedAny(values.Keys, value, token.Position);
            });


    // Shorthand for a token-enum parse
    public static TokenListParser<ShaderToken, T> Keywords<T>() where T : struct, Enum =>
        Token.EqualTo(ShaderToken.Identifier).Select(token =>
            {
                var value = token.ToStringValue();

                if (Enum.TryParse(value, out T result))
                    return result;

                throw Exceptions.ExpectedAny(Enum.GetNames<T>(), value, token.Position);
            });


    // Parses contents of quoted string ""
    static TokenListParser<ShaderToken, string> QuotedString =
        Token.EqualTo(ShaderToken.String)
            .Select(x => x.ToStringValue().Trim('"'));


    // Parses an integer
    static TokenListParser<ShaderToken, int> Integer =
        Token.EqualTo(ShaderToken.Decimal)
            .Select(x => int.Parse(x.ToStringValue()));


    // Parses a float
    static TokenListParser<ShaderToken, float> Float =
    Token.EqualTo(ShaderToken.Decimal)
         .Select(x => float.Parse(x.ToStringValue()));


    // Parses a 4-dimensional vector
    static TokenListParser<ShaderToken, Float4> Vector =
        from _open in Token.EqualTo(ShaderToken.OpenParen)
        from x in Float
        from _c1 in Token.EqualTo(ShaderToken.Comma)
        from y in Float
        from _c2 in Token.EqualTo(ShaderToken.Comma)
        from z in Float
        from _c3 in Token.EqualTo(ShaderToken.Comma)
        from w in Float
        from _close in Token.EqualTo(ShaderToken.CloseParen)
        select new Float4(x, y, z, w);


    // Parses a 4x4 matrix as 4 vectors delimited by an open-close parenthesis
    static TokenListParser<ShaderToken, Float4x4> Matrix =
        from _open in Token.EqualTo(ShaderToken.OpenParen)
        from x in Vector
        from y in Vector
        from z in Vector
        from w in Vector
        from _close in Token.EqualTo(ShaderToken.CloseParen)
        select new Float4x4(x, y, z, w);


    // Parses a texture definition as "" {}
    static TokenListParser<ShaderToken, string> Texture =
        from name in QuotedString
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select name;


    // Matches the next parsed identifier to a dictionary lookup for a TokenListParser
    static TokenListParser<ShaderToken, T> MatchCommand<T>(Dictionary<string, TokenListParser<ShaderToken, T>> commandMap) =>
        from id in Token.EqualTo(ShaderToken.Identifier)
            .Select(x =>
            {
                var value = x.ToStringValue();

                if (!commandMap.ContainsKey(value))
                    throw Exceptions.ExpectedAny(commandMap.Keys, value, x.Position);

                return value;
            })
        from cmd in commandMap[id]
        select cmd;


    // ---------------------- Shader Scope ----------------------


    static TokenListParser<ShaderToken, string> ShaderName =
        from _shader in Keyword("Shader")
        from name in QuotedString
        select name;


    static TokenListParser<ShaderToken, string> Fallback =
        from _fallback in Keyword("Fallback")
        from name in QuotedString
        select name;


    // Cuts out the HLSLINCLUDE-ENDHLSL block if found
    static TokenListParser<ShaderToken, HLSLBlock?> IncludeBlock =
        Token.EqualTo(ShaderToken.HLSLInclude)
            .Select(t => (HLSLBlock?)new HLSLBlock
            {
                Code = t.ToStringValue()["HLSLINCLUDE".Length..^"ENDHLSL".Length],
                StartLine = t.Position.Line,
            })
            .OptionalOrDefault();


    // The main parser for a shader markup language inspired by ShaderLab
    // Contains a property block, global include block, pass block, and a fallback.
    static TokenListParser<ShaderToken, ParsedShader> Shader =
        from name in ShaderName
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from props in PropertiesBlock!.OptionalOrDefault()
        from include in IncludeBlock
        from passes in PassBlock!.Many()
        from fallback in Fallback
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select new ParsedShader
        {
            Name = name,
            Properties = props ?? [],
            Passes = passes ?? [],
            GlobalInclude = include,
            Fallback = fallback,
        };


    // ---------------------- Property Block ----------------------


    static Dictionary<string, ShaderPropertyType> PropertyType =
        Enum.GetValues<ShaderPropertyType>().ToDictionary(x => Enum.GetName(x)!);


    static TokenListParser<ShaderToken, object> PropertyValue(ShaderPropertyType type)
    {
        return type switch
        {
            ShaderPropertyType.Float => Float.Select(x => (object)x),
            ShaderPropertyType.Integer => Integer.Select(x => (object)(float)x),
            ShaderPropertyType.Color or
            ShaderPropertyType.Vector => Vector.Select(x => (object)x),
            ShaderPropertyType.Matrix => Matrix.Select(x => (object)x),
            ShaderPropertyType.Texture2D or
            ShaderPropertyType.Texture2DArray or
            ShaderPropertyType.Texture3D or
            ShaderPropertyType.TextureCubemap or
            ShaderPropertyType.TextureCubemapArray => Texture.Select(x => (object)x),

            _ => throw new NotSupportedException($"Unsupported type {type}")
        };
    }


    // Parses a single property
    static TokenListParser<ShaderToken, ShaderProperty> Property =
        from name in Token.EqualTo(ShaderToken.Identifier)
            .Select(x => x.ToStringValue())

        from _open in Token.EqualTo(ShaderToken.OpenParen)
        from display in QuotedString
        from _separator in Token.EqualTo(ShaderToken.Comma)
        from type in Keywords(PropertyType)
        from _close in Token.EqualTo(ShaderToken.CloseParen)

        from value in
            (from _eq in Token.EqualTo(ShaderToken.Equals)
             from val in PropertyValue(type)
             select val)
            .OptionalOrDefault()

        select new ShaderProperty
        {
            Name = name,
            DisplayName = display,
            PropertyType = type,

            Value = type switch
            {
                ShaderPropertyType.Float or ShaderPropertyType.Integer => new((float)value, 0, 0, 0),
                ShaderPropertyType.Color or ShaderPropertyType.Vector => (Float4)value,
                _ => Float4.Zero
            },

            MatrixValue = type == ShaderPropertyType.Matrix ? (Float4x4)value : Float4x4.Zero,

            TextureValue = type switch
            {
                ShaderPropertyType.Texture2D or
                ShaderPropertyType.Texture2DArray or
                ShaderPropertyType.Texture3D or
                ShaderPropertyType.TextureCubemap or
                ShaderPropertyType.TextureCubemapArray => (string)value,
                _ => ""
            }
        };


    // Parses a property block
    static TokenListParser<ShaderToken, ShaderProperty[]?> PropertiesBlock =
        from _props in Keyword("Properties")
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from props in Property.Many()
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select props;


    // ---------------------- Pass Block ----------------------


    static TokenListParser<ShaderToken, string?> PassName =
        from _shader in Keyword("Name")
        from name in QuotedString
        select name;


    static TokenListParser<ShaderToken, KeyValuePair<string, string>> PassTag =
        from tagKey in QuotedString
        from _equals in Token.EqualTo(ShaderToken.Equals)
        from tagValue in QuotedString
        select new KeyValuePair<string, string>(tagKey, tagValue);


    static TokenListParser<ShaderToken, Dictionary<string, string>?> PassTags =
        from _tags in Keyword("Tags")
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from tags in PassTag.Many()
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select new Dictionary<string, string>(tags);


    static TokenListParser<ShaderToken, HLSLBlock> HlslBlock =
        Token.EqualTo(ShaderToken.HLSLProgram)
            .Select(t => new HLSLBlock
            {
                Code = t.ToStringValue()["HLSLPROGRAM".Length..^"ENDHLSL".Length],
                StartLine = t.Position.Line,
            });


    // Parses a pass block
    static TokenListParser<ShaderToken, ParsedPass> PassBlock =
        from _props in Keyword("Pass")
        from index in Integer.OptionalOrDefault(-1)
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from name in PassName.OptionalOrDefault()
        from tags in PassTags.OptionalOrDefault()
        from state in RenderState!
        from program in HlslBlock
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select new ParsedPass(state, program)
        {
            Name = name,
            Tags = tags,
        };

    // ---------------------- Render State ----------------------


    static Dictionary<string, bool> OnState = new()
    {
        { "On", true },
        { "Off", false }
    };


    static Dictionary<string, CullFace> CullFace = new()
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
            from stencilref in Integer
            select new ParsedPassState() { StencilBackRef = stencilref, StencilFrontRef = stencilref },

        ["ReadMask"] = from readmask in Integer
                       select new ParsedPassState() { StencilBackReadMask = (uint)readmask, StencilFrontReadMask = (uint)readmask },
        ["WriteMask"] = from writemask in Integer
                        select new ParsedPassState() { StencilBackWriteMask = (uint)writemask, StencilFrontWriteMask = (uint)writemask },

        ["Comp"] = from v in Keywords<StencilFunc>()
                   select new ParsedPassState() { StencilBackFunc = v, StencilFrontFunc = v, },
        ["CompBack"] = from v in Keywords<StencilFunc>()
                       select new ParsedPassState() { StencilBackFunc = v },
        ["CompFront"] = from v in Keywords<StencilFunc>()
                        select new ParsedPassState() { StencilFrontFunc = v },

        ["Pass"] = from v in Keywords<StencilOp>()
                   select new ParsedPassState() { StencilBackPassOp = v, StencilFrontPassOp = v },
        ["PassBack"] = from v in Keywords<StencilOp>()
                       select new ParsedPassState() { StencilBackPassOp = v },
        ["PassFront"] = from v in Keywords<StencilOp>()
                        select new ParsedPassState() { StencilFrontPassOp = v },

        ["Fail"] = from v in Keywords<StencilOp>()
                   select new ParsedPassState() { StencilBackFailOp = v, StencilFrontFailOp = v },
        ["FailBack"] = from v in Keywords<StencilOp>()
                       select new ParsedPassState() { StencilBackFailOp = v },
        ["FailFront"] = from v in Keywords<StencilOp>()
                        select new ParsedPassState() { StencilFrontFailOp = v },

        ["ZFail"] = from v in Keywords<StencilOp>()
                    select new ParsedPassState() { StencilBackDepthFailOp = v, StencilFrontDepthFailOp = v },
        ["ZFailBack"] = from v in Keywords<StencilOp>()
                        select new ParsedPassState() { StencilBackDepthFailOp = v },
        ["ZFailFront"] = from v in Keywords<StencilOp>()
                         select new ParsedPassState() { StencilFrontDepthFailOp = v },
    };


    static readonly Dictionary<string, TokenListParser<ShaderToken, ParsedPassState>> RenderStateCommands = new()
    {
        ["AlphaToMask"] =
            from mask in Keywords(OnState)
            select new ParsedPassState() { AlphaToMask = mask },

        ["BlendOp"] =
            from blendop in Keywords<BlendEquation>()
            select new ParsedPassState() { BlendEquationRgb = blendop, BlendEquationAlpha = blendop },

        ["Cull"] =
            from cull in Keywords(CullFace)
            select new ParsedPassState() { CullFace = cull },

        ["ZClip"] =
            from zclip in Keywords(OnState)
            select new ParsedPassState() { EnableDepthClamp = !zclip },

        ["ZTest"] =
            from ztest in Keywords<DepthFunc>()
            select new ParsedPassState() { DepthFunc = ztest },

        ["ZWrite"] =
            from zwrite in Keywords(OnState)
            select new ParsedPassState() { DepthWriteMask = zwrite },

        ["ColorMask"] =
            from mask in Token.EqualTo(ShaderToken.Identifier)
            select new ParsedPassState() { WriteMask = ParseMask(mask) },

        ["Offset"] =
            from factor in Float
            from units in Float
            select new ParsedPassState()
            {
                EnablePolygonOffsetFill = true,
                PolygonOffsetFactor = factor,
                PolygonOffsetUnits = units
            },

        ["Blend"] =
            from src in Keywords<BlendFactor>()
            from dst in Keywords<BlendFactor>()
            select new ParsedPassState()
            {
                BlendSrcRgb = src,
                BlendSrcAlpha = src,
                BlendDstRgb = dst,
                BlendDstAlpha = dst
            },

        ["BlendRGB"] =
            from srcRgb in Keywords<BlendFactor>()
            from dstRgb in Keywords<BlendFactor>()
            select new ParsedPassState() { BlendSrcRgb = srcRgb, BlendDstRgb = dstRgb },

        ["BlendAlpha"] =
            from srcA in Keywords<BlendFactor>()
            from dstA in Keywords<BlendFactor>()
            select new ParsedPassState() { BlendSrcAlpha = srcA, BlendDstAlpha = dstA },

        ["Stencil"] =
            from _open in Token.EqualTo(ShaderToken.OpenBrace)
            from stencilStates in MatchCommand(StencilStateCommands!).Many()
            from _close in Token.EqualTo(ShaderToken.CloseBrace)
            select ParsedPassState.FromSeveral(stencilStates)
    };


    static TokenListParser<ShaderToken, ParsedPassState> RenderState =
        from passStates in MatchCommand(RenderStateCommands!).Many()
        select ParsedPassState.FromSeveral(passStates);


    // ---------------------- Main Parser ----------------------


    public static ParsedShader ParseShader(string input)
    {
        TokenList<ShaderToken> tokens = ShaderTokenizer.Tokenize(input);
        ParsedShader shader = Shader.Parse(tokens);

        return shader;
    }
}
