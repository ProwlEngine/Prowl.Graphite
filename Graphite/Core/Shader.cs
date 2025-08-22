using System.Collections.Generic;


namespace Prowl.Graphite;


public abstract class Shader
{
    public static Shader Create(GraphicsDevice? device = null)
    {
        device ??= GraphicsDevice.Instance;

        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLShader()
        };
    }

    /// <summary>
    /// List of uniform names available to this shader.
    /// </summary>
    public List<string> Properties;
}
