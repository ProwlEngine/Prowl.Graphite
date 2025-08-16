using System;

using Prowl.Vector;


namespace Prowl.Graphite;


public abstract class GraphicsDevice
{
    public static GraphicsDevice Instance { get; private set; }

    internal GraphicsDevice()
    {
        if (Instance != null)
            throw new Exception("Multiple GraphicsDevice instances detected");

        Instance = this;
    }

    public abstract bool IsIdle { get; }

    public abstract void SubmitCommands(CommandBuffer buffer);

    public abstract void SwapBuffers();
}
