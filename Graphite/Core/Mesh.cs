using System;


namespace Prowl.Graphite;


public abstract class Mesh : IDisposable
{
    public static Mesh Create(MeshCreateInfo? createInfo = null, GraphicsDevice? device = null)
    {
        createInfo ??= new MeshCreateInfo();
        device ??= GraphicsDevice.Instance;

        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLMesh(createInfo.Value, (OpenGL.GLGraphicsDevice)device)
        };
    }

    public abstract MeshTopology Topology { get; set; }

    public abstract VertexInputDescriptor[] InputLayout { get; }

    public abstract bool IsReadable { get; set; }

    public abstract void SetVertexInput<T>(Span<T> buffer, int stream) where T : unmanaged;
    public abstract T[] GetVertexInput<T>(int stream) where T : unmanaged;




    public abstract bool Has32BitIndices { get; }

    public abstract void SetIndexInput32(Span<uint> buffer);
    public abstract void SetIndexInput16(Span<ushort> buffer);

    public abstract uint[] GetIndexInput32();
    public abstract ushort[] GetIndexInput16();

    public abstract void Dispose();
}


public struct MeshCreateInfo
{
    public static MeshCreateInfo Default = new MeshCreateInfo()
    {
        VertexLayout = [
            new VertexInputDescriptor("POSITION", VertexInputFormat.Float3),
            new VertexInputDescriptor("NORMAL", VertexInputFormat.Float3),
            new VertexInputDescriptor("TANGENT", VertexInputFormat.Float3),
            new VertexInputDescriptor("UV0", VertexInputFormat.Float4),
            new VertexInputDescriptor("UV1", VertexInputFormat.Float4),
            new VertexInputDescriptor("UV2", VertexInputFormat.Float4)
        ],

        Topology = MeshTopology.Triangles
    };

    public VertexInputDescriptor[] VertexLayout;

    public MeshTopology Topology;
}
