using System;
using System.Collections.Generic;
using System.Linq;

using Prowl.Vector;

using Superpower;
using Superpower.Parsers;
using Superpower.Model;


namespace Prowl.Graphite;


public static class ParserUtility
{
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
    public static TokenListParser<ShaderToken, string> QuotedString =
        Token.EqualTo(ShaderToken.String)
            .Select(x => x.ToStringValue().Trim('"'));


    // Parses an integer
    public static TokenListParser<ShaderToken, int> Integer =
        Token.EqualTo(ShaderToken.Decimal)
            .Select(x => int.Parse(x.ToStringValue()));


    // Parses a float
    public static TokenListParser<ShaderToken, float> Float =
        Token.EqualTo(ShaderToken.Decimal)
            .Select(x => float.Parse(x.ToStringValue()));


    // Parses a 4-dimensional vector
    public static TokenListParser<ShaderToken, Float4> Vector =
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
    public static TokenListParser<ShaderToken, Float4x4> Matrix =
        from _open in Token.EqualTo(ShaderToken.OpenParen)
        from x in Vector
        from y in Vector
        from z in Vector
        from w in Vector
        from _close in Token.EqualTo(ShaderToken.CloseParen)
        select new Float4x4(x, y, z, w);


    // Parses a texture definition as "" {}
    public static TokenListParser<ShaderToken, string> Texture =
        from name in QuotedString
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select name;


    // Matches the next parsed identifier to a dictionary lookup for a TokenListParser
    public static TokenListParser<ShaderToken, T> MatchCommand<T>(Dictionary<string, TokenListParser<ShaderToken, T>> commandMap) =>
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
}
