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

    private uint _buffer;
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

        uint buffer;
        BufferUsageARB hint = GetUsageHint(Target);

        if (_device.ARBDirectStateAccess)
        {
            gl.CreateBuffers(1, &buffer);
            gl.NamedBufferData(
                _buffer,
                (nuint)Size,
                null,
                (GLEnum)hint);
        }
        else
        {
            gl.GenBuffers(1, &buffer);
            gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, _buffer);
            gl.BufferData(
                BufferTargetARB.CopyWriteBuffer,
                (nuint)Size,
                null,
                hint);
        }

        _buffer = buffer;

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
        uint buffer = _buffer;
        gl.DeleteBuffers(1, &buffer);
        _buffer = buffer;
    }


    public override void GetData<T>(Memory<T> data, int managedSourceIndex, int graphicsBufferSourceIndex, int count)
    {
        EnsureResource();

        _device.Dispatcher.EnqueueTask((gl) =>
        {
            fixed (T* dataPtr = data.Span)
                GetBufferDataCore(gl, dataPtr, managedSourceIndex, graphicsBufferSourceIndex, count);

            gl.Flush();
            gl.Finish();
        }, true);
    }


    internal void GetBufferDataCore<T>(GL gl, T* data, int managedSourceIndex, int graphicsBufferSourceIndex, int count) where T : unmanaged
    {
        managedSourceIndex *= sizeof(T);
        graphicsBufferSourceIndex *= sizeof(T);
        count *= sizeof(T);

        if (_device.ARBDirectStateAccess)
        {
            gl.GetNamedBufferSubData(
                _buffer,
                graphicsBufferSourceIndex,
                (nuint)count,
                (byte*)data + managedSourceIndex);
        }
        else
        {
            BufferTargetARB bufferTarget = BufferTargetARB.CopyWriteBuffer;
            gl.BindBuffer(bufferTarget, _buffer);

            gl.GetBufferSubData(
                bufferTarget,
                graphicsBufferSourceIndex,
                (nuint)count,
                (byte*)data + managedSourceIndex);
        }
    }


    public override void SetData<T>(Memory<T> data, int managedSourceIndex, int graphicsBufferSourceIndex, int count)
    {
        EnsureResource();

        _device.Dispatcher.EnqueueTask((gl) =>
        {
            fixed (T* dataPtr = data.Span)
                SetBufferDataCore(gl, dataPtr, managedSourceIndex, graphicsBufferSourceIndex, count);

            gl.Flush();
            gl.Finish();
        }, true);
    }


    // Purely byte indices
    internal void SetBufferDataCore<T>(GL gl, T* data, int managedSourceIndex, int graphicsBufferSourceIndex, int count) where T : unmanaged
    {
        managedSourceIndex *= sizeof(T);
        graphicsBufferSourceIndex *= sizeof(T);
        count *= sizeof(T);

        if (_device.ARBDirectStateAccess)
        {
            gl.NamedBufferSubData(
                _buffer,
                graphicsBufferSourceIndex,
                (nuint)count,
                (byte*)data + managedSourceIndex);
        }
        else
        {
            BufferTargetARB bufferTarget = BufferTargetARB.CopyWriteBuffer;
            gl.BindBuffer(bufferTarget, _buffer);

            gl.BufferSubData(
                bufferTarget,
                graphicsBufferSourceIndex,
                (nuint)count,
                (byte*)data + managedSourceIndex);
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
        if (_device.ARBDirectStateAccess)
        {
            gl.CopyNamedBufferSubData(
                _buffer,
                ((GLGraphicsBuffer)destination)._buffer,
                sourceIndex,
                destinationIndex,
                (nuint)countBytes);
        }
        else
        {
            gl.BindBuffer(BufferTargetARB.CopyReadBuffer, _buffer);
            gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, ((GLGraphicsBuffer)destination)._buffer);

            gl.CopyBufferSubData(
                (GLEnum)BufferTargetARB.CopyReadBuffer,
                (GLEnum)BufferTargetARB.CopyWriteBuffer,
                sourceIndex,
                destinationIndex,
                (nuint)countBytes);
        }
    }


    public override unsafe void* MapBuffer()
    {
        EnsureResource();

        if (MapState == MapState.NotMappable)
            throw new Exception("Cannot map non-mappable buffer");

        int offset = 0;
        nuint sizeInBytes = 0;

        lock (_lock)
        {
            if (_mapState == MapState.Mapped)
                return _mapPtr;

            _device.Dispatcher.EnqueueTask((gl) =>
            {
                MapBufferAccessMask accessMask = MapBufferAccessMask.WriteBit | MapBufferAccessMask.InvalidateBufferBit | MapBufferAccessMask.InvalidateRangeBit;

                if (_device.ARBDirectStateAccess)
                {
                    _mapPtr = gl.MapNamedBufferRange(_buffer, offset, sizeInBytes, accessMask);
                }
                else
                {
                    gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, _buffer);

                    _mapPtr = gl.MapBufferRange(BufferTargetARB.CopyWriteBuffer, offset, sizeInBytes, accessMask);
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

        _device.Dispatcher.EnqueueTask((gl) =>
        {
            bool success;
            if (_device.ARBDirectStateAccess)
            {
                success = gl.UnmapNamedBuffer(_buffer);
            }
            else
            {
                gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, _buffer);

                success = gl.UnmapBuffer(BufferTargetARB.CopyWriteBuffer);
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
