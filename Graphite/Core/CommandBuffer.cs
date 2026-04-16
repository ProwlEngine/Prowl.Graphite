using System;

using Prowl.Vector;
using Prowl.Vector.Geometry;


namespace Prowl.Graphite;


public abstract unsafe class CommandBuffer : IDisposable
{
    /// <summary>
    /// CommandBuffer name. Can be used to identify a buffer in thrown exceptions or in debug messages.
    /// </summary>
    public required string Name { get; init; }


    public static CommandBuffer Create(string name = "", GraphicsDevice? device = null)
    {
        device ??= GraphicsDevice.Instance;

        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLCommandBuffer() { Name = name }
        };
    }


    public abstract void Dispose();

    public abstract void Clear();

    public abstract void ClearRenderTarget(Byte4 clearColor, double clearDepth, byte clearStencil);

    public abstract void DrawMesh(Mesh mesh, int baseVertex = 0, int indexOffset = 0);

    public abstract void DrawMeshIndirect(Mesh mesh, GraphicsBuffer indirectBuffer, int indirectArgsOffset = 0, int baseVertex = 0);

    public abstract void DrawMeshInstanced(Mesh mesh, int instanceCount, int baseInstance = 0, int baseVertex = 0, int indexOffset = 0);

    public abstract void SetScissorRect(Int4 rect);

    public abstract void SetScissorRects(Int4[] rect);

    public abstract void ClearScissorRect();

    public abstract void SetDepthRange(float near, float far);

    public abstract void SetViewport(Int2 position, Int2 size);

    public abstract void SetRenderTarget(RenderTarget? target);

    /// <summary>
    /// Sets the material and pass to be used in all subsequent draw calls until <see cref="SetMaterial"/> is called again.
    /// </summary>
    /// <param name="material"></param>
    /// <param name="pass"></param>
    public abstract void SetMaterial(Material material, int pass);

    public abstract void SetShader(Shader shader, int pass);

    public abstract void SetBufferData<T>(GraphicsBuffer buffer, Memory<T> data, int sourceIndex, int destinationIndex, int count) where T : unmanaged;

    public abstract void CopyBuffer(GraphicsBuffer source, GraphicsBuffer destination, int sourceIndex, int destinationIndex, int countBytes);
}
