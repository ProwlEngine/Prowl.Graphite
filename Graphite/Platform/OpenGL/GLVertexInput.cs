using System.Runtime.CompilerServices;

using Silk.NET.OpenGL;

namespace Prowl.Graphite.OpenGL;


public class GLVertexInput : VertexInput, GLDeferredResource
{
    private GLGraphicsDevice _device;
    private VertexArray _vertexArray;

    private VertexInputDescriptor[] _inputLayout;

    private GLGraphicsBuffer[] _vertexInputBuffers;
    private GLGraphicsBuffer? _indexBuffer;


    private bool _rebindBuffers;
    private bool _disposeRequested;

    private int _indexCount;
    private bool _indices32Bit;

    public override int IndexCount => _indexCount;
    public override bool Indices32Bit => _indices32Bit;
    public bool Created { get; private set; }

    public override VertexInputDescriptor[] InputLayout => _inputLayout;


    private PrimitiveTopology _topology;

    public override PrimitiveTopology Topology
    {
        get => _topology;
        set => _topology = value;
    }

    internal GLEnum GLTopology => _topology switch
    {
        PrimitiveTopology.Triangles => GLEnum.Triangles,
        PrimitiveTopology.Lines => GLEnum.Lines,
        PrimitiveTopology.LineStrip => GLEnum.LineStrip,
        PrimitiveTopology.Points => GLEnum.Points,
        _ => GLEnum.Triangles,
    };


    internal bool BuffersBoundLegacy => !_device.UseModernBindingStyle;
    internal VertexArray VertexArray => _vertexArray;

    internal GLGraphicsBuffer[] VertexInputBuffers => _vertexInputBuffers;


    public GLVertexInput(VertexInputDescriptor[] inputDescriptors, PrimitiveTopology topology, GLGraphicsDevice device)
    {
        _device = device;
        _inputLayout = inputDescriptors;
        _vertexInputBuffers = new GLGraphicsBuffer[_inputLayout.Length];

        Topology = topology;
    }


    public override void SetVertexBuffer(GraphicsBuffer buffer, int stream)
    {
        _vertexInputBuffers[stream] = Unsafe.As<GLGraphicsBuffer>(buffer);
        _rebindBuffers = true;
    }

    public override void SetIndexBuffer(GraphicsBuffer buffer, int indexCount, bool is32Bit)
    {
        _indexBuffer = Unsafe.As<GLGraphicsBuffer>(buffer);
        _rebindBuffers = true;
        _indices32Bit = is32Bit;
        _indexCount = indexCount;
    }


    public void CreateResource(GL gl)
    {
        UpdateInputs(gl);
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


    public void UpdateInputs(GL gl)
    {
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
