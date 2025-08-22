using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Prowl.Vector;


namespace Prowl.Graphite.OpenGL;


public class GLCommandBuffer : CommandBuffer
{
    internal Queue<GLCommand> _glCommands = [];


    public override void Dispose()
    {

    }


    public override void Clear()
    {
        _glCommands.Clear();
    }


    public override void ClearRenderTarget(Byte4 clearColor, double clearDepth, byte clearStencil)
    {
        _glCommands.Enqueue(new ClearRenderTarget() { ClearColor = clearColor, ClearDepth = clearDepth, ClearStencil = clearStencil });
    }


    public override void DrawMesh(Mesh mesh, Material material)
    {

    }


    public override void SetScissorRect(Int4 rect, int viewport = 0)
    {
        _glCommands.Enqueue(new SetScissorRect() { Enable = true, ScissorRect = rect, ViewportIndex = (uint)viewport });
    }


    public override void SetScissorRects(Int4[] rects, int viewportStartIndex = 0)
    {
        _glCommands.Enqueue(new SetScissorRect() { Enable = true, ScissorRects = rects, ViewportIndex = (uint)viewportStartIndex });
    }


    public override void ClearScissorRect()
    {
        _glCommands.Enqueue(new SetScissorRect() { Enable = false });
    }


    public override void SetRenderTarget(RenderTexture? target = null)
    {
        _glCommands.Enqueue(new SetRenderTarget() { Target = Unsafe.As<GLRenderTexture?>(target) });
    }


    /// <summary>
    /// Sets the material and pass to be used in all subsequent draw calls until <see cref="SetMaterial"/> is called again.
    /// </summary>
    /// <param name="material"></param>
    /// <param name="pass"></param>
    public override void SetMaterial(Material material, int pass)
    {

    }
}
