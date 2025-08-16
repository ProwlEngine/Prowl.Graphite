using System;
using System.Threading;

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
