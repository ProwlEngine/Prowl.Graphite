using System;
using System.IO;
using System.Text.Json;

using Prowl.Graphite;


public class Program
{
    public static void Main()
    {
        string sourceShader = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Shader.shader"));

        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        ParsedShader parsed = ShaderParser.ParseShader(sourceShader);

        stopwatch.Stop();

        Console.WriteLine("Parsed in " + stopwatch.ElapsedMilliseconds + " ms");

        Console.WriteLine(JsonSerializer.Serialize(parsed, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IncludeFields = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        })
        // Json escaped line endings
        .Replace("\\n", "\n")
        .Replace("\\r", "\r"));
    }
}
