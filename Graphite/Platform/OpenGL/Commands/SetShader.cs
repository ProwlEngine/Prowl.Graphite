using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal struct SetShader : GLCommand
{
    public GLShaderData ShaderData;


    public void Execute(GLDispatcher dispatcher, GL gl)
    {
        GLPipeline.GetPipeline(ShaderData).ApplyRenderState(dispatcher, gl);
    }
}
