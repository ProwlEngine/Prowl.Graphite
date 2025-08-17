using Silk.NET.OpenGL;

using Prowl.Vector;
using System;
using System.Threading;


namespace Prowl.Graphite.OpenGL;


internal struct SetRenderTarget : GLCommand
{
    public GLRenderTexture? Target;


    public void Execute(GLDispatcher dispatcher)
    {
        if (Target == null)
            dispatcher.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        else
            dispatcher.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, Target._internalFramebuffer.Handle);
    }
}
