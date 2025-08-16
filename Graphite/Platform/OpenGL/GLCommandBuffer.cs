using System;
using System.Collections.Generic;

using Prowl.Vector;


namespace Prowl.Graphite.OpenGL;


internal enum GLCommandType
{

}


internal struct GLCommand
{
    public GLCommandType Opcode;
    public object Item0;
    public object Item1;
}


public class GLCommandBuffer : CommandBuffer
{
    internal Queue<GLCommand> _glCommands = [];


    public override void Dispose()
    {
        _glCommands = null;
    }


    public override void Clear()
    {
        _glCommands.Clear();
    }


    public override void ClearRenderTarget(Vector4 clearColor, double clearDepth, byte clearStencil)
    {
    }


    public override void DrawMesh(Mesh mesh, Material material)
    {

    }

    public override void SetScissorRect(Rect rect)
    {

    }

    public override void ClearScissorRect()
    {

    }

    public override void SetRenderTarget(RenderTexture? target = null)
    {
    }

    /// <summary>
    /// Sets the material and pass to be used in all subsequent draw calls until <see cref="SetMaterial"/> is called again.
    /// </summary>
    /// <param name="material"></param>
    /// <param name="pass"></param>
    public override void SetMaterial(Material material, int pass)
    {

    }

    /// <summary>
    /// Makes a primitive draw call for a
    /// </summary>
    /// <param name="topology"></param>
    /// <param name="vertexCount"></param>
    /// <param name="instanceCount"></param>
    /// <param name="vertexStart"></param>
    /// <param name="instanceStart"></param>
    public override void DrawPrimitives(MeshTopology topology, uint vertexCount, uint instanceCount = 1, uint vertexStart = 0, uint instanceStart = 0)
    {

    }

    public override void DrawPrimitivesIndexed(MeshTopology topology, GraphicsBuffer indexBuffer, int indexCount, int startIndex = 0, int instanceCount = 1)
    {

    }
}
