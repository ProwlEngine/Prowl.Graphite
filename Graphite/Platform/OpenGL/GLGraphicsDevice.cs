using System;
using System.Collections.Generic;

using Prowl.Vector;

using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


public partial class GLGraphicsDevice : GraphicsDevice
{
    private static IGLContext s_context;
    private static GL s_gl;

    public GLGraphicsDevice(IGLContext source) : base()
    {
        s_context = source;
        s_gl = GL.GetApi(source);

        InitializeGLThread();
    }


    public override void SubmitCommands(CommandBuffer buffer)
    {
        // _processingQueue.Add(new(((GLCommandBuffer)buffer)._glCommands));
    }


    public override void SwapBuffers()
    {
        s_context.SwapBuffers();
    }

    protected override GraphicsBackend GetBackend() => GraphicsBackend.OpenGL;
}
