using System.Collections.Generic;

using Prowl.Vector;


namespace Prowl.Graphite;


public abstract class Mesh
{
    public static Mesh Create(GraphicsDevice? device = null)
    {
        device ??= GraphicsDevice.Instance;

        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLMesh()
        };
    }


    public abstract void SetVertices(List<Float3> vertices);
    public abstract void SetIndices(List<int> indices);
}
