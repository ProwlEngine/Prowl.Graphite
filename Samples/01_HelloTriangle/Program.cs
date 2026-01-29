using System.Runtime.InteropServices;
using Prowl.Graphite;
using Prowl.Vector;
using Silk.NET.Input;
using Silk.NET.Windowing;

using GL = Silk.NET.OpenGL.GL;
using GBuffer = Prowl.Graphite.Buffer;

namespace HelloTriangle;

class Program
{
    private static IWindow? _window;
    private static GL? _gl;
    private static GraphiteDevice? _device;

    private static GBuffer? _vertexBuffer;
    private static ShaderModule? _vertexShader, _fragmentShader;
    private static PipelineState? _pipeline;

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(800, 600);
        options.Title = "01 - Hello Triangle";
        options.VSync = true;
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(4, 3));

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Closing += OnClose;
        _window.Resize += OnResize;
        _window.Run();
    }

    private static void OnLoad()
    {
        _gl = GL.GetApi(_window!);
        _device = GraphiteDevice.CreateOpenGL(_gl);
        _device.Initialize(GraphiteDeviceOptions.Debug);
        _device.ResizeSwapchain((uint)_window!.Size.X, (uint)_window.Size.Y);

        Console.WriteLine($"Graphics Device: {_device.Capabilities.DeviceName}");

        var input = _window.CreateInput();
        foreach (var keyboard in input.Keyboards)
            keyboard.KeyDown += (kb, key, code) => { if (key == Key.Escape) _window.Close(); };

        CreateResources();
    }

    private static void CreateResources()
    {
        // Triangle vertices: position (3) + color (3), CW winding
        float[] vertices =
        [
             0.0f,  0.5f, 0.0f,  1.0f, 0.0f, 0.0f,  // Top - Red
             0.5f, -0.5f, 0.0f,  0.0f, 0.0f, 1.0f,  // Bottom Right - Blue
            -0.5f, -0.5f, 0.0f,  0.0f, 1.0f, 0.0f,  // Bottom Left - Green
        ];

        _vertexBuffer = _device!.CreateBuffer<byte>(BufferUsage.Vertex | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<float>(vertices));

        const string vertexShaderSource = """
            #version 430 core
            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec3 aColor;
            out vec3 vColor;
            void main() {
                gl_Position = vec4(aPosition, 1.0);
                vColor = aColor;
            }
            """;

        const string fragmentShaderSource = """
            #version 430 core
            in vec3 vColor;
            out vec4 FragColor;
            void main() {
                FragColor = vec4(vColor, 1.0);
            }
            """;

        _vertexShader = _device.CreateShaderModule(ShaderModuleDescriptor.VertexGLSL(vertexShaderSource));
        _fragmentShader = _device.CreateShaderModule(ShaderModuleDescriptor.FragmentGLSL(fragmentShaderSource));

        _pipeline = _device.CreatePipelineState(new PipelineStateDescriptor
        {
            VertexShader = _vertexShader,
            FragmentShader = _fragmentShader,
            VertexLayout = new VertexLayoutDescriptor(
                new VertexBufferLayout(24,
                    new VertexAttribute(0, VertexFormat.Float3, 0),
                    new VertexAttribute(1, VertexFormat.Float3, 12)
                )
            ),
            DepthStencilState = DepthStencilStateDescriptor.NoDepth,
            RenderPassLayout = RenderPassLayout.SingleColor(TextureFormat.RGBA8Unorm, null),
        });
    }

    private static void OnRender(double deltaTime)
    {
        if (_device == null || _pipeline == null) return;

        using var cmd = _device.CreateCommandList();
        cmd.Begin();
        cmd.BeginRenderPass(RenderPassDescriptor.SingleColor(
            RenderPassColorAttachment.Clear(_device.GetSwapchainTexture(), new Float4(0.1f, 0.1f, 0.2f, 1.0f))));
        cmd.SetViewport(0, 0, _device.SwapchainWidth, _device.SwapchainHeight);
        cmd.SetPipeline(_pipeline);
        cmd.SetVertexBuffer(0, _vertexBuffer!);
        cmd.Draw(3);
        cmd.EndRenderPass();
        cmd.End();
        _device.SubmitCommands(cmd);
    }

    private static void OnResize(Silk.NET.Maths.Vector2D<int> size) => _device?.ResizeSwapchain((uint)size.X, (uint)size.Y);

    private static void OnClose()
    {
        _pipeline?.Dispose();
        _fragmentShader?.Dispose();
        _vertexShader?.Dispose();
        _vertexBuffer?.Dispose();
        _device?.Dispose();
    }
}
