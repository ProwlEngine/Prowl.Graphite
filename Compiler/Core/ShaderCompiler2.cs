// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

/*

using Glslang.NET;

using Prowl.Runtime;
using Prowl.Runtime.Rendering;

using Veldrid;

using Shader = Glslang.NET.Shader;

namespace Prowl.Editor;

public static partial class ShaderCompiler
{
    private static string AddDefines(string sourceCode, KeywordState keywords)
    {
        StringBuilder builder = new StringBuilder();

        foreach (var pair in keywords.KeyValuePairs)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                continue;

            builder.Append("#define ");
            builder.Append(pair.Key);
            builder.Append(" ");
            builder.AppendLine(pair.Value);
        }

        builder.Append(sourceCode);

        return builder.ToString();
    }


    public static ShaderDescription[]? Compile(ShaderCreationArgs args, KeywordState keywords, FileIncluder includer, List<CompilationMessage> messages)
    {
        ShaderDescription[] outputs = new ShaderDescription[args.entryPoints.Length];

        using Glslang.NET.Program program = new Glslang.NET.Program();

        CompilationInput input = new CompilationInput()
        {
            language = SourceType.HLSL,
            stage = ShaderStage.Fragment,
            client = ClientType.Vulkan,
            clientVersion = TargetClientVersion.Vulkan_1_1,
            targetLanguage = TargetLanguage.SPV,
            targetLanguageVersion = TargetLanguageVersion.SPV_1_3,
            defaultVersion = 500,
            code = AddDefines(args.sourceCode, keywords),
            hlslFunctionality1 = true,
            defaultProfile = ShaderProfile.None,
            forceDefaultVersionAndProfile = false,
            forwardCompatible = false,
            messages = MessageType.Enhanced | MessageType.ReadHlsl | MessageType.DisplayErrorColumn | MessageType.CascadingErrors,
        };

        string fileName = Path.GetFileName(includer.SourceFile);

        foreach (EntryPoint entrypoint in args.entryPoints)
        {
            input.sourceEntrypoint = entrypoint.Name;
            input.stage = StageToType(entrypoint.Stage);
            input.fileIncluder = includer.GetIncluder(messages);

            Shader shader = new Shader(input);

            shader.SetOptions(
                ShaderOptions.AutoMapBindings |
                ShaderOptions.AutoMapLocations |
                ShaderOptions.MapUnusedUniforms |
                ShaderOptions.UseHLSLIOMapper
            );

            bool preprocessed = shader.Preprocess();
            bool parsed = shader.Parse();

            CheckMessages(shader.GetDebugLog(), keywords.Count, includer, messages);
            CheckMessages(shader.GetInfoLog(), keywords.Count, includer, messages);

            if (!preprocessed || !parsed)
                return null;

            program.AddShader(shader);
        }

        bool linked = program.Link(MessageType.VulkanRules | MessageType.SpvRules | input.messages ?? MessageType.Default);
        bool mapIO = program.MapIO();

        CheckMessages(program.GetDebugLog(), keywords.Count, includer, messages);
        CheckMessages(program.GetInfoLog(), keywords.Count, includer, messages);

        if (!linked || !mapIO)
            return null;

        for (int i = 0; i < args.entryPoints.Length; i++)
        {
            EntryPoint entryPoint = args.entryPoints[i];

            bool generatedSPIRV = program.GenerateSPIRV(out uint[] SPIRVWords, StageToType(entryPoint.Stage));

            CheckMessages(program.GetSPIRVMessages(), keywords.Count, includer, messages);

            if (!generatedSPIRV || SPIRVWords == null || SPIRVWords.Length == 0)
                return null;

            outputs[i].EntryPoint = "main";
            outputs[i].Stage = entryPoint.Stage;
            outputs[i].ShaderBytes = GetBytes(SPIRVWords);
        }

        return outputs;
    }
}

*/
