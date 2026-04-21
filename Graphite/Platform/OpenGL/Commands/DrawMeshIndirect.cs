using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal unsafe struct DrawMeshIndirect : GLCommand
{
    public GLMesh Mesh;
    public int BaseVertex;
    public GLGraphicsBuffer IndirectBuffer;
    public int IndirectArgsOffset;




    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        GLPipeline activePipeline = dispatcher.ActivePipeline;

        activePipeline.BindAttributes(gl, Mesh);

        GLEnum indexType = Mesh.Has32BitIndices ? GLEnum.UnsignedInt : GLEnum.UnsignedShort;

        IndirectBuffer.EnsureResource();

        gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, IndirectBuffer._buffer.Handle);
        gl.DrawElementsIndirect(Mesh.GLTopology, indexType, IndirectArgsOffset);
    }
}
