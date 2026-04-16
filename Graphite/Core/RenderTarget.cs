namespace Prowl.Graphite;


/// <summary>
/// Represents a combined depth and render buffer, and can be used as a target for rendering operations.
/// A <see cref="RenderTarget"/> is equivalent to a framebuffer in other graphics APIs.
/// </summary>
public abstract class RenderTarget
{
    public static RenderTarget Create(GraphicsDevice? device = null)
    {
        device ??= GraphicsDevice.Instance;

        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLRenderTarget()
        };
    }
}
