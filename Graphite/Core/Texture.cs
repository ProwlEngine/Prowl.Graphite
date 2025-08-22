

namespace Prowl.Graphite;


public abstract class Texture
{
    public static Texture Create(GraphicsDevice? device = null)
    {
        device ??= GraphicsDevice.Instance;

        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLTexture()
        };
    }
}
