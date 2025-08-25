using System;

using Silk.NET.SDL;

using Prowl.Graphite;
using Prowl.Graphite.OpenGL;
using Prowl.Vector;
using System.Collections.Generic;
using Silk.NET.OpenGL;
using System.Linq;


public unsafe class Program
{
    static Sdl sdl;
    static Window* window;
    static SdlContext context;

    const string ShaderSource = """


""";

    static GraphicsDevice device;
    static ShaderCompiler compiler;

    static Prowl.Graphite.Shader shader;
    static Mesh mesh;
    static Material material;
    static CommandBuffer buffer;
    static RenderTexture target;

    static GraphicsBuffer dataBuffer;


    public static void SetupWindow()
    {
        sdl = Sdl.GetApi();
        window = sdl.CreateWindow("My Window", 0, 0, 500, 500, (uint)WindowFlags.Opengl);
        context = new SdlContext(sdl, window);
    }


    public static void Main()
    {
        SetupWindow();

        device = new GLGraphicsDevice(() =>
        {
            context.Create();
            return context;
        });

        compiler = new ShaderCompiler();

        if (!compiler.CompileShader(ShaderSource, out shader))
            Console.WriteLine("Failed to compile shader");

        mesh = Mesh.Create();

        mesh.SetVertices(new List<Float3>());
        mesh.SetIndices(new List<int>());

        material = Material.Create(shader);
        target = RenderTexture.Create(1960, 1080, RenderTextureFormat.RGBA32);
        dataBuffer = GraphicsBuffer.Create(new() { Usage = BufferUsage.None, Count = 5, Stride = 1, Target = BufferTarget.Vertex });

        buffer = CommandBuffer.Create("Main Buffer");

        while (Poll())
        {
            buffer.Clear();

            buffer.SetRenderTarget(null);
            buffer.ClearRenderTarget((Byte4)(Colors.PowderBlue * 255), 1.0, 0);

            // material.SetVector("Color", new Vector3(0.5, 0.5, 1.0));
            // material.SetMatrix("MVP", Matrix4x4.Identity);

            buffer.SetMaterial(material, 0);

            buffer.DrawMesh(mesh, material);

            device.SubmitCommands(buffer);

            device.SwapBuffers();

        }

        Console.WriteLine("Quit Game");
    }


    private static bool Poll()
    {
        Event sdlEvent = default;
        while (sdl.PollEvent(ref sdlEvent) != 0)
        {
            if (sdlEvent.Type == (uint)EventType.Quit)
            {
                sdl.Quit();
                return false;
            }
        }

        return true;
    }
}
