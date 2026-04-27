using Silk.NET.OpenGL;

using Prowl.Vector;


namespace Prowl.Graphite.OpenGL;


internal unsafe struct SetScissorRect : GLCommand
{
    public bool? Enable;

    public bool Set;

    public Int4 ScissorRect;


    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        if (Set)
        {
            gl.Scissor(ScissorRect.X, ScissorRect.Y, (uint)ScissorRect.Z, (uint)ScissorRect.W);
        }

        if (Enable != null)
        {
            if (Enable.Value)
                gl.Enable(GLEnum.ScissorTest);
            else
                gl.Disable(GLEnum.ScissorTest);
        }
    }
}
