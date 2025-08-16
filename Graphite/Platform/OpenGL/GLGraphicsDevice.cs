using System;
using System.Collections.Generic;

using Prowl.Vector;

using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


public partial class GLGraphicsDevice : GraphicsDevice
{
    private static GLCommandProcessor s_glCommandProcessor;

    public GLGraphicsDevice(IGLContext source) : base()
    {
        s_glCommandProcessor = new(GL.GetApi(source), source);
    }


    public override void SubmitCommands(CommandBuffer buffer)
    {
        // _processingQueue.Add(new(((GLCommandBuffer)buffer)._glCommands));
    }


    public override void SwapBuffers()
    {
        s_glCommandProcessor.SwapBuffers();
    }

    protected override GraphicsBackend GetBackend() => GraphicsBackend.OpenGL;
}
