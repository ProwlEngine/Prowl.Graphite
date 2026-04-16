using Silk.NET.OpenGL;

using Prowl.Vector;


namespace Prowl.Graphite.OpenGL;


internal unsafe struct SetViewport : GLCommand
{
    public Int4 Viewport;


    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        gl.Viewport(Viewport.X, Viewport.Y, (uint)Viewport.Z, (uint)Viewport.W);
    }
}
