using System;

using Silk.NET.SDL;

using Prowl.Graphite;
using Prowl.Graphite.OpenGL;
using Prowl.Vector;
using System.Collections.Generic;


public unsafe class Program
{
    static Sdl sdl;
    static Window* window;
    static SdlContext context;

    const string ShaderSource = """


""";

    static GraphicsDevice device;

    static Shader shader;
    static Mesh mesh;
    static Material material;
    static CommandBuffer buffer;
    static RenderTexture target;


    public static void SetupWindow()
    {
        sdl = Sdl.GetApi();
        window = sdl.CreateWindow("My Window", 0, 0, 500, 500, (uint)WindowFlags.Opengl);
        context = new SdlContext(sdl, window);
        context.Create();
        context.MakeCurrent();
    }


    public static void Main()
    {
        SetupWindow();

        device = new GLGraphicsDevice(context);

        if (!ShaderCompiler.CompileShader(ShaderSource, out shader))
            Console.WriteLine("Failed to compile shader");

        mesh = Mesh.Create();

        mesh.SetVertices(new List<Vector3>());
        mesh.SetIndices(new List<int>());

        material = Material.Create(shader);
        target = RenderTexture.Create(1960, 1080, RenderTextureFormat.RGBA32);

        buffer = CommandBuffer.Create("Main Buffer");

        while (true)
        {
            Console.WriteLine("Polling");

            Event sdlEvent = default;
            while (sdl.PollEvent(ref sdlEvent) != 0)
            {
                if (sdlEvent.Type == (uint)EventType.Quit)
                    return;
            }

            buffer.Clear();

            buffer.SetRenderTarget(target);
            buffer.ClearRenderTarget(Vector4.one, 1.0, 0);

            // material.SetVector("Color", new Vector3(0.5, 0.5, 1.0));
            // material.SetMatrix("MVP", Matrix4x4.Identity);

            buffer.SetMaterial(material, 0);

            buffer.DrawMesh(mesh, material);

            device.SubmitCommands(buffer);

            while (!device.IsIdle)
                System.Threading.Thread.Sleep(1);

            Console.WriteLine(device.IsIdle);

            device.SwapBuffers();
        }
    }
}
