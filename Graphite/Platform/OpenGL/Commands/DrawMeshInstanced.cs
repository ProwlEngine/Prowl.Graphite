using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal unsafe struct DrawMeshInstanced : GLCommand
{
    public GLMesh Mesh;
    public int InstanceCount;
    public int BaseVertex;
    public int BaseInstance;
    public int IndexOffset;




    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        GLPipeline activePipeline = dispatcher.ActivePipeline;

        activePipeline.BindAttributes(gl, Mesh);

        nint indexOffset = IndexOffset * (Mesh.Has32BitIndices ? 4 : 2);
        GLEnum indexType = Mesh.Has32BitIndices ? GLEnum.UnsignedInt : GLEnum.UnsignedShort;

        gl.DrawElementsInstancedBaseVertexBaseInstance(
            Mesh.GLTopology,
            (uint)Mesh.IndexCount,
            indexType,
            indexOffset,
            (uint)InstanceCount,
            BaseVertex,
            (uint)BaseInstance
        );
    }
}
