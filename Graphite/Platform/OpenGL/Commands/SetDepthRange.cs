using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal unsafe struct SetDepthRange : GLCommand
{
    public double Near;
    public double Far;


    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        gl.DepthRange(Near, Far);
    }
}
