using System;

using Prowl.Vector;

using Superpower;
using Superpower.Parsers;


namespace Prowl.Graphite.Compiler.Parser;


public static class ParsedProperty
{
    static TokenListParser<ShaderToken, object> PropertyValue(ShaderPropertyType type)
    {
        return type switch
        {
            ShaderPropertyType.Float => ParserUtility.Float.Select(x => (object)x),
            ShaderPropertyType.Integer => ParserUtility.Integer.Select(x => (object)(float)x),
            ShaderPropertyType.Color or
            ShaderPropertyType.Vector => ParserUtility.Vector.Select(x => (object)x),
            ShaderPropertyType.Matrix => ParserUtility.Matrix.Select(x => (object)x),
            ShaderPropertyType.Texture2D or
            ShaderPropertyType.Texture2DArray or
            ShaderPropertyType.Texture3D or
            ShaderPropertyType.TextureCubemap or
            ShaderPropertyType.TextureCubemapArray => ParserUtility.Texture.Select(x => (object)x),

            _ => throw new NotSupportedException($"Unsupported type {type}")
        };
    }


    // Parses a single property
    static TokenListParser<ShaderToken, ShaderProperty> PropertyParser =
        from name in Token.EqualTo(ShaderToken.Identifier)
            .Select(x => x.ToStringValue())

        from _open in Token.EqualTo(ShaderToken.OpenParen)
        from display in ParserUtility.QuotedString
        from _separator in Token.EqualTo(ShaderToken.Comma)
        from type in ParserUtility.Keywords<ShaderPropertyType>()
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


    public static TokenListParser<ShaderToken, ShaderProperty> Parse() =>
        PropertyParser;
}
