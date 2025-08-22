

namespace Prowl.Graphite;


public abstract class RenderBuffer
{
    public static RenderBuffer Create(GraphicsDevice? device = null)
    {
        device ??= GraphicsDevice.Instance;

        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLRenderBuffer()
        };
    }
}
