using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Prowl.Vector;

using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


public partial class GLGraphicsDevice : GraphicsDevice
{
    private GLDispatcher _dispatcher;
    private List<GLDeferredResource> _disposeQueue;

    internal GLDispatcher Dispatcher => _dispatcher;

    internal bool ARBBufferStorage;
    internal bool ARBDirectStateAccess;


    public GLGraphicsDevice(Func<IGLContext> contextProvider) : base()
    {
        _dispatcher = new(contextProvider, this);
        _disposeQueue = [];
    }


    public override void SubmitCommands(CommandBuffer buffer)
    {
        _dispatcher.SubmitBuffer((GLCommandBuffer)buffer);
    }


    public override void WaitForIdle()
    {
        _dispatcher.WaitForIdle();
    }


    public override void SwapBuffers()
    {
        _dispatcher.SwapBuffers();
    }

    protected override GraphicsBackend GetBackend() => GraphicsBackend.OpenGL;


    internal void EnqueueDisposable(GLDeferredResource disposable)
    {
        _disposeQueue.Add(disposable);
    }


    internal void FlushDisposables(GL gl)
    {
        for (int i = 0; i < _disposeQueue.Count; i++)
            _disposeQueue[i].DestroyResource(gl);

        _disposeQueue.Clear();
    }
}
