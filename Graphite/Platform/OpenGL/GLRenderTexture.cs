using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


public class GLRenderTexture : RenderTexture
{
    public GLRenderBuffer[] ColorBuffers { get; private set; }
    public GLRenderBuffer? DepthBuffer;

    internal Framebuffer _internalFramebuffer;


    public GLRenderTexture(int width, int height, RenderTextureFormat format, bool hasDepthTexture)
    {
    }
}
