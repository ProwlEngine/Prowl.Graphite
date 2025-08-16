

namespace Prowl.Graphite;


public abstract class RenderBuffer
{
    public static RenderBuffer Create()
    {
        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLRenderBuffer()
        };
    }
}
