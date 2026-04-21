using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal struct SetRenderTarget : GLCommand
{
    public GLRenderTarget? Target;


    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        if (Target == null)
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        else
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Target._internalFramebuffer.Handle);
    }
}
