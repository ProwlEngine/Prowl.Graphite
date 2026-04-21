using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Prowl.Vector;


namespace Prowl.Graphite.OpenGL;


public class GLCommandBuffer : CommandBuffer
{
    internal Queue<GLCommand> Commands { get; private set; } = [];


    public override void Dispose()
    {

    }


    public override void Clear()
    {
        Commands.Clear();
    }


    public override void ClearRenderTarget(Byte4 clearColor, double clearDepth, byte clearStencil)
    {
        Commands.Enqueue(new ClearRenderTarget() { ClearColor = clearColor, ClearDepth = clearDepth, ClearStencil = clearStencil });
    }


    public override void DrawMesh(Mesh mesh, int baseVertex = 0, int indexOffset = 0)
    {
        Commands.Enqueue(new DrawMesh() { Mesh = Unsafe.As<GLMesh>(mesh), BaseVertex = baseVertex, IndexOffset = indexOffset });
    }


    public override void DrawMeshIndirect(Mesh mesh, GraphicsBuffer indirectBuffer, int indirectArgsOffset = 0, int baseVertex = 0)
    {
        Commands.Enqueue(new DrawMeshIndirect()
        {
            Mesh = Unsafe.As<GLMesh>(mesh),
            IndirectBuffer = Unsafe.As<GLGraphicsBuffer>(indirectBuffer),
            IndirectArgsOffset = indirectArgsOffset,
            BaseVertex = baseVertex
        });
    }


    public override void DrawMeshInstanced(Mesh mesh, int instanceCount, int baseInstance = 0, int baseVertex = 0, int indexOffset = 0)
    {
        Commands.Enqueue(new DrawMeshInstanced()
        {
            Mesh = Unsafe.As<GLMesh>(mesh),
            InstanceCount = instanceCount,
            BaseInstance = baseInstance,
            BaseVertex = baseVertex,
            IndexOffset = indexOffset
        });
    }


    public override void SetScissorRect(Int4 rect)
    {
        Commands.Enqueue(new SetScissorRect() { Enable = true, ScissorRect = rect });
    }


    public override void SetScissorRects(Int4[] rects)
    {
        Commands.Enqueue(new SetScissorRect() { Enable = true, ScissorRects = rects });
    }


    public override void ClearScissorRect()
    {
        Commands.Enqueue(new SetScissorRect() { Enable = false });
    }


    public override void SetRenderTarget(RenderTarget? target = null)
    {
        Commands.Enqueue(new SetRenderTarget() { Target = Unsafe.As<GLRenderTarget>(target) });
    }


    /// <summary>
    /// Sets the material and shader pass to be used in all subsequent draw calls until <see cref="SetMaterial"/> is called again.
    /// </summary>
    /// <param name="material"></param>
    /// <param name="passIndex"></param>
    public override void SetMaterial(Material material, int passIndex)
    {
        ArgumentNullException.ThrowIfNull(material, nameof(material));

        SetShader(material.Shader, passIndex);
    }


    public override void SetShader(Shader shader, int passIndex)
    {
        if (passIndex < 0 || shader.Passes.Length >= passIndex)
            throw new ArgumentOutOfRangeException($"Pass '{passIndex}' is outside of pass range for shader '{shader.Name}'");

        ShaderPass pass = shader.Passes[passIndex];

        if (!pass.GetBackend(GraphicsBackend.OpenGL, out ShaderData? data))
            throw new Exception("No shader backend data for OpenGL pass.");

        Commands.Enqueue(new SetShader() { ShaderData = Unsafe.As<GLShaderData>(data!) });
    }


    public override void SetBufferData<T>(GraphicsBuffer buffer, Memory<T> data, int sourceIndex, int destinationIndex, int count)
    {
        Commands.Enqueue(new SetBufferData<T>()
        {
            Destination = Unsafe.As<GLGraphicsBuffer>(buffer),
            SourceData = data,
            SourceIndex = sourceIndex,
            DestinationIndex = destinationIndex,
            Count = count
        });
    }


    public override void CopyBuffer(GraphicsBuffer source, GraphicsBuffer destination, int sourceIndex, int destinationIndex, int countBytes)
    {
        Commands.Enqueue(new SetBufferData<byte>()
        {
            Destination = Unsafe.As<GLGraphicsBuffer>(destination),
            SourceBuffer = Unsafe.As<GLGraphicsBuffer>(source),
            SourceIndex = sourceIndex,
            DestinationIndex = destinationIndex,
            Count = countBytes
        });
    }


    public override void SetDepthRange(float near, float far)
    {
        Commands.Enqueue(new SetDepthRange() { Near = near, Far = far });
    }


    public override void SetViewport(Int2 position, Int2 size)
    {
        Commands.Enqueue(new SetViewport() { Viewport = new Int4(position, size) });
    }
}
