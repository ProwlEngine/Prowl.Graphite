using Silk.NET.OpenGL;

using Prowl.Vector;
using System;
using System.Threading;


namespace Prowl.Graphite.OpenGL;


internal struct SetRenderTarget : GLCommand
{
    public GLRenderTexture? Target;


    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        if (Target == null)
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        else
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, Target._internalFramebuffer.Handle);
    }
}
