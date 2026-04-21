using System.Collections.Generic;


namespace Prowl.Graphite;


public class ParsedShader
{
    public string? Name;
    public string? Fallback;

    public ShaderProperty[]? Properties;
    public ParsedPass[]? Passes;
}
