using System;
using System.Collections.Generic;

using Prowl.Vector;

using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


public partial class GLGraphicsDevice : GraphicsDevice
{
    private static GLDispatcher s_glCommandProcessor;

    public GLGraphicsDevice(Func<IGLContext> contextProvider) : base()
    {
        s_glCommandProcessor = new(this, contextProvider);
    }


    public override void SubmitCommands(CommandBuffer buffer)
    {
        s_glCommandProcessor.SubmitBuffer((GLCommandBuffer)buffer);
    }


    public override void WaitForIdle()
    {
        s_glCommandProcessor.WaitForIdle();
    }


    public override void SwapBuffers()
    {
        s_glCommandProcessor.SwapBuffers();
    }

    protected override GraphicsBackend GetBackend() => GraphicsBackend.OpenGL;


    internal void FlushDisposables()
    {

    }
}
