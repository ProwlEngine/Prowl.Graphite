using System;
using System.Diagnostics;

using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal sealed unsafe class GLGraphicsBuffer : GraphicsBuffer, GLDeferredResource
{
    private object _lock = new();

    private bool _disposeRequested;
    private GLGraphicsDevice _device;

    public bool Created { get; private set; }

    public override bool IsValid => Created;

    private MapState _mapState;
    public override MapState MapState => _mapState;

    private int _count;
    public override int Count => _count;

    private int _stride;
    public override int Stride => _stride;

    private BufferTarget _target;
    public override BufferTarget Target => _target;

    private BufferUsage _usage;
    public override BufferUsage Usage => _usage;

    private Silk.NET.OpenGL.Buffer _buffer;
    private void* _mapPtr;


    public GLGraphicsBuffer(GraphicsBufferCreateInfo info, GLGraphicsDevice device)
    {
        _device = device;
        _count = info.Count;
        _stride = info.Stride;
        _target = info.Target;
        _usage = info.Usage;

        EnsureResource();
    }


    public static BufferUsageARB GetUsageHint(BufferTarget target)
    {
        if (target.HasFlag(BufferTarget.Uniform))
        {
            return BufferUsageARB.DynamicDraw;
        }
        else
        {
            return BufferUsageARB.StaticDraw;
        }
    }


    public void EnsureResource()
    {
        lock (_lock)
        {
            if (!Created)
                _device.Dispatcher.CreateResource(this, true);
        }
    }


    public void CreateResource(GL gl)
    {
        if (Created)
            return;

        BufferUsageARB hint = GetUsageHint(Target);
        GLDispatcher dispatcher = _device.Dispatcher;

        if (_device.ARBDirectStateAccess)
        {
            gl.CreateBuffers(1, out _buffer);
            dispatcher.CheckError();

            gl.NamedBufferData(
                _buffer.Handle,
                (nuint)Size,
                null,
                (GLEnum)hint);
            dispatcher.CheckError();
        }
        else
        {
            gl.GenBuffers(1, out _buffer);
            dispatcher.CheckError();

            gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, _buffer.Handle);
            dispatcher.CheckError();

            gl.BufferData(
                BufferTargetARB.CopyWriteBuffer,
                (nuint)Size,
                null,
                hint);
            dispatcher.CheckError();
        }

        Created = true;
    }


    public override void Dispose()
    {
        if (!_disposeRequested)
        {
            _disposeRequested = true;
            _device.EnqueueDisposable(this);
        }
    }


    public void DestroyResource(GL gl)
    {
        gl.DeleteBuffers(1, in _buffer);
        _device.Dispatcher.CheckError();
    }


    public override void GetData<T>(Memory<T> data, int graphicsBufferSourceIndex)
    {
        EnsureResource();

        _device.Dispatcher.EnqueueTask((gl) =>
        {
            fixed (T* dataPtr = data.Span)
                GetBufferDataCore(gl, dataPtr, graphicsBufferSourceIndex, data.Length);

            gl.Flush();
            gl.Finish();
        }, true);
    }


    internal void GetBufferDataCore<T>(GL gl, T* data, int graphicsBufferSourceIndex, int count) where T : unmanaged
    {
        graphicsBufferSourceIndex *= sizeof(T);
        count *= sizeof(T);
        GLDispatcher dispatcher = _device.Dispatcher;

        if (_device.ARBDirectStateAccess)
        {
            gl.GetNamedBufferSubData(
                _buffer.Handle,
                graphicsBufferSourceIndex,
                (nuint)count,
                data);
            dispatcher.CheckError();
        }
        else
        {
            BufferTargetARB bufferTarget = BufferTargetARB.CopyWriteBuffer;
            gl.BindBuffer(bufferTarget, _buffer.Handle);
            dispatcher.CheckError();

            gl.GetBufferSubData(
                bufferTarget,
                graphicsBufferSourceIndex,
                (nuint)count,
                data);
            dispatcher.CheckError();
        }
    }


    public override void SetData<T>(Memory<T> data, int graphicsBufferSourceIndex)
    {
        EnsureResource();

        _device.Dispatcher.EnqueueTask((gl) =>
        {
            fixed (T* dataPtr = data.Span)
                SetBufferDataCore(gl, dataPtr, graphicsBufferSourceIndex, data.Length);

            gl.Flush();
            gl.Finish();
        }, true);
    }


    // Purely byte indices
    internal void SetBufferDataCore<T>(GL gl, T* data, int graphicsBufferSourceIndex, int count) where T : unmanaged
    {
        graphicsBufferSourceIndex *= sizeof(T);
        count *= sizeof(T);
        GLDispatcher dispatcher = _device.Dispatcher;

        if (_device.ARBDirectStateAccess)
        {
            gl.NamedBufferSubData(
                _buffer.Handle,
                graphicsBufferSourceIndex,
                (nuint)count,
                data);
            dispatcher.CheckError();
        }
        else
        {
            BufferTargetARB bufferTarget = BufferTargetARB.CopyWriteBuffer;
            gl.BindBuffer(bufferTarget, _buffer.Handle);
            dispatcher.CheckError();

            gl.BufferSubData(
                bufferTarget,
                graphicsBufferSourceIndex,
                (nuint)count,
                data);
            dispatcher.CheckError();
        }
    }


    public override void CopyBuffer(GraphicsBuffer destination, int sourceIndex, int destinationIndex, int countBytes)
    {
        EnsureResource();

        _device.Dispatcher.EnqueueTask((gl) =>
        {
            CopyBufferCore(gl, destination, sourceIndex, destinationIndex, countBytes);

            gl.Flush();
            gl.Finish();
        }, true);
    }


    internal void CopyBufferCore(GL gl, GraphicsBuffer destination, int sourceIndex, int destinationIndex, int countBytes)
    {
        GLDispatcher dispatcher = _device.Dispatcher;

        if (_device.ARBDirectStateAccess)
        {
            gl.CopyNamedBufferSubData(
                _buffer.Handle,
                ((GLGraphicsBuffer)destination)._buffer.Handle,
                sourceIndex,
                destinationIndex,
                (nuint)countBytes);
            dispatcher.CheckError();
        }
        else
        {
            gl.BindBuffer(BufferTargetARB.CopyReadBuffer, _buffer.Handle);
            dispatcher.CheckError();

            gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, ((GLGraphicsBuffer)destination)._buffer.Handle);
            dispatcher.CheckError();

            gl.CopyBufferSubData(
                (GLEnum)BufferTargetARB.CopyReadBuffer,
                (GLEnum)BufferTargetARB.CopyWriteBuffer,
                sourceIndex,
                destinationIndex,
                (nuint)countBytes);
            dispatcher.CheckError();
        }
    }


    public override unsafe void* MapBuffer()
    {
        EnsureResource();

        if (MapState == MapState.NotMappable)
            throw new Exception("Cannot map non-mappable buffer");

        int offset = 0;
        nuint sizeInBytes = 0;

        GLDispatcher dispatcher = _device.Dispatcher;

        lock (_lock)
        {
            if (_mapState == MapState.Mapped)
                return _mapPtr;

            dispatcher.EnqueueTask((gl) =>
            {
                MapBufferAccessMask accessMask = MapBufferAccessMask.WriteBit | MapBufferAccessMask.InvalidateBufferBit | MapBufferAccessMask.InvalidateRangeBit;

                if (_device.ARBDirectStateAccess)
                {
                    _mapPtr = gl.MapNamedBufferRange(_buffer.Handle, offset, sizeInBytes, accessMask);
                    dispatcher.CheckError();
                }
                else
                {
                    gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, _buffer.Handle);
                    dispatcher.CheckError();

                    _mapPtr = gl.MapBufferRange(BufferTargetARB.CopyWriteBuffer, offset, sizeInBytes, accessMask);
                    dispatcher.CheckError();
                }
            }, true);

            _mapState = MapState.Mapped;
            return _mapPtr;
        }
    }


    public override void UnmapBuffer()
    {
        // More performance if it saves creating the resource!!!!
        if (MapState == MapState.NotMappable)
            throw new Exception("Cannot unmap non-mappable buffer");

        EnsureResource();

        if (_mapState == MapState.Unmapped)
            return;

        GLDispatcher dispatcher = _device.Dispatcher;

        _device.Dispatcher.EnqueueTask((gl) =>
        {
            bool success;
            if (_device.ARBDirectStateAccess)
            {
                success = gl.UnmapNamedBuffer(_buffer.Handle);
                dispatcher.CheckError();
            }
            else
            {
                gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, _buffer.Handle);
                dispatcher.CheckError();

                success = gl.UnmapBuffer(BufferTargetARB.CopyWriteBuffer);
                dispatcher.CheckError();
            }

            if (!success)
            {
                throw new Exception("Corrupt map");
            }

            lock (_lock)
            {
                _mapPtr = null;
                _mapState = MapState.Unmapped;
            }
        });
    }
}
