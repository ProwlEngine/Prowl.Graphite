

namespace Prowl.Graphite;


public abstract class Texture
{
    public static Texture Create()
    {
        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLTexture()
        };
    }
}
