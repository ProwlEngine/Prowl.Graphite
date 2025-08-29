using System;
using System.Collections.Generic;

using Silk.NET.OpenGL;

namespace Prowl.Graphite.OpenGL;


public unsafe class GLMesh : Mesh, GLDeferredResource
{
    private GLGraphicsDevice _device;
    private VertexArray _vertexArray;

    private VertexInputDescriptor[] _inputLayout;

    private GLGraphicsBuffer[] _vertexInputBuffers;

    private Array?[] _vertexInputArrays;

    internal Array?[] VertexInputArrays
    {
        get
        {
            _vertexInputArrays ??= new Array?[_inputLayout.Length];

            if (_vertexInputArrays.Length != _inputLayout.Length)
                Array.Resize(ref _vertexInputArrays, _inputLayout.Length);

            return _vertexInputArrays;
        }
    }

    private bool _has32BitIndices;

    private GLGraphicsBuffer _indexBuffer;
    private Array? _indexArray;


    private bool _modifiedBuffers;
    private bool _modifiedLayout;


    public bool Created { get; private set; }

    public override bool IsReadable { get; set; }

    public override bool Has32BitIndices => _has32BitIndices;

    internal int VertexCount => VertexInputArrays[0] != null ? VertexInputArrays[0]!.Length : _vertexInputBuffers[0].Count;



    public GLMesh(MeshCreateInfo info, GLGraphicsDevice device)
    {
        _device = device;
        SetInputLayout(info.VertexLayout);
    }




    public override void SetVertexInput<T>(Span<T> buffer, int stream)
    {

    }


    public override T[] GetVertexInput<T>(int stream)
    {
        if (!IsReadable)
            throw new Exception("Cannot read from write-only mesh");

        if (VertexInputArrays[stream] == null)
        {
            if (_vertexInputBuffers[stream] == null)
                return [];

            T[] newData = new T[_vertexInputBuffers[stream].Count];
            _vertexInputBuffers[stream].GetData<T>(newData, 0);
            VertexInputArrays[stream] = newData;
        }

        if (VertexInputArrays[stream] is T[] array)
            return array;

        Type? elementType = VertexInputArrays[stream]!.GetType().GetElementType();
        throw new Exception($"Attempted to read mesh vertex data stored as {elementType} as {typeof(T)}");
    }


    public override void SetIndexInput32(Span<uint> buffer)
    {
        _has32BitIndices = true;

        uint[] indices = new uint[buffer.Length];
        buffer.CopyTo(indices);
        _indexArray = indices;
    }


    public override void SetIndexInput16(Span<ushort> buffer)
    {
        _has32BitIndices = false;

        ushort[] indices = new ushort[buffer.Length];
        buffer.CopyTo(indices);
        _indexArray = indices;
    }


    public override uint[] GetIndexInput32()
    {
        if (!IsReadable)
            throw new Exception("Cannot read from write-only mesh");

        if (!Has32BitIndices)
            throw new Exception("Mesh contains 16-bit indices. Cannot read 32-bit indices");

        if (_indexArray == null)
        {
            if (_indexBuffer == null)
                return [];

            uint[] newData = new uint[_indexBuffer.Count];
            _indexBuffer.GetData<uint>(newData, 0);
            _indexArray = newData;
        }

        return (_indexArray as uint[])!;
    }


    public override ushort[] GetIndexInput16()
    {
        if (!IsReadable)
            throw new Exception("Cannot read from write-only mesh");

        if (Has32BitIndices)
            throw new Exception("Mesh contains 32-bit indices. Cannot read 16-bit indices");

        if (_indexArray == null)
        {
            if (_indexBuffer == null)
                return [];

            ushort[] newData = new ushort[_indexBuffer.Count];
            _indexBuffer.GetData<ushort>(newData, 0);
            _indexArray = newData;
        }

        return (_indexArray as ushort[])!;
    }


    public override void SetInputLayout(IEnumerable<VertexInputDescriptor> layout)
    {
        _inputLayout = [.. layout];

        if (_inputLayout.Length == 0)
            _inputLayout = [new VertexInputDescriptor("", VertexInputFormat.Float1)];

        _modifiedLayout = true;
    }


    public unsafe void CreateResource(GL gl)
    {
        UpdateMeshData(gl);
    }


    public void UpdateMeshData(GL gl)
    {
        if (_modifiedBuffers)
        {

        }

        if (!IsReadable)
        {
            _indexArray = null;

            for (int i = 0; i < VertexInputArrays.Length; i++)
                VertexInputArrays[i] = null;
        }
    }


    public void DestroyResource(GL gl)
    {

    }
}
