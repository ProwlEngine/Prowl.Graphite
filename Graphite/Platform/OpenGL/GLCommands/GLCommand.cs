namespace Prowl.Graphite.OpenGL;


internal enum GLCommandType
{

}


internal interface GLCommand
{
    public GLCommandType Command { get; }
}
