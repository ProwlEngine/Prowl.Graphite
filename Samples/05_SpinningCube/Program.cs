using System.Runtime.InteropServices;
using Prowl.Graphite;
using Prowl.Vector;
using Silk.NET.Input;
using Silk.NET.Windowing;

using GL = Silk.NET.OpenGL.GL;
using GBuffer = Prowl.Graphite.Buffer;

namespace SpinningCube;

/// <summary>
/// Sample 05: Spinning Cube
/// Demonstrates 3D rendering with depth testing and a perspective camera.
/// </summary>
class Program
{
    private static IWindow? _window;
    private static GL? _gl;
    private static GraphiteDevice? _device;

    private static GBuffer? _vertexBuffer, _indexBuffer, _uniformBuffer;
    private static Texture? _depthTexture;
    private static ShaderModule? _vertexShader, _fragmentShader;
    private static PipelineState? _pipeline;
    private static BindGroupLayout? _bindGroupLayout;
    private static BindGroup? _bindGroup;

    private static float _time;

    [StructLayout(LayoutKind.Sequential)]
    struct UniformData
    {
        public Float4x4 Model, View, Projection;
    }

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(800, 600);
        options.Title = "05 - Spinning Cube";
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
        // Cube vertices: position (3) + color (3), CW winding
        float[] vertices =
        [
            // Front (Red)
            -0.5f, -0.5f,  0.5f,  1, 0, 0,   0.5f, -0.5f,  0.5f,  1, 0, 0,
             0.5f,  0.5f,  0.5f,  1, 0, 0,  -0.5f,  0.5f,  0.5f,  1, 0, 0,
            // Back (Green)
             0.5f, -0.5f, -0.5f,  0, 1, 0,  -0.5f, -0.5f, -0.5f,  0, 1, 0,
            -0.5f,  0.5f, -0.5f,  0, 1, 0,   0.5f,  0.5f, -0.5f,  0, 1, 0,
            // Top (Blue)
            -0.5f,  0.5f,  0.5f,  0, 0, 1,   0.5f,  0.5f,  0.5f,  0, 0, 1,
             0.5f,  0.5f, -0.5f,  0, 0, 1,  -0.5f,  0.5f, -0.5f,  0, 0, 1,
            // Bottom (Yellow)
            -0.5f, -0.5f, -0.5f,  1, 1, 0,   0.5f, -0.5f, -0.5f,  1, 1, 0,
             0.5f, -0.5f,  0.5f,  1, 1, 0,  -0.5f, -0.5f,  0.5f,  1, 1, 0,
            // Right (Magenta)
             0.5f, -0.5f,  0.5f,  1, 0, 1,   0.5f, -0.5f, -0.5f,  1, 0, 1,
             0.5f,  0.5f, -0.5f,  1, 0, 1,   0.5f,  0.5f,  0.5f,  1, 0, 1,
            // Left (Cyan)
            -0.5f, -0.5f, -0.5f,  0, 1, 1,  -0.5f, -0.5f,  0.5f,  0, 1, 1,
            -0.5f,  0.5f,  0.5f,  0, 1, 1,  -0.5f,  0.5f, -0.5f,  0, 1, 1,
        ];

        ushort[] indices =
        [
            0, 1, 2, 0, 2, 3,       4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,    12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19, 20, 21, 22, 20, 22, 23,
        ];

        _vertexBuffer = _device!.CreateBuffer<byte>(BufferUsage.Vertex | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<float>(vertices));
        _indexBuffer = _device.CreateBuffer<byte>(BufferUsage.Index | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<ushort>(indices));
        _uniformBuffer = _device.CreateBuffer(BufferDescriptor.Uniform((uint)Marshal.SizeOf<UniformData>()));

        CreateDepthTexture();

        const string vertexShaderSource = """
            #version 430 core
            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec3 aColor;
            layout(std140, binding = 0) uniform Matrices { mat4 model, view, projection; };
            out vec3 vColor;
            void main() {
                gl_Position = projection * view * model * vec4(aPosition, 1.0);
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

        _bindGroupLayout = _device.CreateBindGroupLayout(new BindGroupLayoutDescriptor(
            BindGroupLayoutEntry.UniformBuffer(0, ShaderStage.Vertex)
        ));

        _bindGroup = _device.CreateBindGroup(new BindGroupDescriptor(_bindGroupLayout,
            BindGroupEntry.ForBuffer(0, _uniformBuffer)
        ));

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
            RenderPassLayout = RenderPassLayout.SingleColor(TextureFormat.RGBA8Unorm),
            BindGroupLayouts = [_bindGroupLayout],
        });
    }

    private static void CreateDepthTexture()
    {
        _depthTexture?.Dispose();
        _depthTexture = _device!.CreateTexture(TextureDescriptor.DepthStencil(_device.SwapchainWidth, _device.SwapchainHeight));
    }

    private static void OnRender(double deltaTime)
    {
        if (_device == null || _pipeline == null) return;

        _time += (float)deltaTime;
        float aspect = (float)_device.SwapchainWidth / _device.SwapchainHeight;

        var uniformData = new UniformData
        {
            Model = Float4x4.RotateX(_time * 0.5f) * Float4x4.RotateY(_time),
            View = Float4x4.CreateLookAt(new Float3(0, 0, 3), Float3.Zero, Float3.UnitY),
            Projection = Float4x4.CreatePerspectiveFov(MathF.PI / 4, aspect, 0.1f, 100.0f),
        };
        _device.UpdateBuffer(_uniformBuffer!, 0, MemoryMarshal.CreateReadOnlySpan(ref uniformData, 1));

        using var cmd = _device.CreateCommandList();
        cmd.Begin();
        cmd.BeginRenderPass(new RenderPassDescriptor
        {
            ColorAttachments = [RenderPassColorAttachment.Clear(_device.GetSwapchainTexture(), new Float4(0.1f, 0.1f, 0.15f, 1.0f))],
            DepthStencilAttachment = RenderPassDepthStencilAttachment.Clear(_depthTexture!),
        });
        cmd.SetViewport(0, 0, _device.SwapchainWidth, _device.SwapchainHeight);
        cmd.SetPipeline(_pipeline);
        cmd.SetBindGroup(0, _bindGroup!);
        cmd.SetVertexBuffer(0, _vertexBuffer!);
        cmd.SetIndexBuffer(_indexBuffer!, IndexFormat.Uint16);
        cmd.DrawIndexed(36);
        cmd.EndRenderPass();
        cmd.End();
        _device.SubmitCommands(cmd);
    }

    private static void OnResize(Silk.NET.Maths.Vector2D<int> size)
    {
        if (_device == null || size.X == 0 || size.Y == 0) return;
        _device.ResizeSwapchain((uint)size.X, (uint)size.Y);
        CreateDepthTexture();
    }

    private static void OnClose()
    {
        _pipeline?.Dispose();
        _bindGroup?.Dispose();
        _bindGroupLayout?.Dispose();
        _fragmentShader?.Dispose();
        _vertexShader?.Dispose();
        _depthTexture?.Dispose();
        _uniformBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _vertexBuffer?.Dispose();
        _device?.Dispose();
    }
}
