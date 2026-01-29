using System.Runtime.InteropServices;
using Prowl.Graphite;
using Prowl.Vector;
using Silk.NET.Input;
using Silk.NET.Windowing;

using GL = Silk.NET.OpenGL.GL;
using GBuffer = Prowl.Graphite.Buffer;

namespace UniformBuffers;

/// <summary>
/// Sample 04: Uniform Buffers
/// Demonstrates using uniform buffers to pass MVP matrices to shaders.
/// The triangle rotates over time using model matrix transformation.
/// </summary>
class Program
{
    private static IWindow? _window;
    private static GL? _gl;
    private static GraphiteDevice? _device;

    // Resources
    private static GBuffer? _vertexBuffer;
    private static GBuffer? _uniformBuffer;
    private static ShaderModule? _vertexShader;
    private static ShaderModule? _fragmentShader;
    private static PipelineState? _pipeline;
    private static BindGroupLayout? _bindGroupLayout;
    private static BindGroup? _bindGroup;

    private static float _time;

    // Uniform data structure - must match shader layout
    [StructLayout(LayoutKind.Sequential)]
    struct UniformData
    {
        public Float4x4 Model;
        public Float4x4 View;
        public Float4x4 Projection;
    }

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(800, 600);
        options.Title = "04 - Uniform Buffers";
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
        // Triangle vertices: position (3) + color (3)
        // CW winding order (Unity convention)
        float[] vertices =
        [
            // Position          // Color
             0.0f,  0.5f, 0.0f,  1.0f, 0.0f, 0.0f,  // Top - Red
             0.5f, -0.5f, 0.0f,  0.0f, 0.0f, 1.0f,  // Bottom Right - Blue
            -0.5f, -0.5f, 0.0f,  0.0f, 1.0f, 0.0f,  // Bottom Left - Green
        ];

        _vertexBuffer = _device!.CreateBuffer<byte>(
            BufferUsage.Vertex | BufferUsage.CopyDestination,
            MemoryMarshal.AsBytes<float>(vertices));

        // Create uniform buffer for MVP matrices
        _uniformBuffer = _device.CreateBuffer(new BufferDescriptor
        {
            SizeInBytes = (uint)Marshal.SizeOf<UniformData>(),
            Usage = BufferUsage.Uniform | BufferUsage.CopyDestination,
            MemoryAccess = MemoryAccess.CpuToGpu,
            DebugName = "MVPUniformBuffer"
        });

        // Shaders with uniform buffer
        const string vertexShaderSource = """
            #version 430 core

            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec3 aColor;

            layout(std140, binding = 0) uniform Matrices {
                mat4 model;
                mat4 view;
                mat4 projection;
            };

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

        // Create bind group layout for uniform buffer
        _bindGroupLayout = _device.CreateBindGroupLayout(new BindGroupLayoutDescriptor
        {
            Entries =
            [
                new BindGroupLayoutEntry
                {
                    Binding = 0,
                    Visibility = ShaderStage.Vertex,
                    Type = BindingType.UniformBuffer,
                }
            ]
        });

        // Create bind group
        _bindGroup = _device.CreateBindGroup(new BindGroupDescriptor
        {
            Layout = _bindGroupLayout,
            Entries =
            [
                new BindGroupEntry
                {
                    Binding = 0,
                    Buffer = new BufferBinding(_uniformBuffer, 0, (uint)Marshal.SizeOf<UniformData>()),
                }
            ]
        });

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
            BindGroupLayouts = [_bindGroupLayout],
        });
    }

    private static void OnRender(double deltaTime)
    {
        if (_device == null || _pipeline == null) return;

        _time += (float)deltaTime;

        // Update uniform buffer with rotating model matrix
        float aspect = (float)_device.SwapchainWidth / _device.SwapchainHeight;

        var uniformData = new UniformData
        {
            Model = Float4x4.RotateZ(_time),
            View = Float4x4.Identity,
            Projection = Float4x4.CreateOrtho(2.0f * aspect, 2.0f, -1.0f, 1.0f),
        };

        // Upload uniform data
        _device.UpdateBuffer(_uniformBuffer!, 0, MemoryMarshal.CreateReadOnlySpan(ref uniformData, 1));

        var swapchainTexture = _device.GetSwapchainTexture();

        using var commandList = _device.CreateCommandList();
        commandList.Begin();

        var colorAttachment = RenderPassColorAttachment.Clear(swapchainTexture, new Float4(0.1f, 0.15f, 0.2f, 1.0f));
        commandList.BeginRenderPass(RenderPassDescriptor.SingleColor(colorAttachment));

        commandList.SetViewport(0, 0, _device.SwapchainWidth, _device.SwapchainHeight);

        commandList.SetPipeline(_pipeline);
        commandList.SetBindGroup(0, _bindGroup!);
        commandList.SetVertexBuffer(0, _vertexBuffer!);

        commandList.Draw(3);

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
        _bindGroup?.Dispose();
        _bindGroupLayout?.Dispose();
        _fragmentShader?.Dispose();
        _vertexShader?.Dispose();
        _uniformBuffer?.Dispose();
        _vertexBuffer?.Dispose();
        _device?.Dispose();
    }
}
