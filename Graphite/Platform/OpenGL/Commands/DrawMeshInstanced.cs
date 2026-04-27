using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal struct DrawInstanced : GLCommand
{
    public GLVertexInput VertexInput;
    public int InstanceCount;
    public int BaseVertex;
    public int BaseInstance;
    public int IndexOffset;




    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        GLPipeline activePipeline = dispatcher.ActivePipeline;

        activePipeline.BindAttributes(gl, VertexInput);

        nint indexOffset = IndexOffset * (VertexInput.Indices32Bit ? 4 : 2);
        GLEnum indexType = VertexInput.Indices32Bit ? GLEnum.UnsignedInt : GLEnum.UnsignedShort;

        gl.DrawElementsInstancedBaseVertexBaseInstance(
            VertexInput.GLTopology,
            (uint)VertexInput.IndexCount,
            indexType,
            indexOffset,
            (uint)InstanceCount,
            BaseVertex,
            (uint)BaseInstance
        );
    }
}
