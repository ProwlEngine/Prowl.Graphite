using System.Collections.Generic;


namespace Prowl.Graphite;


public abstract class Shader
{
    /// <summary>
    /// List of uniform names available to this shader.
    /// </summary>
    public List<string> Properties;
}
