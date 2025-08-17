using System;

using Prowl.Vector;
using Prowl.Vector.Geometry;


namespace Prowl.Graphite;


public abstract class CommandBuffer : IDisposable
{
    /// <summary>
    /// CommandBuffer name. Can be used to identify a buffer in thrown exceptions or in debug messages.
    /// </summary>
    public required string Name { get; init; }


    public static CommandBuffer Create(string name = "")
    {
        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLCommandBuffer() { Name = name }
        };
    }


    public abstract void Dispose();

    public abstract void Clear();

    public abstract void ClearRenderTarget(Byte4 clearColor, double clearDepth, byte clearStencil);

    public abstract void DrawMesh(Mesh mesh, Material material);

    public abstract void SetScissorRect(Int4 rect, int viewport = 0);

    public abstract void SetScissorRects(Int4[] rect, int viewport = 0);

    public abstract void ClearScissorRect();

    public abstract void SetRenderTarget(RenderTexture? target);

    /// <summary>
    /// Sets the material and pass to be used in all subsequent draw calls until <see cref="SetMaterial"/> is called again.
    /// </summary>
    /// <param name="material"></param>
    /// <param name="pass"></param>
    public abstract void SetMaterial(Material material, int pass);

    /// <summary>
    /// Makes a primitive draw call for a
    /// </summary>
    /// <param name="topology"></param>
    /// <param name="vertexCount"></param>
    /// <param name="instanceCount"></param>
    /// <param name="vertexStart"></param>
    /// <param name="instanceStart"></param>
    public abstract void DrawPrimitives(MeshTopology topology, uint vertexCount, uint instanceCount = 1, uint vertexStart = 0, uint instanceStart = 0);

    public abstract void DrawPrimitivesIndexed(MeshTopology topology, GraphicsBuffer indexBuffer, int indexCount, int startIndex = 0, int instanceCount = 1);
}
