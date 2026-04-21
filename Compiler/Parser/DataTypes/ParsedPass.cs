using System.Collections.Generic;


namespace Prowl.Graphite;


public class ParsedPass
{
    public string Name = "";

    public Dictionary<string, string>? Tags = null;

    public required ParsedPassState State;

    public required ParsedShaderSource Source;
}
