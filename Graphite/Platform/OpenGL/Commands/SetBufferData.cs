using Silk.NET.OpenGL;

using System;


namespace Prowl.Graphite.OpenGL;


internal unsafe struct SetBufferData<T> : GLCommand where T : unmanaged
{
    public GLGraphicsBuffer? SourceBuffer;
    public Memory<T>? SourceData;

    public GLGraphicsBuffer Destination;

    public int SourceIndex;
    public int DestinationIndex;
    public int Count;



    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        if (SourceBuffer != null)
        {
            SourceBuffer.CopyBufferCore(gl, Destination, SourceIndex, DestinationIndex, Count);
        }
        else if (SourceData != null)
        {
            fixed (T* dataPtr = SourceData.Value.Span.Slice(SourceIndex))
                Destination.SetBufferDataCore(gl, dataPtr, DestinationIndex, Count);
        }
        else
        {
            throw new Exception("No source data to copy from");
        }
    }
}
