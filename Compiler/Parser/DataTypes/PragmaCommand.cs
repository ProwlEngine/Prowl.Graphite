using System.Collections.Generic;


namespace Prowl.Graphite;



public enum PragmaType
{
    Vertex,
    Fragment,
    MultiCompile,

}


public struct PragmaCommand
{
    public PragmaType Type;
    public string Value;
    public string[] Values;
}
