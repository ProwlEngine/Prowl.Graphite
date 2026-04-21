using System.Collections.Generic;

using Superpower;
using Superpower.Model;


namespace Prowl.Graphite.Compiler.Parser;


public static class Exceptions
{
    public static ParseException Expected(string expected, string found, Position position) =>
        new ParseException($"Expected '{expected}' but found '{found}'", position);


    public static ParseException ExpectedAny(IEnumerable<string> expected, string found, Position position) =>
        new ParseException($"Expected any of '{string.Join(", ", expected)}' but got '{found}'", position);

    public static ParseException AdjustException(ParseException ex, Position start)
    {
        Position adjusted = new(
            start.Absolute + ex.ErrorPosition.Absolute,
            start.Line + ex.ErrorPosition.Line - 1,
            ex.ErrorPosition.Column
        );

        return new ParseException(ex.Message, adjusted, ex.InnerException);
    }
}
