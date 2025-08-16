using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;


namespace Prowl.Graphite.OpenGL;


internal static class ResetEventPool
{
    private static Queue<ManualResetEvent> _resetEventPool = [];

    public static ManualResetEvent Rent()
    {
        lock (_resetEventPool)
        {
            if (_resetEventPool.TryDequeue(out ManualResetEvent? ev))
            {
                return ev;
            }
        }
        return new ManualResetEvent(false);
    }

    public static void Return(ManualResetEvent mre)
    {
        if (_resetEventPool.Count > Environment.ProcessorCount)
        {
            mre.Dispose();
            return;
        }

        lock (_resetEventPool)
        {
            _resetEventPool.Enqueue(mre);
            mre.Reset();
        }
    }
}
