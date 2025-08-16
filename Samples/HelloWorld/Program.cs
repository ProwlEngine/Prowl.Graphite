using System;

using Silk.NET.SDL;

using Prowl.Graphite;
using Prowl.Graphite.OpenGL;
using Prowl.Vector;


public unsafe class Program
{
    Sdl sdl;
    Window* window;
    SdlContext context;

    const string ShaderSource = """


""";

    GraphicsDevice device;

    Shader shader;
    Mesh mesh;
    Material material;
    CommandBuffer buffer;
    RenderTexture target;


    public void SetupWindow()
    {
        sdl = Sdl.GetApi();
        window = sdl.CreateWindow("My Window", 0, 0, 500, 500, (uint)WindowFlags.Opengl);
        context = new SdlContext(sdl, window);
    }


    public void Main()
    {
        SetupWindow();

        device = new GLGraphicsDevice(context);

        if (!ShaderCompiler.CompileShader(ShaderSource, out shader))
            return;

        mesh = new Mesh();

        mesh.SetVertices(new List<Vector3>());
        mesh.SetIndices(new List<int>());

        material = new Material(shader);
        target = new RenderTexture(1960, 1080, RenderTextureFormat.RGBA32);

        buffer = new CommandBuffer("Main Buffer");

        while (true)
        {
            Event sdlEvent = default;
            while (sdl.PollEvent(ref sdlEvent) != 0)
            {
                if (sdlEvent.Type == (uint)EventType.Quit)
                    return;
            }

            buffer.Clear();

            buffer.SetRenderTarget(target);
            buffer.ClearRenderTarget(Vector4.zero, 1.0, 0);

            material.SetVector("Color", new Vector3(0.5, 0.5, 1.0));
            material.SetMatrix("MVP", Matrix4x4.Identity);

            buffer.SetMaterial(material, 0);

            buffer.DrawMesh(mesh, material);

            device.SubmitCommands(buffer);

            device.SwapBuffers();
        }
    }
}
