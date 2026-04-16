using Silk.NET.OpenGL;

using Prowl.Vector;
using System;
using System.Threading;

using System.Linq;


namespace Prowl.Graphite.OpenGL;


internal struct SetShader : GLCommand
{
    public GLShaderData ShaderData;


    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        GLPipeline.GetPipeline(ShaderData).ApplyRenderState(dispatcher, gl);
    }
}
