using System;


namespace Prowl.Graphite;


public abstract class VertexInput : IDisposable
{
    public static VertexInput Create(VertexInputDescriptor[] inputLayout, PrimitiveTopology topology, GraphicsDevice? device = null)
    {
        device ??= GraphicsDevice.Instance;

        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLVertexInput(inputLayout, topology, (OpenGL.GLGraphicsDevice)device)
        };
    }

    public abstract PrimitiveTopology Topology { get; set; }

    public abstract VertexInputDescriptor[] InputLayout { get; }


    public abstract bool Indices32Bit { get; }
    public abstract int IndexCount { get; }

    public abstract void SetVertexBuffer(GraphicsBuffer buffer, int stream);
    public abstract void SetIndexBuffer(GraphicsBuffer buffer, int indexCount, bool is32Bit);

    public abstract void Dispose();
}
