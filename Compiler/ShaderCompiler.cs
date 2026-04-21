using System;
using System.IO;
using System.Linq;

using Prowl.Slang;

using Superpower;


namespace Prowl.Graphite;


// Will use slang so no need for abstraction
public class ShaderCompiler
{
    public class FileProvider : IFileProvider
    {
        public Memory<byte>? LoadFile(string path)
        {
            if (!File.Exists(path))
                return null;

            return new Memory<byte>(File.ReadAllBytes(path));
        }
    }


    /// <summary>
    /// Compiles yo shi for ya
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="searchPaths"></param>
    /// <param name="result"></param>
    /// <exception cref="CompilationException">
    /// Thrown when the Slang shader compiler fails to compile shader code.
    /// View <see cref="CompilationException.Diagnostics"/> to diagnose compilation exceptions.
    /// </exception>
    /// <exception cref="ParseException">
    /// Thrown when the shader definition tokenizer fails to parse the shader definition file.
    /// </exception>
    /// <returns></returns>
    public Shader CompileShader(string moduleName, DirectoryInfo[] searchPaths)
    {
        return null;

        TargetDescription[] targetDesc =
        [
            new()
                {
                    Format = CompileTarget.Spirv,
                    Profile = GlobalSession.FindProfile("spirv_1_5")
                },

                new()
                {
                    Format = CompileTarget.Dxil,
                    Profile = GlobalSession.FindProfile("sm_6_6")
                },

                new()
                {
                    Format = CompileTarget.Glsl,
                    Profile = GlobalSession.FindProfile("glsl_450")
                },

                new()
                {
                    Format = CompileTarget.Metal,
                    Profile = GlobalSession.FindProfile("metal_2_0")
                },

                new()
                {
                    Format = CompileTarget.Wgsl,
                    Profile = GlobalSession.FindProfile("wgsl_1_0")
                }
        ];

        SessionDescription sessionDesc = new()
        {
            Targets = targetDesc,
            SearchPaths = [.. searchPaths.Select(x => x.FullName)],
            FileProvider = new FileProvider()
        };

        Session session = GlobalSession.CreateSession(sessionDesc);

        Module module = session.LoadModule("hello-world", out DiagnosticInfo diagnostics);

        EntryPoint[] entryPoints =
            [.. Enumerable.Range(0, module.GetDefinedEntryPointCount()).Select(x => module.GetDefinedEntryPoint(x))];

        ComponentType program = session.CreateCompositeComponentType([module, .. entryPoints], out diagnostics);

        Memory<byte> compiledCode = program.GetEntryPointCode(0, 0, out diagnostics);

        ShaderReflection reflection = program.GetLayout(1, out diagnostics);

        string json = reflection.ToJson();

        Console.WriteLine(json);
    }


    public bool CompileShader(Memory<byte> sourceBytes, out Shader result, FileInfo? filePath = null)
    {
        result = null;
        return false;
    }
}
