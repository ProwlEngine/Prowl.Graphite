using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal struct DrawIndirect : GLCommand
{
    public GLVertexInput VertexInput;
    public int BaseVertex;
    public GLGraphicsBuffer IndirectBuffer;
    public int IndirectArgsOffset;




    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        GLPipeline activePipeline = dispatcher.ActivePipeline;

        activePipeline.BindAttributes(gl, VertexInput);

        GLEnum indexType = VertexInput.Indices32Bit ? GLEnum.UnsignedInt : GLEnum.UnsignedShort;

        IndirectBuffer.EnsureResource();

        gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, IndirectBuffer._buffer.Handle);
        gl.DrawElementsIndirect(VertexInput.GLTopology, indexType, IndirectArgsOffset);
    }
}
