using System;

using Prowl.Vector;


namespace Prowl.Graphite;


public abstract class GraphicsDevice
{
    public static GraphicsBackend Backend => Instance.GetBackend();
    public static GraphicsDevice Instance { get; private set; }

    internal GraphicsDevice()
    {
        if (Instance != null)
            throw new Exception("Multiple GraphicsDevice instances detected");

        Instance = this;
    }

    public abstract void WaitForIdle();

    public abstract void SubmitCommands(CommandBuffer buffer);

    public abstract void SwapBuffers();


    protected abstract GraphicsBackend GetBackend();
}
