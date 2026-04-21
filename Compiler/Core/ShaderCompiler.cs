using System.IO;

using Prowl.Slang;
using Prowl.Graphite.Compiler.Parser;


using Superpower;


namespace Prowl.Graphite.Compiler;


// Will use slang so no need for abstraction
public class ShaderCompiler
{
    /// <inheritdoc cref="CompileShader"/>
    public Shader CompileShader(FileInfo sourceFile) =>
        CompileShader(File.ReadAllText(sourceFile.FullName));


    /// <summary>
    /// Compiles yo shi for ya
    /// </summary>
    /// <exception cref="CompilationException">
    /// Thrown when the Slang shader compiler fails to compile shader code.
    /// View <see cref="CompilationException.Diagnostics"/> to diagnose compilation exceptions.
    /// </exception>
    /// <exception cref="ParseException">
    /// Thrown when the shader definition tokenizer fails to parse the shader definition file.
    /// </exception>
    /// <returns></returns>
    public Shader CompileShader(string sourceText)
    {
        ParsedShader parsed = ParsedShader.Parse(sourceText);
        return null;
    }
}
