using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using Silk.NET.OpenGL;
using Silk.NET.Core.Contexts;


namespace Prowl.Graphite.OpenGL;



internal enum WorkItemType
{
    ExecuteBuffer,
    GenericAction,
    SwapBuffers,
    WaitForIdle,
    InitializeResource,
}


internal unsafe struct GLWorkItem
{
    public readonly WorkItemType Type;
    public readonly object? Object0;
    public readonly object? Object1;
    public readonly uint UInt0;
    public readonly uint UInt1;


    public GLWorkItem(Queue<GLCommand> commandBuffer, object? fence)
    {
        Type = WorkItemType.ExecuteBuffer;
        Object0 = commandBuffer;
        Object1 = fence;

        UInt0 = 0;
        UInt1 = 0;
    }

    public GLWorkItem(Action<GL> a, ManualResetEvent? mre)
    {
        Type = WorkItemType.GenericAction;
        Object0 = a;
        Object1 = mre;

        UInt0 = 0;
        UInt1 = 0;
    }

    public GLWorkItem(ManualResetEvent mre, bool isFullFlush)
    {
        Type = WorkItemType.WaitForIdle;
        Object0 = mre;
        Object1 = null;

        UInt0 = isFullFlush ? 1u : 0u;
        UInt1 = 0;
    }

    public GLWorkItem(GLDeferredResource resource, ManualResetEvent? mre)
    {
        Type = WorkItemType.InitializeResource;
        Object0 = resource;
        Object1 = mre;
    }

    public GLWorkItem(WorkItemType type)
    {
        Type = type;
        Object0 = null;
        Object1 = null;

        UInt0 = 0;
        UInt1 = 0;
    }
}


internal class GLDispatcher
{
    private GLGraphicsDevice _device;

    private Func<IGLContext> _contextProvider;
    private IGLContext _context;
    private GL _gl;
    private Thread _glExecutionThread;
    private BlockingCollection<GLWorkItem> _processingQueue = new(new ConcurrentQueue<GLWorkItem>());

    internal IGLContext Context => _context;



    public GLDispatcher(Func<IGLContext> contextProvider, GLGraphicsDevice device)
    {
        _device = device;
        _contextProvider = contextProvider;

        _glExecutionThread = new Thread(ProcessCommands)
        {
            IsBackground = true,
            Name = "OpenGL Worker"
        };

        _glExecutionThread.Start();
    }


    private void ProcessCommands()
    {
        _context = _contextProvider.Invoke();
        _gl = GL.GetApi(_context);

        _context.MakeCurrent();

        _device.ARBDirectStateAccess = _gl.IsExtensionPresent("GL_ARB_direct_state_access");

        foreach (GLWorkItem workItem in _processingQueue.GetConsumingEnumerable())
        {
            ProcessItem(workItem);
        }
    }


    public void SubmitBuffer(GLCommandBuffer buffer, object? fence = null)
    {
        GLWorkItem workItem = new(new Queue<GLCommand>(buffer.Commands), fence);
        _processingQueue.Add(workItem);
    }


    public void WaitForIdle()
    {
        ManualResetEvent mre = ResetEventPool.Rent();
        GLWorkItem workItem = new(mre, false);

        _processingQueue.Add(workItem);
        mre.WaitOne();

        ResetEventPool.Return(mre);
    }


    public void SwapBuffers()
    {
        GLWorkItem workItem = new(WorkItemType.SwapBuffers);
        _processingQueue.Add(workItem);
    }


    public void CreateResource(GLDeferredResource resource, bool blockUntilCreated = false)
    {
        ManualResetEvent? mre = blockUntilCreated ? ResetEventPool.Rent() : null;
        GLWorkItem workItem = new(resource, mre);

        _processingQueue.Add(workItem);

        if (mre != null)
        {
            mre.WaitOne();
            ResetEventPool.Return(mre);
        }
    }


    public void EnqueueTask(Action<GL> task, bool blockUntilFinished = false)
    {
        ManualResetEvent? mre = blockUntilFinished ? ResetEventPool.Rent() : null;
        GLWorkItem workItem = new(task, mre);

        _processingQueue.Add(workItem);

        if (mre != null)
        {
            mre.WaitOne();
            ResetEventPool.Return(mre);
        }
    }


    private void ProcessItem(GLWorkItem workItem)
    {
        ManualResetEvent? eventAfterExecute = null;

        try
        {
            switch (workItem.Type)
            {
                case WorkItemType.WaitForIdle:
                    eventAfterExecute = Unsafe.As<ManualResetEvent>(workItem.Object0);
                    Debug.Assert(eventAfterExecute != null);

                    _device.FlushDisposables(_gl);
                    bool isFullFlush = workItem.UInt0 != 0;
                    if (isFullFlush)
                    {
                        _gl.Flush();
                        _gl.Finish();
                    }

                    break;

                case WorkItemType.SwapBuffers:
                    _context.SwapBuffers();
                    _device.FlushDisposables(_gl);

                    break;

                case WorkItemType.ExecuteBuffer:
                    Queue<GLCommand>? commandQueue = Unsafe.As<Queue<GLCommand>>(workItem.Object0);
                    Debug.Assert(commandQueue != null);

                    while (commandQueue.TryDequeue(out GLCommand? command))
                        command.Execute(this, _gl);

                    break;

                case WorkItemType.InitializeResource:
                    GLDeferredResource? resource = Unsafe.As<GLDeferredResource>(workItem.Object0);
                    Debug.Assert(resource != null);

                    if (workItem.Object1 != null)
                    {
                        eventAfterExecute = Unsafe.As<ManualResetEvent>(workItem.Object1);
                        Debug.Assert(eventAfterExecute != null);
                    }

                    resource.CreateResource(_gl);

                    break;

                case WorkItemType.GenericAction:
                    Action<GL>? action = Unsafe.As<Action<GL>>(workItem.Object0);
                    Debug.Assert(action != null);

                    if (workItem.Object1 != null)
                    {
                        eventAfterExecute = Unsafe.As<ManualResetEvent>(workItem.Object1);
                        Debug.Assert(eventAfterExecute != null);
                    }

                    action.Invoke(_gl);

                    break;
            }
        }
        finally
        {
            eventAfterExecute?.Set();
        }
    }
}
