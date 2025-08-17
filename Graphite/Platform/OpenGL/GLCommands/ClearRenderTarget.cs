using Silk.NET.OpenGL;

using Prowl.Vector;
using System;
using System.Threading;


namespace Prowl.Graphite.OpenGL;


internal struct ClearRenderTarget : GLCommand
{
    public Byte4 ClearColor;
    public double ClearDepth;
    public byte ClearStencil;


    public void Execute(GLDispatcher dispatcher)
    {
        dispatcher.Gl.ClearColor(System.Drawing.Color.FromArgb(ClearColor.A, ClearColor.R, ClearColor.G, ClearColor.B));
        dispatcher.Gl.ClearDepth(ClearDepth);
        dispatcher.Gl.ClearStencil(ClearStencil);

        dispatcher.Gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit | ClearBufferMask.ColorBufferBit);
    }
}
