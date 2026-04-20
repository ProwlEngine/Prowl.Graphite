using System.Collections.Generic;


namespace Prowl.Graphite;


public class ParsedPass
{
    public string Name = "";

    public Dictionary<string, string>? Tags = null;

    public ParsedPassState State;

    public HLSLBlock Program;


    public ParsedPass(ParsedPassState state, HLSLBlock program)
    {
        State = state;
        Program = program;
    }
}
