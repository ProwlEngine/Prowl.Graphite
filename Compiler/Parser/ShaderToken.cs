namespace Prowl.Graphite;


public enum ShaderToken
{
    ShaderSource,
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
