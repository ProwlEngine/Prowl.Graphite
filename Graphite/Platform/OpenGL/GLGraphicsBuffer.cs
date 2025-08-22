using System;
using System.Diagnostics;

using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal sealed unsafe class GLGraphicsBuffer : GraphicsBuffer, GLDeferredResource
{
    private object _lock = new();

    private bool _disposeRequested;
    private bool _canBufferSubData;
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

    public static BufferStorageMask GetStorageMask(BufferUsage usage)
    {
        BufferStorageMask storageMask = 0;

        if (usage.HasFlag(BufferUsage.MapForWrite))
        {
            storageMask |= BufferStorageMask.MapWriteBit;
            storageMask |= BufferStorageMask.ClientStorageBit;
            storageMask |= BufferStorageMask.DynamicStorageBit;
        }

        return storageMask;
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

        BufferStorageMask mask = GetStorageMask(Usage);

        if (_device.ARBDirectStateAccess)
        {
            uint buffer;
            gl.CreateBuffers(1, &buffer);
            _buffer = buffer;


            if (mask != 0 && _device.ARBBufferStorage)
            {
                gl.NamedBufferStorage(
                    _buffer,
                    (nuint)Size,
                    null,
                    mask);
                _canBufferSubData = (mask & BufferStorageMask.DynamicStorageBit) != 0;
            }
            else
            {
                BufferUsageARB hint = GetUsageHint(Target);
                gl.NamedBufferData(
                    _buffer,
                    (nuint)Size,
                    null,
                    (GLEnum)hint);
                _canBufferSubData = true;
            }
        }
        else
        {
            uint buffer;
            gl.GenBuffers(1, &buffer);
            _buffer = buffer;

            gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, _buffer);

            if (mask != 0 && _device.ARBBufferStorage)
            {
                gl.BufferStorage(
                    (GLEnum)BufferTargetARB.CopyWriteBuffer,
                    (nuint)Size,
                    null,
                    (uint)mask);
                _canBufferSubData = (mask & BufferStorageMask.DynamicStorageBit) != 0;
            }
            else
            {
                BufferUsageARB hint = GetUsageHint(Target);
                gl.BufferData(
                    BufferTargetARB.CopyWriteBuffer,
                    (nuint)Size,
                    null,
                    hint);
                _canBufferSubData = true;
            }
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
        uint buffer = _buffer;
        gl.DeleteBuffers(1, &buffer);
        _buffer = buffer;
    }


    public override unsafe void GetData(void* data, int destinationIndex, int sourceIndex, int countBytes)
    {
    }


    public override unsafe void SetData(void* data, int sourceIndex, int destinationIndex, int countBytes)
    {
        if (!_canBufferSubData)
            throw new Exception("Cannot write to read-only buffer");
    }


    public override unsafe void* MapBuffer()
    {
        EnsureResource();

        if (!_canBufferSubData)
            throw new Exception("Cannot write to read-only buffer");

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
