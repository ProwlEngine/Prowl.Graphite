using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal unsafe struct DrawMesh : GLCommand
{
    public GLMesh Mesh;
    public int BaseVertex;
    public int IndexOffset;


    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        GLPipeline activePipeline = dispatcher.ActivePipeline;

        activePipeline.BindAttributes(gl, Mesh);

        nint indexOffset = IndexOffset * (Mesh.Has32BitIndices ? 4 : 2);
        GLEnum indexType = Mesh.Has32BitIndices ? GLEnum.UnsignedInt : GLEnum.UnsignedShort;

        gl.DrawElementsBaseVertex(
            Mesh.GLTopology,
            (uint)Mesh.IndexCount,
            indexType,
            indexOffset,
            BaseVertex);
    }
}
