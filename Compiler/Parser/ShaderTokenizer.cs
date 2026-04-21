using Superpower;
using Superpower.Parsers;
using Superpower.Model;
using Superpower.Tokenizers;


namespace Prowl.Graphite.Compiler.Parser;


public static class ShaderTokenizer
{
    // Cuts out single-line comments like the one you're reading now
    static TextParser<Unit> SingleLineComment =
        from _ in Character.EqualTo('/')
        from __ in Character.EqualTo('/')
        from content in Character.Except('\n').Many()
        select Unit.Value;

    /*
        Cuts out multiline comments like the one you're reading now
    */
    static TextParser<Unit> MultiLineComment =
        from _ in Character.EqualTo('/')
        from __ in Character.EqualTo('*')
        from content in Character.Except('*')
            .Or(Character.EqualTo('*').Where(_ => false))
            .Many()
        from ___ in Character.EqualTo('*')
        from ____ in Character.EqualTo('/')
        select Unit.Value;


    // Tokenizes the top-level ShaderLab-inspired synax of a shader
    static Tokenizer<ShaderToken> Tokenizer =
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

            .Match(Span.EqualTo("ShaderSource"), ShaderToken.ShaderSource)
            .Match(Identifier.CStyle, ShaderToken.Identifier)

            // Numbers
            .Match(Numerics.Decimal, ShaderToken.Decimal)

            .Build();


    public static TokenList<ShaderToken> Tokenize(string input)
        => Tokenizer.Tokenize(input);
}
