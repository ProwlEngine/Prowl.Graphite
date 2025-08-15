using System;
using System.IO;


namespace Prowl.Graphite;


// Will use slang so no need for abstraction
public static class ShaderCompiler
{
    public static bool CompileShader(string sourceString, out Shader result, FileInfo? filePath = null)
    {
        result = null;
        return false;
    }


    public static bool CompileShader(Memory<byte> sourceBytes, out Shader result, FileInfo? filePath = null)
    {
        result = null;
        return false;
    }
}
