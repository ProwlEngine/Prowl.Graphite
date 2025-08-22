using System;

using Prowl.Vector;


namespace Prowl.Graphite;


public abstract unsafe class GraphicsBuffer : IDisposable
{
    public static GraphicsBuffer Create(GraphicsBufferCreateInfo info, GraphicsDevice? device = null)
    {
        device ??= GraphicsDevice.Instance;

        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLGraphicsBuffer(info, (OpenGL.GLGraphicsDevice)device)
        };
    }

    public abstract bool IsValid { get; }

    public abstract MapState MapState { get; }

    public abstract int Count { get; }

    public abstract int Stride { get; }

    public int Size => Count * Stride;


    public abstract BufferTarget Target { get; }
    public abstract BufferUsage Usage { get; }


    public abstract void Dispose();

    public abstract void GetData(void* data, int destinationIndex, int sourceIndex, int countBytes);

    public abstract void SetData(void* data, int sourceIndex, int destinationIndex, int countBytes);

    public abstract void* MapBuffer();

    public abstract void UnmapBuffer();
}


public struct GraphicsBufferCreateInfo
{
    public BufferTarget Target;
    public BufferUsage Usage;

    public int Count;
    public int Stride;
}
