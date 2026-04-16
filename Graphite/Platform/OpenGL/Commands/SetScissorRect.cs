using Silk.NET.OpenGL;

using Prowl.Vector;


namespace Prowl.Graphite.OpenGL;


internal unsafe struct SetScissorRect : GLCommand
{
    public bool? Enable;

    public bool Set;

    public uint ViewportIndex;
    public Int4 ScissorRect;
    public Int4[] ScissorRects;


    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        if (Set)
        {
            if (ScissorRects == null)
            {
                gl.ScissorIndexed(ViewportIndex, ScissorRect.X, ScissorRect.Y, (uint)ScissorRect.Z, (uint)ScissorRect.W);
            }
            else
            {
                fixed (Int4* ScissorRectsPtr = ScissorRects)
                    gl.ScissorArray(ViewportIndex, (uint)ScissorRects.Length, (int*)ScissorRectsPtr);
            }
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
