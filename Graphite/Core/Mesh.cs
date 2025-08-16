using System.Collections.Generic;

using Prowl.Vector;


namespace Prowl.Graphite;


public abstract class Mesh
{

    public abstract void SetVertices(List<Vector3> vertices);
    public abstract void SetIndices(List<int> indices);
}
