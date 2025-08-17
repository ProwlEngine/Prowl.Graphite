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


    public void Execute(GLDispatcher dispatcher)
    {
        if (Set)
        {
            if (ScissorRects == null)
            {
                dispatcher.Gl.ScissorIndexed(ViewportIndex, ScissorRect.X, ScissorRect.Y, (uint)ScissorRect.Z, (uint)ScissorRect.W);
            }
            else
            {
                fixed (Int4* ScissorRectsPtr = ScissorRects)
                    dispatcher.Gl.ScissorArray(ViewportIndex, (uint)ScissorRects.Length, (int*)ScissorRectsPtr);
            }
        }

        if (Enable != null)
        {
            if (Enable.Value)
                dispatcher.Gl.Enable(GLEnum.ScissorTest);
            else
                dispatcher.Gl.Disable(GLEnum.ScissorTest);
        }
    }
}
