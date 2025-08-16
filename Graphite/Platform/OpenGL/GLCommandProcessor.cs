using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using System.Drawing;

using Silk.NET.OpenGL;
using Silk.NET.Core.Contexts;

using Prowl.Vector;


namespace Prowl.Graphite.OpenGL;



internal enum WorkItemType
{
    ExecuteBuffer,
    GenericAction,
    TerminateAction,
    SwapBuffers,
    WaitForIdle,
}


internal unsafe struct GLWorkItem
{
    public readonly WorkItemType Type;
    public readonly object? Object0;
    public readonly object? Object1;
    public readonly uint UInt0;
    public readonly uint UInt1;


    public GLWorkItem(GLCommandBuffer commandBuffer, object? fence)
    {
        Type = WorkItemType.ExecuteBuffer;
        Object0 = commandBuffer;
        Object1 = fence;

        UInt0 = 0;
        UInt1 = 0;
    }

    public GLWorkItem(Action a, ManualResetEvent? mre, bool isTermination)
    {
        Type = isTermination ? WorkItemType.TerminateAction : WorkItemType.GenericAction;
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

    public GLWorkItem(WorkItemType type)
    {
        Type = type;
        Object0 = null;
        Object1 = null;

        UInt0 = 0;
        UInt1 = 0;
    }
}


internal class GLCommandProcessor
{
    private Queue<ManualResetEvent> _resetPool;

    private ManualResetEvent RentResetEvent()
    {
        lock (_resetPool)
        {
            if (_resetPool.TryDequeue(out ManualResetEvent? ev))
            {
                return ev;
            }
        }
        return new ManualResetEvent(false);
    }


    private void ReturnResetEvent(ManualResetEvent mre)
    {
        if (_resetPool.Count > Environment.ProcessorCount)
        {
            mre.Dispose();
            return;
        }

        lock (_resetPool)
        {
            _resetPool.Enqueue(mre);
            mre.Reset();
        }
    }


    private struct CommandContext
    {
        public GLRenderTexture? ActiveTarget;
    }


    private IGLContext _context;
    private GL _gl;
    private Thread _glExecutionThread;
    private BlockingCollection<GLWorkItem> _processingQueue = new(new ConcurrentQueue<GLWorkItem>());


    public GLCommandProcessor(GL gl, IGLContext context)
    {
        _context = context;
        _gl = gl;
        _glExecutionThread = new Thread(ProcessCommands);
        _glExecutionThread.Start();
    }


    private void ProcessCommands()
    {
        foreach (GLWorkItem workItem in _processingQueue.GetConsumingEnumerable())
        {
            ProcessItem(workItem);
        }
    }


    public void WaitForIdle()
    {
        ManualResetEvent mre = RentResetEvent();
        GLWorkItem workItem = new(mre, isFullFlush: false);

        _processingQueue.Add(workItem);
        mre.WaitOne();

        ReturnResetEvent(mre);
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

                    bool isFullFlush = workItem.UInt0 != 0;
                    if (isFullFlush)
                    {
                        _gl.Flush();
                        _gl.Finish();
                    }

                    break;
            }
        }
        finally
        {
            eventAfterExecute?.Set();
        }
    }


    private Color Vec4ToColor(Vector4 vector)
    {
        return Color.FromArgb(
            (int)(MathD.Clamp01(vector.x) * 255),
            (int)(MathD.Clamp01(vector.y) * 255),
            (int)(MathD.Clamp01(vector.z) * 255),
            (int)(MathD.Clamp01(vector.w) * 255)
        );
    }
}
