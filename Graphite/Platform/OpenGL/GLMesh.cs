using System;
using System.Runtime.InteropServices;

using Silk.NET.OpenGL;

namespace Prowl.Graphite.OpenGL;


public unsafe class GLMesh : Mesh, GLDeferredResource
{
    private GLGraphicsDevice _device;
    private VertexArray _vertexArray;

    private VertexInputDescriptor[] _inputLayout;

    private GLGraphicsBuffer[]? _vertexInputBuffers;
    internal GLGraphicsBuffer?[] VertexInputBuffers
    {
        get
        {
            _vertexInputBuffers ??= new GLGraphicsBuffer[_inputLayout.Length];

            if (_vertexInputBuffers.Length != _inputLayout.Length)
                Array.Resize(ref _vertexInputBuffers, _inputLayout.Length);

            return _vertexInputBuffers;
        }
    }

    private (Array?, bool)[]? _vertexInputArrays;
    internal (Array? Array, bool IsDirty)[] VertexInputArrays
    {
        get
        {
            _vertexInputArrays ??= new (Array?, bool)[_inputLayout.Length];

            if (_vertexInputArrays.Length != _inputLayout.Length)
                Array.Resize(ref _vertexInputArrays, _inputLayout.Length);

            return _vertexInputArrays;
        }
    }

    private bool _has32BitIndices;
    private MeshTopology _topology;
    private GLEnum _glTopology;

    private GLGraphicsBuffer? _indexBuffer;
    private (Array? Array, bool IsDirty) _indexArray;


    private bool _rebindBuffers;

    private bool _disposeRequested;


    public bool Created { get; private set; }

    public override VertexInputDescriptor[] InputLayout => _inputLayout;

    public override bool IsReadable { get; set; }

    public override bool Has32BitIndices => _has32BitIndices;

    public override MeshTopology Topology
    {
        get => _topology;
        set
        {
            _topology = value;

            _glTopology = _topology switch
            {
                MeshTopology.Triangles => GLEnum.Triangles,
                MeshTopology.Lines => GLEnum.Lines,
                MeshTopology.LineStrip => GLEnum.LineStrip,
                MeshTopology.Points => GLEnum.Points,
                _ => GLEnum.Triangles,
            };
        }
    }

    internal GLEnum GLTopology => _glTopology;


    internal bool BuffersBoundLegacy => !_device.UseModernBindingStyle;

    internal int VertexCount { get; private set; }

    internal int IndexCount { get; private set; }

    internal VertexArray VertexArray => _vertexArray;



    public GLMesh(MeshCreateInfo info, GLGraphicsDevice device)
    {
        _device = device;
        _inputLayout = info.VertexLayout;

        Topology = info.Topology;
    }




    public override void SetVertexInput<T>(Span<T> buffer, int stream)
    {
        if (stream == 0)
            VertexCount = buffer.Length;

        if (stream != 0 && buffer.Length < VertexCount)
            throw new Exception("Buffer size must be the same as vertex count");

        T[] dataCopy = new T[buffer.Length];
        buffer.CopyTo(dataCopy);
        VertexInputArrays[stream] = (dataCopy, true);
    }


    public override T[] GetVertexInput<T>(int stream)
    {
        if (!IsReadable)
            throw new Exception("Cannot read from write-only mesh");

        if (VertexInputArrays[stream].Array == null)
        {
            if (VertexInputBuffers[stream] == null)
                return [];

            T[] newData = new T[VertexCount];
            VertexInputBuffers[stream]!.GetData<T>(newData, 0);
            VertexInputArrays[stream] = (newData, false);
        }

        if (VertexInputArrays[stream].Array is T[] array)
            return array;

        Type? elementType = VertexInputArrays[stream]!.GetType().GetElementType();
        throw new Exception($"Attempted to read mesh vertex data stored as {elementType} as {typeof(T)}");
    }


    public override void SetIndexInput32(Span<uint> buffer)
    {
        _has32BitIndices = true;
        IndexCount = buffer.Length;

        uint[] indices = new uint[buffer.Length];
        buffer.CopyTo(indices);
        _indexArray = (indices, true);
    }


    public override void SetIndexInput16(Span<ushort> buffer)
    {
        _has32BitIndices = false;
        IndexCount = buffer.Length;

        ushort[] indices = new ushort[buffer.Length];
        buffer.CopyTo(indices);
        _indexArray = (indices, true);
    }


    public override uint[] GetIndexInput32()
    {
        if (!IsReadable)
            throw new Exception("Cannot read from write-only mesh");

        if (!Has32BitIndices)
            throw new Exception("Mesh contains 16-bit indices. Cannot read 32-bit indices");

        if (_indexArray.Array == null)
        {
            if (_indexBuffer == null)
                return [];

            uint[] newData = new uint[IndexCount];
            _indexBuffer.GetData<uint>(newData, 0);
            _indexArray = (newData, false);
        }

        return (_indexArray.Array as uint[])!;
    }


    public override ushort[] GetIndexInput16()
    {
        if (!IsReadable)
            throw new Exception("Cannot read from write-only mesh");

        if (Has32BitIndices)
            throw new Exception("Mesh contains 32-bit indices. Cannot read 16-bit indices");

        if (_indexArray.Array == null)
        {
            if (_indexBuffer == null)
                return [];

            ushort[] newData = new ushort[IndexCount];
            _indexBuffer.GetData<ushort>(newData, 0);
            _indexArray = (newData, false);
        }

        return (_indexArray.Array as ushort[])!;
    }


    public unsafe void CreateResource(GL gl)
    {
        UpdateMeshData(gl);
    }


    private void UpdateBuffer(GL gl, Array data, int elementStride, BufferTarget target, ref GLGraphicsBuffer? buffer)
    {
        int requestedSize = elementStride * data.Length;
        int capacity = buffer != null ? buffer.Size : 0;

        // Resize buffer if data is less than half capacity or larger than capacity.
        if (requestedSize > capacity || requestedSize < capacity / 2 || buffer == null)
        {
            _rebindBuffers = true;

            int newSize = (int)(data.Length * 1.5f);

            GraphicsBufferCreateInfo info = new()
            {
                Target = target,
                Count = newSize,
                Stride = elementStride,
            };

            buffer = new GLGraphicsBuffer(info, _device);
        }

        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        buffer!.SetBufferDataCore(gl, (byte*)handle.AddrOfPinnedObject(), 0, requestedSize);
        handle.Free();
    }


    private void CreateVertexArray(GL gl)
    {
        // Everything must be set on buffer bind, as forced to in legacy GL path.
        if (BuffersBoundLegacy)
        {
            gl.GenVertexArrays(1, out _vertexArray);
            return;
        }

        gl.CreateVertexArrays(1, out _vertexArray);

        for (uint i = 0; i < _inputLayout.Length; i++)
        {
            VertexInputDescriptor descriptor = _inputLayout[(int)i];

            gl.EnableVertexArrayAttrib(_vertexArray.Handle, i);
            gl.VertexArrayAttribFormat(_vertexArray.Handle, i, descriptor.Format.Dimension(), descriptor.Format.ToGLEnum(), false, 0);
        }
    }


    public void UpdateMeshData(GL gl)
    {
        // Update buffers with new data
        for (int i = 0; i < VertexInputBuffers.Length; i++)
        {
            (Array? data, bool isDirty) = VertexInputArrays[i];

            if (data == null || !isDirty)
                continue;

            UpdateBuffer(gl, data, _inputLayout[i].Format.Size(), BufferTarget.Vertex, ref VertexInputBuffers[i]);

            VertexInputArrays[i] = (data, false);
        }

        if (_indexArray.Array != null && _indexArray.IsDirty)
        {
            UpdateBuffer(gl, _indexArray.Array, _has32BitIndices ? sizeof(uint) : sizeof(ushort), BufferTarget.Index, ref _indexBuffer);
            _indexArray.IsDirty = false;
        }

        if (_vertexArray.Handle == 0)
            CreateVertexArray(gl);

        if (_rebindBuffers)
        {
            if (!BuffersBoundLegacy)
                BindBuffers(gl);
            else
                BindBuffersLegacy(gl);

            _rebindBuffers = false;
        }

        if (!IsReadable)
        {
            _indexArray.Array = null;

            for (int i = 0; i < VertexInputArrays.Length; i++)
                VertexInputArrays[i] = (null, false);
        }

        Created = true;
    }


    internal void BindBuffers(GL gl)
    {
        for (int i = 0; i < VertexInputBuffers.Length; i++)
        {
            if (VertexInputBuffers[i] == null)
                continue;

            gl.VertexArrayVertexBuffer(_vertexArray.Handle, (uint)i, VertexInputBuffers[i]!._buffer.Handle, 0, (uint)VertexInputBuffers[i]!.Stride);
        }

        if (_indexBuffer != null)
            gl.VertexArrayElementBuffer(_vertexArray.Handle, _indexBuffer._buffer.Handle);
    }


    internal void BindBuffersLegacy(GL gl)
    {
        gl.BindVertexArray(_vertexArray.Handle);

        // Moved to GLPipeline when buffers are getting mapped to shader inputs
        // for (int i = 0; i < VertexInputBuffers.Length; i++)
        // {
        //     if (VertexInputBuffers[i] == null)
        //         continue;

        //     VertexInputDescriptor descriptor = _inputLayout[i];

        //     gl.EnableVertexAttribArray((uint)i);
        //     gl.BindBuffer(BufferTargetARB.ArrayBuffer, VertexInputBuffers[i]._buffer.Handle);
        //     gl.VertexAttribPointer((uint)i, descriptor.Format.Dimension(), descriptor.Format.ToGLEnum(), false, (uint)descriptor.Format.Size(), (void*)0);
        // }

        if (_indexBuffer != null)
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer._buffer.Handle);

        gl.BindVertexArray(0);
    }


    internal VertexArray GetVAO()
    {
        return _vertexArray;
    }


    public void DestroyResource(GL gl)
    {
        if (_vertexArray.Handle != 0)
        {
            gl.DeleteVertexArrays(1, in _vertexArray);
            _device.Dispatcher.CheckError();
            _vertexArray = default;
        }

        if (_vertexInputBuffers != null)
        {
            foreach (GLGraphicsBuffer buffer in _vertexInputBuffers)
                buffer?.DestroyResource(gl);
        }

        _indexBuffer?.DestroyResource(gl);

        Created = false;
    }


    public override void Dispose()
    {
        if (!_disposeRequested)
        {
            _disposeRequested = true;
            _device.EnqueueDisposable(this);
        }
    }
}
