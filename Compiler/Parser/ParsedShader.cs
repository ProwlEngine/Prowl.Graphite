using Superpower;
using Superpower.Model;
using Superpower.Parsers;


namespace Prowl.Graphite.Compiler.Parser;


public class ParsedShader
{
    public string? Name;
    public string? Fallback;

    public ShaderProperty[]? Properties;
    public ParsedPass[]? Passes;


    // Parses a property block
    static TokenListParser<ShaderToken, ShaderProperty[]?> PropertiesBlock =
        from _props in ParserUtility.Keyword("Properties")
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from props in ParsedProperty.Parse().Many()
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select props;


    // The main parser for a shader markup language inspired by ShaderLab
    // Contains a property block, pass block, and a fallback.
    static TokenListParser<ShaderToken, ParsedShader> ShaderParser =
        from _shader in ParserUtility.Keyword("Shader")
        from shader in ParserUtility.QuotedString
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from props in PropertiesBlock.OptionalOrDefault()
        from passes in ParsedPass.Parse().Many()
        from _fallback in ParserUtility.Keyword("Fallback")
        from fallback in ParserUtility.QuotedString
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select new ParsedShader
        {
            Name = shader,
            Properties = props ?? [],
            Passes = passes ?? [],
            Fallback = fallback,
        };


    public static ParsedShader Parse(string source)
    {
        TokenList<ShaderToken> tokens = ShaderTokenizer.Tokenize(source);
        return ShaderParser.Parse(tokens);
    }
}
