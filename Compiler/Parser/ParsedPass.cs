using System.Collections.Generic;


using Superpower;
using Superpower.Model;
using Superpower.Parsers;


namespace Prowl.Graphite;


public class ParsedPass
{
    public string Name = "";

    public Dictionary<string, string>? Tags = null;

    public required ParsedPassState State;

    public required ParsedShaderSource Source;


    static TokenListParser<ShaderToken, string?> PassName =
        from _shader in ParserUtility.Keyword("Name")
        from name in ParserUtility.QuotedString
        select name;


    static TokenListParser<ShaderToken, KeyValuePair<string, string>> PassTag =
        from tagKey in ParserUtility.QuotedString
        from _equals in Token.EqualTo(ShaderToken.Equals)
        from tagValue in ParserUtility.QuotedString
        select new KeyValuePair<string, string>(tagKey, tagValue);


    static TokenListParser<ShaderToken, Dictionary<string, string>?> PassTags =
        from _tags in ParserUtility.Keyword("Tags")
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from tags in PassTag.Many()
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select new Dictionary<string, string>(tags);


    static TokenListParser<ShaderToken, ParsedShaderSource> ShaderSource =
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


    // Parses a pass block
    static TokenListParser<ShaderToken, ParsedPass> PassBlock =
        from _props in ParserUtility.Keyword("Pass")
        from index in ParserUtility.Integer.OptionalOrDefault(-1)
        from _open in Token.EqualTo(ShaderToken.OpenBrace)
        from name in PassName.OptionalOrDefault()
        from tags in PassTags.OptionalOrDefault()
        from state in ParsedPassState.Parse()
        from source in ShaderSource
        from _close in Token.EqualTo(ShaderToken.CloseBrace)
        select new ParsedPass()
        {
            Name = name,
            Tags = tags,
            State = state,
            Source = source
        };


    public static TokenListParser<ShaderToken, ParsedPass> Parse() =>
        PassBlock;
}
