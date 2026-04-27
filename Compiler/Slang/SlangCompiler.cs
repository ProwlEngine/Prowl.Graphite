using System;
using System.IO;
using System.Linq;

using Prowl.Slang;


namespace Prowl.Graphite.Compiler.Slang;


// Will use slang so no need for abstraction
public class SlangCompiler
{
    static TargetDescription[] SlangTargets =
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
    /// <param name="moduleName">The module filename to load</param>
    /// <param name="entryPointNames">The entrypoints to use</param>
    /// <param name="searchPaths">The provided search paths that the compiler will use</param>
    /// <exception cref="CompilationException">
    /// Thrown when the Slang shader compiler fails to compile shader code.
    /// View <see cref="CompilationException.Diagnostics"/> to diagnose compilation exceptions.
    /// </exception>
    /// <returns></returns>
    public void CompileShader(string moduleName, string[] entryPointNames, DirectoryInfo[] searchPaths)
    {
        SessionDescription sessionDesc = new()
        {
            Targets = SlangTargets,
            SearchPaths = [.. searchPaths.Select(x => x.FullName)],
            FileProvider = new FileProvider()
        };

        Session session = GlobalSession.CreateSession(sessionDesc);

        Module module = session.LoadModule(moduleName, out DiagnosticInfo diagnostics);

        EntryPoint[] entryPoints = new EntryPoint[entryPointNames.Length];

        for (int i = 0; i < module.GetDefinedEntryPointCount(); i++)
        {
            EntryPoint entryPoint = module.GetDefinedEntryPoint(i);
            string entryPointName = entryPoint.GetFunctionReflection().Name;

            for (int j = 0; j < entryPointNames.Length; j++)
            {
                if (entryPointName.Equals(entryPointNames[i], StringComparison.Ordinal))
                    entryPoints[j] = module.GetDefinedEntryPoint(i);
            }
        }

        ComponentType program = session.CreateCompositeComponentType([module, .. entryPoints], out diagnostics);

        Memory<byte> compiledCode = program.GetEntryPointCode(0, 0, out diagnostics);

        ShaderReflection reflection = program.GetLayout(1, out diagnostics);

        string json = reflection.ToJson();

        Console.WriteLine(json);
    }
}
