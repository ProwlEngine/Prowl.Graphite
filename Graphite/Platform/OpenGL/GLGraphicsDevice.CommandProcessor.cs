using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using System.Drawing;

using Silk.NET.OpenGL;
using Prowl.Vector;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Prowl.Graphite.OpenGL;



public partial class GLGraphicsDevice
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


    private Thread _glExecutionThread;
    private BlockingCollection<GLWorkItem> _processingQueue = new(new ConcurrentQueue<GLWorkItem>());


    private void InitializeGLThread()
    {
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


    public override void WaitForIdle()
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
                        s_gl.Flush();
                        s_gl.Finish();
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
