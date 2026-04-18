using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

using Prowl.Vector;

using Superpower;
using Superpower.Parsers;
using Superpower.Tokenizers;
using Superpower.Display;
using Superpower.Model;


namespace Prowl.Graphite;


public static class ShaderParser
{
    public enum ShaderToken
    {
        HlslInclude,
        HlslProgram,

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

        // Digit token types
        Decimal,
    }


    static Tokenizer<ShaderToken> Instance { get; } =
        new TokenizerBuilder<ShaderToken>()
            .Ignore(Span.WhiteSpace)

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
                ShaderToken.HlslInclude
            )
            .Match(
                Span.Regex(@"HLSLPROGRAM[\s\S]*?ENDHLSL"),
                ShaderToken.HlslProgram
            )

            .Match(Identifier.CStyle, ShaderToken.Identifier)

            // Numbers
            .Match(Numerics.Decimal, ShaderToken.Decimal)

            .Build();


    // ---------------------- Exception Generators ----------------------


    public static ParseException Expected(string expected, string found, Position position) =>
        new ParseException($"Expected '{expected}' but found '{found}'", position);


    public static ParseException ExpectedAny(IEnumerable<string> expected, string found, Position position) =>
        new ParseException($"Expected any of '{string.Join(", ", expected)}' but got '{found}'", position);


    // ---------------------- Token Validators ----------------------


    public static TokenListParser<ShaderToken, Token<ShaderToken>> Keyword(string expected, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        return Token.EqualTo(ShaderToken.Identifier)
            .Where(t =>
                t.ToStringValue().Equals(expected, comparison));
    }


    public static TokenListParser<ShaderToken, Token<ShaderToken>> RequiredKeyword(string expected, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        return Token.EqualTo(ShaderToken.Identifier)
            .Where(token =>
            {
                string value = token.ToStringValue();

                if (value.Equals(expected, comparison))
                    return true;

                throw Expected(expected, value, token.Position);
            });
    }


    public static TokenListParser<ShaderToken, T> Keywords<T>(Dictionary<string, T> values) =>
        Token.EqualTo(ShaderToken.Identifier).Select(token =>
            {
                var value = token.ToStringValue();

                if (values.TryGetValue(value, out T? result))
                    return result;

                throw ExpectedAny(values.Keys, value, token.Position);
            });


    // ---------------------- Primitive Type Parsers ----------------------


    static TokenListParser<ShaderToken, string> QuotedString =
        Token.EqualTo(ShaderToken.String)
            .Select(x => x.ToStringValue().Trim('"'));


    static TokenListParser<ShaderToken, int> Integer =
        Token.EqualTo(ShaderToken.Decimal)
            .Select(x => int.Parse(x.ToStringValue()));


    static TokenListParser<ShaderToken, float> Float =
    Token.EqualTo(ShaderToken.Decimal)
         .Select(x => float.Parse(x.ToStringValue()));


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


    static TokenListParser<ShaderToken, Float4x4> Matrix =
        from _open in Token.EqualTo(ShaderToken.OpenParen)
        from x in Vector
        from y in Vector
        from z in Vector
        from w in Vector
        from _close in Token.EqualTo(ShaderToken.CloseParen)
        select new Float4x4(x, y, z, w);


    static TokenListParser<ShaderToken, string> Texture =
        from name in QuotedString
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select name;


    // ---------------------- Shader Scope ----------------------


    static TokenListParser<ShaderToken, string> ShaderName =
        from _shader in RequiredKeyword("Shader")
        from name in QuotedString
        select name;


    static TokenListParser<ShaderToken, string> Fallback =
        from _fallback in Keyword("Fallback")
        from name in QuotedString
        select name;


    static TokenListParser<ShaderToken, HlslBlock?> IncludeBlock =
        Token.EqualTo(ShaderToken.HlslInclude)
            .Select(t => (HlslBlock?)new HlslBlock
            {
                Code = t.ToStringValue()["HLSLINCLUDE".Length..^"ENDHLSL".Length].Trim(),
                StartLine = t.Position.Line
            })
            .OptionalOrDefault();


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


    static TokenListParser<ShaderToken, HlslBlock> HlslBlock =
        Token.EqualTo(ShaderToken.HlslProgram)
            .Select(t => new HlslBlock
            {
                Code = t.ToStringValue()["HLSLPROGRAM".Length..^"ENDHLSL".Length].Trim(),
                StartLine = t.Position.Line
            });


    // Parses a pass block
    static TokenListParser<ShaderToken, ParsedPass> PassBlock =
        from _props in Keyword("Pass")
        from index in Integer.OptionalOrDefault(-1)
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from name in PassName.OptionalOrDefault()
        from tags in PassTags.OptionalOrDefault()
        from state in RenderStateCommands!.Many()
        from program in HlslBlock
        from _close in Token.EqualTo(ShaderToken.CloseBrace)

        select new ParsedPass(ApplyRenderState(ShaderPassState.Default(), state), program)
        {
            Name = name,
            Tags = tags,
        };


    // ---------------------- Render State Commands ----------------------


    static Dictionary<string, bool> OnState = new()
    {
        { "On", true },
        { "Off", false }
    };


    static Dictionary<string, CullFace> CullState = new()
    {
        { "Back", CullFace.Back },
        { "Front", CullFace.Front },
        { "Off", 0 }
    };


    static Dictionary<string, DepthFunc> ZTestState =
        Enum.GetValues<DepthFunc>().ToDictionary(x => Enum.GetName(x)!);


    static Dictionary<string, BlendEquation> BlendEquations =
        Enum.GetValues<BlendEquation>().ToDictionary(x => Enum.GetName(x)!);


    static Dictionary<string, StencilOp> StencilOperations =
        Enum.GetValues<StencilOp>().ToDictionary(x => Enum.GetName(x)!);


    static Dictionary<string, StencilFunc> StencilFunctions =
        Enum.GetValues<StencilFunc>().ToDictionary(x => Enum.GetName(x)!);


    static Dictionary<string, BlendFactor> BlendFactors =
        Enum.GetValues<BlendFactor>().ToDictionary(x => Enum.GetName(x)!);


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


    static TokenListParser<ShaderToken, ColorWriteMask> ColorMaskCommand =
        from mask in Token.EqualTo(ShaderToken.Identifier)
        select ParseMask(mask);


    static TokenListParser<ShaderToken, (bool, float, float)> OffsetCommand =
        from factor in Float
        from units in Float
        select (true, factor, units);


    static Action<ShaderPassState> MakeBlend(BlendFactor? srcRgb, BlendFactor? dstRgb, BlendFactor? srcA, BlendFactor? dstA)
    {
        return s =>
        {
            s.BlendSrcRgb = srcRgb ?? s.BlendSrcRgb;
            s.BlendDstRgb = dstRgb ?? s.BlendDstRgb;
            s.BlendSrcAlpha = srcA ?? s.BlendSrcAlpha;
            s.BlendDstAlpha = dstA ?? s.BlendDstAlpha;
        };
    }


    static TokenListParser<ShaderToken, Action<ShaderPassState>> BlendCommand =
        from src in Keywords(BlendFactors)
        from dst in Keywords(BlendFactors)
        select MakeBlend(src, dst, src, dst);


    static TokenListParser<ShaderToken, Action<ShaderPassState>> BlendRGBCommand =
        from srcRgb in Keywords(BlendFactors)
        from dstRgb in Keywords(BlendFactors)
        select MakeBlend(srcRgb, dstRgb, null, null);


    static TokenListParser<ShaderToken, Action<ShaderPassState>> BlendAlphaCommand =
        from srcA in Keywords(BlendFactors)
        from dstA in Keywords(BlendFactors)
        select MakeBlend(null, null, srcA, dstA);


    static TokenListParser<ShaderToken, Action<ShaderPassState>> StencilCommand =
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from stencilCommands in StencilCommands!.Many()
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select (Action<ShaderPassState>)(x => ApplyRenderState(x, stencilCommands));


    static readonly Dictionary<string, TokenListParser<ShaderToken, Action<ShaderPassState>>> CommandMap = new()
    {
        ["AlphaToMask"] = Keywords(OnState)
            .Select(v => (Action<ShaderPassState>)(s => s.AlphaToMask = v)),

        ["BlendOp"] = Keywords(BlendEquations)
            .Select(v => (Action<ShaderPassState>)(s =>
            {
                s.BlendEquationRgb = v;
                s.BlendEquationAlpha = v;
            })),

        ["Cull"] = Keywords(CullState)
            .Select(v => (Action<ShaderPassState>)(s => s.CullFace = v)),

        ["ZClip"] = Keywords(OnState)
            .Select(v => (Action<ShaderPassState>)(s => s.EnableDepthClamp = !v)),

        ["ZTest"] = Keywords(ZTestState)
            .Select(v => (Action<ShaderPassState>)(s => s.DepthFunc = v)),

        ["ZWrite"] = Keywords(OnState)
            .Select(v => (Action<ShaderPassState>)(s => s.DepthWriteMask = v)),

        ["ColorMask"] = ColorMaskCommand
            .Select(v => (Action<ShaderPassState>)(s => s.WriteMask = v)),

        ["Offset"] = OffsetCommand
            .Select(v => (Action<ShaderPassState>)(s =>
            {
                s.EnablePolygonOffsetFill = v.Item1;
                s.PolygonOffsetFactor = v.Item2;
                s.PolygonOffsetUnits = v.Item3;
            })),

        ["Blend"] = BlendCommand,
        ["BlendRGB"] = BlendRGBCommand,
        ["BlendAlpha"] = BlendAlphaCommand,

        ["Stencil"] = StencilCommand
    };


    static TokenListParser<ShaderToken, Action<ShaderPassState>> RenderStateCommands =
        from id in Token.EqualTo(ShaderToken.Identifier)
            .Select(x =>
            {
                string value = x.ToStringValue();

                if (!CommandMap.ContainsKey(value))
                    throw ExpectedAny(CommandMap.Keys, value, x.Position);

                return value;
            })
        from cmd in CommandMap[id]
        select cmd;


    static ShaderPassState ApplyRenderState(ShaderPassState state, IEnumerable<Action<ShaderPassState>> stateCommands)
    {
        foreach (Action<ShaderPassState> stateCommand in stateCommands)
            stateCommand.Invoke(state);

        return state;
    }


    static readonly Dictionary<string, TokenListParser<ShaderToken, Action<ShaderPassState>>> StencilCommandMap = new()
    {
        ["Ref"] = Integer
            .Select(v => (Action<ShaderPassState>)(s => { s.StencilBackRef = v; s.StencilFrontRef = v; })),

        ["ReadMask"] = Integer
            .Select(v => (Action<ShaderPassState>)(s => { s.StencilBackReadMask = (uint)v; s.StencilFrontReadMask = (uint)v; })),
        ["WriteMask"] = Integer
            .Select(v => (Action<ShaderPassState>)(s => { s.StencilBackWriteMask = (uint)v; s.StencilFrontWriteMask = (uint)v; })),

        ["Comp"] = Keywords(StencilFunctions)
            .Select(v => (Action<ShaderPassState>)(s => { s.StencilBackFunc = v; s.StencilFrontFunc = v; })),
        ["CompBack"] = Keywords(StencilFunctions)
            .Select(v => (Action<ShaderPassState>)(s => s.StencilBackFunc = v)),
        ["CompFront"] = Keywords(StencilFunctions)
            .Select(v => (Action<ShaderPassState>)(s => s.StencilFrontFunc = v)),

        ["Pass"] = Keywords(StencilOperations)
            .Select(v => (Action<ShaderPassState>)(s => { s.StencilBackPassOp = v; s.StencilFrontPassOp = v; })),
        ["PassBack"] = Keywords(StencilOperations)
            .Select(v => (Action<ShaderPassState>)(s => s.StencilBackPassOp = v)),
        ["PassFront"] = Keywords(StencilOperations)
            .Select(v => (Action<ShaderPassState>)(s => s.StencilFrontPassOp = v)),

        ["Fail"] = Keywords(StencilOperations)
            .Select(v => (Action<ShaderPassState>)(s => { s.StencilBackFailOp = v; s.StencilFrontFailOp = v; })),
        ["FailFront"] = Keywords(StencilOperations)
            .Select(v => (Action<ShaderPassState>)(s => s.StencilFrontFailOp = v)),
        ["FailBack"] = Keywords(StencilOperations)
            .Select(v => (Action<ShaderPassState>)(s => s.StencilBackFailOp = v)),

        ["ZFail"] = Keywords(StencilOperations)
            .Select(v => (Action<ShaderPassState>)(s => { s.StencilBackDepthFailOp = v; s.StencilFrontDepthFailOp = v; })),
        ["ZFailBack"] = Keywords(StencilOperations)
            .Select(v => (Action<ShaderPassState>)(s => s.StencilBackDepthFailOp = v)),
        ["ZFailFront"] = Keywords(StencilOperations)
            .Select(v => (Action<ShaderPassState>)(s => s.StencilFrontDepthFailOp = v)),
    };


    static TokenListParser<ShaderToken, Action<ShaderPassState>> StencilCommands =
        from id in Token.EqualTo(ShaderToken.Identifier)
            .Select(x =>
            {
                string value = x.ToStringValue();

                if (!StencilCommandMap.ContainsKey(value))
                    throw ExpectedAny(CommandMap.Keys, value, x.Position);

                return value;
            })
        from cmd in StencilCommandMap[id]
        select cmd;


    // ---------------------- Main Parser ----------------------


    public static ParsedShader Parse(string input)
    {
        var tokenizer = Instance;
        var tokens = tokenizer.Tokenize(input);
        return Shader.Parse(tokens);
    }
}


public class HlslBlock
{
    public string? Code;
    public int StartLine;
}


public class ParsedShader
{
    public string? Name;
    public string? Fallback;

    public HlslBlock? GlobalInclude;

    public ShaderProperty[]? Properties;
    public ParsedPass[]? Passes;
}


public class ParsedPass
{
    public string Name = "";

    public Dictionary<string, string>? Tags = null;

    public ShaderPassState State;

    public HlslBlock Program;


    public ParsedPass(ShaderPassState state, HlslBlock program)
    {
        State = state;
        Program = program;
    }
}


public struct EntryPoint(ShaderStages stages, string name)
{
    public ShaderStages Stage = stages;
    public string Name = name;
}
