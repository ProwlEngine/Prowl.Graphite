

namespace Prowl.Graphite;


public abstract class RenderTexture
{
    public static RenderTexture Create(int width, int height, RenderTextureFormat format, bool hasDepthTexture = true)
    {
        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLRenderTexture(width, height, format, hasDepthTexture)
        };
    }
}
