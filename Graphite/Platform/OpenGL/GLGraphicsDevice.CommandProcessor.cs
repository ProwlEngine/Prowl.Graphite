using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Prowl.Graphite.OpenGL;



internal partial class GLGraphicsDevice
{
    private Thread _glExecutionThread;
    private BlockingCollection<Queue<GLCommand>> _processingQueue = new(new ConcurrentQueue<Queue<GLCommand>>());


    private void InitializeGLThread()
    {
        _glExecutionThread = new Thread(ProcessCommands);
    }


    private void ProcessCommands()
    {
        foreach (Queue<GLCommand> commandBuffer in _processingQueue.GetConsumingEnumerable())
        {
            while (commandBuffer.TryDequeue(out GLCommand command))
            {
                ProcessCommand(command);
            }
        }
    }


    public override bool IsIdle => _processingQueue.Count == 0;


    private void ProcessCommand(GLCommand command)
    {
        s_gl.
    }
}
