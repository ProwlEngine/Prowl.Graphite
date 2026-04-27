using System;
using System.Runtime.CompilerServices;


namespace Prowl.Graphite;


public sealed class Mesh : IDisposable
{
    private readonly VertexInput _input;

    // Per-stream state
    private readonly (Array? Array, bool IsDirty)[] _streamArrays;
    private readonly Action<GraphicsBuffer, CommandBuffer>[] _streamUploaders;
    private readonly int[] _streamElementSizes;
    private readonly GraphicsBuffer?[] _vertexBuffers;

    // Index state
    private (Array? Array, bool IsDirty) _indexArray;
    private Action<GraphicsBuffer, CommandBuffer>? _indexUploader;
    private GraphicsBuffer? _indexBuffer;

    private bool _disposed;
    private bool _has32BitIndices;


    public VertexInput Input => _input;

    public int VertexCount { get; private set; }
    public int IndexCount { get; private set; }

    public bool Has32BitIndices => _has32BitIndices;
    public bool IsReadable { get; set; }

    public VertexInputDescriptor[] InputLayout => Input.InputLayout;

    // Pass to CommandBuffer.Draw() after calling Upload().


    public PrimitiveTopology Topology
    {
        get => _input.Topology;
        set => _input.Topology = value;
    }


    public static Mesh Create(MeshCreateInfo? createInfo = null, GraphicsDevice? device = null)
    {
        MeshCreateInfo info = createInfo ?? MeshCreateInfo.Default;
        VertexInput input = VertexInput.Create(info.VertexLayout, info.Topology, device);
        return new Mesh(info.VertexLayout, input);
    }


    private Mesh(VertexInputDescriptor[] layout, VertexInput input)
    {
        _input = input;
        _streamArrays = new (Array?, bool)[layout.Length];
        _streamUploaders = new Action<GraphicsBuffer, CommandBuffer>[layout.Length];
        _streamElementSizes = new int[layout.Length];
        _vertexBuffers = new GraphicsBuffer?[layout.Length];
    }


    public void SetVertexInput<T>(Span<T> buffer, int stream) where T : unmanaged
    {
        if (stream == 0)
            VertexCount = buffer.Length;
        else if (buffer.Length < VertexCount)
            throw new InvalidOperationException("Vertex stream buffer must be at least as large as the vertex count.");

        T[] copy = buffer.ToArray();

        _streamArrays[stream] = (copy, true);
        _streamElementSizes[stream] = Unsafe.SizeOf<T>();
        _streamUploaders[stream] = (buf, cmd) => cmd.SetBufferData<T>(buf, copy, 0, 0, copy.Length);
    }


    public T[] GetVertexInput<T>(int stream) where T : unmanaged
    {
        if (!IsReadable)
            throw new InvalidOperationException("Cannot read vertex data from a write-only Mesh.");

        if (_streamArrays[stream].Item1 == null)
        {
            if (_vertexBuffers[stream] == null)
                return [];

            T[] readback = new T[VertexCount];
            _vertexBuffers[stream]!.GetData<T>(readback, 0);
            _streamArrays[stream] = (readback, false);
        }

        if (_streamArrays[stream] is T[] arr)
            return arr;

        Type? storedType = _streamArrays[stream]!.GetType().GetElementType();
        throw new InvalidCastException($"Stream {stream} stores {storedType} but {typeof(T)} was requested.");
    }


    public void SetIndexInput32(Span<uint> buffer)
    {
        _has32BitIndices = true;
        IndexCount = buffer.Length;

        uint[] copy = buffer.ToArray();
        _indexArray = (copy, true);
        _indexUploader = (buf, cmd) => cmd.SetBufferData<uint>(buf, copy, 0, 0, copy.Length);
    }


    public void SetIndexInput16(Span<ushort> buffer)
    {
        _has32BitIndices = false;
        IndexCount = buffer.Length;

        ushort[] copy = buffer.ToArray();
        _indexArray = (copy, true);
        _indexUploader = (buf, cmd) => cmd.SetBufferData<ushort>(buf, copy, 0, 0, copy.Length);
    }


    public uint[] GetIndexInput32()
    {
        if (!IsReadable) throw new InvalidOperationException("Cannot read index data from a write-only Mesh.");
        if (!Has32BitIndices) throw new InvalidOperationException("Mesh stores 16-bit indices; cannot read as 32-bit.");

        if (_indexArray.Array == null)
        {
            if (_indexBuffer == null)
                return [];

            uint[] readback = new uint[IndexCount];
            _indexBuffer.GetData<uint>(readback, 0);
            _indexArray = (readback, false);
        }

        return (uint[])_indexArray.Array;
    }


    public ushort[] GetIndexInput16()
    {
        if (!IsReadable) throw new InvalidOperationException("Cannot read index data from a write-only Mesh.");
        if (Has32BitIndices) throw new InvalidOperationException("Mesh stores 32-bit indices; cannot read as 16-bit.");

        if (_indexArray.Array == null)
        {
            if (_indexBuffer == null)
                return [];

            ushort[] readback = new ushort[IndexCount];
            _indexBuffer.GetData<ushort>(readback, 0);
            _indexArray = (readback, false);
        }

        return (ushort[])_indexArray.Array;
    }


    // Uploads all dirty CPU data to the GPU through the provided CommandBuffer.
    // Call this before submitting a draw call that uses this Mesh.
    internal void Upload(CommandBuffer cmd)
    {
        for (int i = 0; i < _streamArrays.Length; i++)
        {
            if (_streamArrays[i].Array == null || !_streamArrays[i].IsDirty)
                continue;

            EnsureVertexBuffer(i, _streamArrays[i].Array!.Length, _streamElementSizes[i]);
            _streamUploaders[i].Invoke(_vertexBuffers[i]!, cmd);
            _streamArrays[i] = (_streamArrays[i].Array, false);
        }

        if (_indexArray.Array != null && _indexArray.IsDirty)
        {
            int stride = _has32BitIndices ? sizeof(uint) : sizeof(ushort);
            EnsureIndexBuffer(_indexArray.Array.Length, stride);
            _indexUploader!.Invoke(_indexBuffer!, cmd);
            _indexArray = (_indexArray.Array, false);
        }

        if (!IsReadable)
        {
            _indexArray.Array = null;
            for (int i = 0; i < _streamArrays.Length; i++)
                _streamArrays[i].Array = null;
        }
    }


    private void EnsureVertexBuffer(int stream, int elementCount, int elementStride)
    {
        int requestedSize = elementCount * elementStride;
        int capacity = _vertexBuffers[stream]?.Size ?? 0;

        if (_vertexBuffers[stream] != null && requestedSize <= capacity && requestedSize >= capacity / 2)
            return;

        _vertexBuffers[stream]?.Dispose();
        _vertexBuffers[stream] = GraphicsBuffer.Create(new GraphicsBufferCreateInfo
        {
            Target = BufferTarget.Vertex,
            Count = (int)(elementCount * 1.5f),
            Stride = elementStride,
        });

        _input.SetVertexBuffer(_vertexBuffers[stream]!, stream);
    }


    private void EnsureIndexBuffer(int elementCount, int elementStride)
    {
        int requestedSize = elementCount * elementStride;
        int capacity = _indexBuffer?.Size ?? 0;

        bool needsResize = _indexBuffer == null || requestedSize > capacity || requestedSize < capacity / 2;
        if (!needsResize)
        {
            // Count may have changed even if buffer is the same; always update VertexInput.
            _input.SetIndexBuffer(_indexBuffer!, IndexCount, _has32BitIndices);
            return;
        }

        _indexBuffer?.Dispose();
        _indexBuffer = GraphicsBuffer.Create(new GraphicsBufferCreateInfo
        {
            Target = BufferTarget.Index,
            Count = (int)(elementCount * 1.5f),
            Stride = elementStride,
        });

        _input.SetIndexBuffer(_indexBuffer!, IndexCount, _has32BitIndices);
    }


    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _input.Dispose();

        foreach (GraphicsBuffer? buf in _vertexBuffers)
            buf?.Dispose();

        _indexBuffer?.Dispose();
    }
}


public struct MeshCreateInfo
{
    public static MeshCreateInfo Default = new()
    {
        VertexLayout =
        [
            new VertexInputDescriptor("POSITION", VertexInputFormat.Float3),
            new VertexInputDescriptor("NORMAL",   VertexInputFormat.Float3),
            new VertexInputDescriptor("TANGENT",  VertexInputFormat.Float3),
            new VertexInputDescriptor("UV0",      VertexInputFormat.Float4),
            new VertexInputDescriptor("UV1",      VertexInputFormat.Float4),
            new VertexInputDescriptor("UV2",      VertexInputFormat.Float4),
        ],

        Topology = PrimitiveTopology.Triangles
    };

    public VertexInputDescriptor[] VertexLayout;
    public PrimitiveTopology Topology;
}
