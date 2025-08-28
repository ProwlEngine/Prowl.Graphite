using Silk.NET.OpenGL;

using Prowl.Vector;
using System;
using System.Threading;


namespace Prowl.Graphite.OpenGL;


internal unsafe struct DrawMesh : GLCommand
{
    public GLMesh Mesh;
    public GLMaterial Material;


    public void Execute(GLDispatcher dispatcher, GL gl)
    {

    }
}
