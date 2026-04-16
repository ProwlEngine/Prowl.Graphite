using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal interface GLCommand
{
    void Execute(GLDispatcher dispatcher, GL gl);
}
