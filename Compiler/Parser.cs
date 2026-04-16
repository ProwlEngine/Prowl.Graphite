using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Prowl.Vector;

using Superpower;
using Superpower.Parsers;
using Superpower.Tokenizers;
using Superpower.Display;


namespace Prowl.Graphite;


public static class ShaderParser
{
    public enum ShaderToken
    {
        // Shader block tokens
        Shader,
        Fallback,
        Properties,
        Pass,

        // Pass block tokens
        Name,
        Tags,

        HlslInclude,
        HlslProgram,

        // Shader property tokens
        PropInt,
        PropFloat,
        PropVector,
        PropColor,
        PropMatrix,
        PropTex2D,
        PropTex3D,
        PropTex2DArray,
        PropTexCube,
        PropTexCubemapArray,

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
        Integer,
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

            // Shader block tokens
            .Match(Span.EqualTo("Shader"), ShaderToken.Shader)
            .Match(Span.EqualTo("Properties"), ShaderToken.Properties)
            .Match(Span.EqualTo("Pass"), ShaderToken.Pass)
            .Match(Span.EqualTo("Fallback"), ShaderToken.Fallback)

            // Pass block tokens
            .Match(Span.EqualTo("Name"), ShaderToken.Name)
            .Match(Span.EqualTo("Tags"), ShaderToken.Tags)

            // HLSL blocks
            .Match(
                Span.Regex(@"HLSLINCLUDE[\s\S]*?ENDHLSL"),
                ShaderToken.HlslInclude
            )
            .Match(
                Span.Regex(@"HLSLPROGRAM[\s\S]*?ENDHLSL"),
                ShaderToken.HlslProgram
            )

            // Property tokens
            .Match(Span.EqualTo("Integer"), ShaderToken.PropInt)
            .Match(Span.EqualTo("Float"), ShaderToken.PropFloat)
            .Match(Span.EqualTo("Vector"), ShaderToken.PropVector)
            .Match(Span.EqualTo("Color"), ShaderToken.PropColor)
            .Match(Span.EqualTo("Matrix"), ShaderToken.PropMatrix)
            .Match(Span.EqualTo("2DArray"), ShaderToken.PropTex2DArray)
            .Match(Span.EqualTo("2D"), ShaderToken.PropTex2D)
            .Match(Span.EqualTo("3D"), ShaderToken.PropTex3D)
            .Match(Span.EqualTo("CubeArray"), ShaderToken.PropTexCubemapArray)
            .Match(Span.EqualTo("Cube"), ShaderToken.PropTexCube)

            .Match(Identifier.CStyle, ShaderToken.Identifier)

            // Numbers
            .Match(Numerics.Decimal, ShaderToken.Decimal)

            .Build();

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
        from _shader in Token.EqualTo(ShaderToken.Shader)
        from name in QuotedString
        select name;

    static TokenListParser<ShaderToken, string> Fallback =
        from _shader in Token.EqualTo(ShaderToken.Fallback)
        from name in QuotedString
        select name;

    static TokenListParser<ShaderToken, HlslBlock> IncludeBlock =
        Token.EqualTo(ShaderToken.HlslInclude)
            .Select(t => new HlslBlock
            {
                Code = t.ToStringValue().Replace("HLSLINCLUDE", "").Replace("ENDHLSL", "").Trim(),
                StartLine = t.Position.Line
            });

    static TokenListParser<ShaderToken, ParsedShader> Shader =
        from name in ShaderName
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from props in PropertiesBlock.OptionalOrDefault()
        from include in IncludeBlock.OptionalOrDefault()
        from passes in PassBlock.Many()
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

    static TokenListParser<ShaderToken, ShaderPropertyType> PropertyType =
        Token.EqualTo(ShaderToken.PropInt).Value(ShaderPropertyType.Integer)
        .Or(Token.EqualTo(ShaderToken.PropFloat).Value(ShaderPropertyType.Float))
        .Or(Token.EqualTo(ShaderToken.PropVector).Value(ShaderPropertyType.Vector))
        .Or(Token.EqualTo(ShaderToken.PropColor).Value(ShaderPropertyType.Color))
        .Or(Token.EqualTo(ShaderToken.PropMatrix).Value(ShaderPropertyType.Matrix))
        .Or(Token.EqualTo(ShaderToken.PropTex2D).Value(ShaderPropertyType.Texture2D))
        .Or(Token.EqualTo(ShaderToken.PropTex3D).Value(ShaderPropertyType.Texture3D))
        .Or(Token.EqualTo(ShaderToken.PropTex2DArray).Value(ShaderPropertyType.Texture2DArray))
        .Or(Token.EqualTo(ShaderToken.PropTexCube).Value(ShaderPropertyType.TextureCubemap))
        .Or(Token.EqualTo(ShaderToken.PropTexCubemapArray).Value(ShaderPropertyType.TextureCubemapArray));

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
        from type in PropertyType
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
        from _props in Token.EqualTo(ShaderToken.Properties)
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from props in Property.Many()
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select props;

    // ---------------------- Pass Block ----------------------

    static TokenListParser<ShaderToken, string?> PassName =
        from _shader in Token.EqualTo(ShaderToken.Name)
        from name in QuotedString
        select name;

    static TokenListParser<ShaderToken, KeyValuePair<string, string>> PassTag =
        from tagKey in QuotedString
        from _equals in Token.EqualTo(ShaderToken.Equals)
        from tagValue in QuotedString
        select new KeyValuePair<string, string>(tagKey, tagValue);

    static TokenListParser<ShaderToken, Dictionary<string, string>?> PassTags =
        from _tags in Token.EqualTo(ShaderToken.Tags)
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from tags in PassTag.Many()
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select new Dictionary<string, string>(tags);

    static TokenListParser<ShaderToken, HlslBlock> HlslBlock =
        Token.EqualTo(ShaderToken.HlslProgram)
            .Select(t => new HlslBlock
            {
                Code = t.ToStringValue().Replace("HLSLPROGRAM", "").Replace("ENDHLSL", "").Trim(),
                StartLine = t.Position.Line
            });

    // Parses a pass block
    static TokenListParser<ShaderToken, ParsedPass> PassBlock =
        from _props in Token.EqualTo(ShaderToken.Pass)
        from index in Integer.OptionalOrDefault(-1)
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from name in PassName.OptionalOrDefault()
        from tags in PassTags.OptionalOrDefault()
        from program in HlslBlock
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select new ParsedPass
        {
            Name = name,
            Program = program,
            Tags = tags
        };


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

    public HlslBlock GlobalInclude;

    public ShaderProperty[]? Properties;
    public ParsedPass[]? Passes;
}


public class ParsedPass
{
    public string Name = "";

    public Dictionary<string, string>? Tags;
    public ShaderPassState State;

    public HlslBlock Program;
}


public struct EntryPoint(ShaderStages stages, string name)
{
    public ShaderStages Stage = stages;
    public string Name = name;
}
