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


    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        gl.ClearColor(System.Drawing.Color.FromArgb(ClearColor.A, ClearColor.R, ClearColor.G, ClearColor.B));
        gl.ClearDepth(ClearDepth);
        gl.ClearStencil(ClearStencil);

        gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit | ClearBufferMask.ColorBufferBit);
    }
}
