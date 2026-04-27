using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal struct Draw : GLCommand
{
    public GLVertexInput Input;
    public int BaseVertex;
    public int IndexOffset;


    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        GLPipeline activePipeline = dispatcher.ActivePipeline;

        activePipeline.BindAttributes(gl, Input);

        nint indexOffset = IndexOffset * (Input.Indices32Bit ? 4 : 2);
        GLEnum indexType = Input.Indices32Bit ? GLEnum.UnsignedInt : GLEnum.UnsignedShort;

        gl.DrawElementsBaseVertex(
            Input.GLTopology,
            (uint)Input.IndexCount,
            indexType,
            indexOffset,
            BaseVertex);
    }
}
