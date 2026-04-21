using Superpower;
using Superpower.Parsers;

namespace Prowl.Graphite.Compiler.Parser;


public class ParsedShaderSource
{
    public required string ShaderSourceFile;
    public required string VertexEntrypoint;
    public required string FragmentEntrypoint;


    static TokenListParser<ShaderToken, ParsedShaderSource> ShaderSourceParser =
        from _shaderSource in Token.EqualTo(ShaderToken.ShaderSource)
        from shaderSource in ParserUtility.QuotedString
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from _vertex in ParserUtility.Keyword("Vertex")
        from vertexEntrypoint in ParserUtility.QuotedString
        from _fragment in ParserUtility.Keyword("Fragment")
        from fragmentEntrypoint in ParserUtility.QuotedString
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select new ParsedShaderSource()
        {
            ShaderSourceFile = shaderSource,
            VertexEntrypoint = vertexEntrypoint,
            FragmentEntrypoint = fragmentEntrypoint
        };


    public static TokenListParser<ShaderToken, ParsedShaderSource> Parse() =>
        ShaderSourceParser;
}
