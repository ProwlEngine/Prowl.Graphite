using System.Runtime.InteropServices;
using Prowl.Graphite;
using Prowl.Vector;
using Silk.NET.Input;
using Silk.NET.Windowing;

using GL = Silk.NET.OpenGL.GL;
using GBuffer = Prowl.Graphite.Buffer;

namespace IndexedQuad;

/// <summary>
/// Sample 02: Indexed Quad
/// Demonstrates indexed drawing using an index buffer to render a quad from 4 vertices.
/// </summary>
class Program
{
    private static IWindow? _window;
    private static GL? _gl;
    private static GraphiteDevice? _device;

    // Resources
    private static GBuffer? _vertexBuffer;
    private static GBuffer? _indexBuffer;
    private static ShaderModule? _vertexShader;
    private static ShaderModule? _fragmentShader;
    private static PipelineState? _pipeline;

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(800, 600);
        options.Title = "02 - Indexed Quad";
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
        Console.WriteLine($"Backend: {_device.BackendName}");

        var input = _window.CreateInput();
        foreach (var keyboard in input.Keyboards)
        {
            keyboard.KeyDown += (kb, key, code) =>
            {
                if (key == Key.Escape)
                    _window.Close();
            };
        }

        CreateResources();
    }

    private static void CreateResources()
    {
        // Quad vertices: position (3) + color (3)
        // Only 4 vertices needed for a quad when using indexed drawing
        float[] vertices =
        [
            // Position          // Color
            -0.5f,  0.5f, 0.0f,  1.0f, 0.0f, 0.0f,  // Top-left - Red
             0.5f,  0.5f, 0.0f,  0.0f, 1.0f, 0.0f,  // Top-right - Green
             0.5f, -0.5f, 0.0f,  0.0f, 0.0f, 1.0f,  // Bottom-right - Blue
            -0.5f, -0.5f, 0.0f,  1.0f, 1.0f, 0.0f,  // Bottom-left - Yellow
        ];

        // Index buffer: two triangles forming a quad (clockwise winding - Unity convention)
        ushort[] indices =
        [
            0, 1, 2,  // First triangle
            0, 2, 3,  // Second triangle
        ];

        // Create vertex buffer
        _vertexBuffer = _device!.CreateBuffer<byte>(
            BufferUsage.Vertex | BufferUsage.CopyDestination,
            MemoryMarshal.AsBytes<float>(vertices));

        // Create index buffer
        _indexBuffer = _device.CreateBuffer<byte>(
            BufferUsage.Index | BufferUsage.CopyDestination,
            MemoryMarshal.AsBytes<ushort>(indices));

        // Shaders
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

        // Create pipeline
        _pipeline = _device.CreatePipelineState(new PipelineStateDescriptor
        {
            VertexShader = _vertexShader,
            FragmentShader = _fragmentShader,
            VertexLayout = new VertexLayoutDescriptor(
                new VertexBufferLayout(24, // 6 floats * 4 bytes
                    new VertexAttribute(0, VertexFormat.Float3, 0),  // Position
                    new VertexAttribute(1, VertexFormat.Float3, 12)  // Color
                )
            ),
            Topology = PrimitiveTopology.TriangleList,
            RasterizerState = RasterizerStateDescriptor.Default,
            DepthStencilState = DepthStencilStateDescriptor.NoDepth,
            BlendState = BlendStateDescriptor.Opaque,
            RenderPassLayout = RenderPassLayout.SingleColor(TextureFormat.RGBA8Unorm),
            BindGroupLayouts = [],
        });
    }

    private static void OnRender(double deltaTime)
    {
        if (_device == null || _pipeline == null) return;

        var swapchainTexture = _device.GetSwapchainTexture();

        using var commandList = _device.CreateCommandList();
        commandList.Begin();

        var colorAttachment = RenderPassColorAttachment.Clear(swapchainTexture, new Float4(0.1f, 0.1f, 0.2f, 1.0f));
        commandList.BeginRenderPass(RenderPassDescriptor.SingleColor(colorAttachment));

        commandList.SetViewport(0, 0, _device.SwapchainWidth, _device.SwapchainHeight);

        commandList.SetPipeline(_pipeline);
        commandList.SetVertexBuffer(0, _vertexBuffer!);
        commandList.SetIndexBuffer(_indexBuffer!, IndexFormat.Uint16);

        // Draw indexed: 6 indices (2 triangles)
        commandList.DrawIndexed(6);

        commandList.EndRenderPass();
        commandList.End();

        _device.SubmitCommands(commandList);
    }

    private static void OnResize(Silk.NET.Maths.Vector2D<int> size)
    {
        _device?.ResizeSwapchain((uint)size.X, (uint)size.Y);
    }

    private static void OnClose()
    {
        _pipeline?.Dispose();
        _fragmentShader?.Dispose();
        _vertexShader?.Dispose();
        _indexBuffer?.Dispose();
        _vertexBuffer?.Dispose();
        _device?.Dispose();
    }
}
