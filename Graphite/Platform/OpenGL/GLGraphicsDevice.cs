using System;

using Prowl.Vector;

using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


public class GLGraphicsDevice : GraphicsDevice
{
    private static IGLContext s_context;
    private static GL s_gl;

    public GLGraphicsDevice(IGLContext source) : base()
    {
        s_context = source;
        s_gl = GL.GetApi(source);
    }


    public override void SubmitCommands(CommandBuffer buffer)
    {
        _processingQueue.Add(((GLCommandBuffer)buffer)._glCommands);
    }


    public override void SwapBuffers()
    {
        s_context.SwapBuffers();
    }
}
