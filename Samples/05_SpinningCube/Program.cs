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
/// A colored cube spins on multiple axes.
/// </summary>
class Program
{
    private static IWindow? _window;
    private static GL? _gl;
    private static GraphiteDevice? _device;

    // Resources
    private static GBuffer? _vertexBuffer;
    private static GBuffer? _indexBuffer;
    private static GBuffer? _uniformBuffer;
    private static Texture? _depthTexture;
    private static ShaderModule? _vertexShader;
    private static ShaderModule? _fragmentShader;
    private static PipelineState? _pipeline;
    private static BindGroupLayout? _bindGroupLayout;
    private static BindGroup? _bindGroup;

    private static float _time;

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
        // Cube vertices: position (3) + color (3)
        // Each face has a different color
        float[] vertices =
        [
            // Front face (Red)
            -0.5f, -0.5f,  0.5f,  1.0f, 0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,  1.0f, 0.0f, 0.0f,

            // Back face (Green)
             0.5f, -0.5f, -0.5f,  0.0f, 1.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  0.0f, 1.0f, 0.0f,

            // Top face (Blue)
            -0.5f,  0.5f,  0.5f,  0.0f, 0.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  0.0f, 0.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  0.0f, 0.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 0.0f, 1.0f,

            // Bottom face (Yellow)
            -0.5f, -0.5f, -0.5f,  1.0f, 1.0f, 0.0f,
             0.5f, -0.5f, -0.5f,  1.0f, 1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 1.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,  1.0f, 1.0f, 0.0f,

            // Right face (Magenta)
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  1.0f, 0.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 0.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f, 1.0f,

            // Left face (Cyan)
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f, 1.0f,
        ];

        // Indices for the cube (6 faces * 2 triangles * 3 vertices)
        ushort[] indices =
        [
            // Front
            0, 1, 2, 0, 2, 3,
            // Back
            4, 5, 6, 4, 6, 7,
            // Top
            8, 9, 10, 8, 10, 11,
            // Bottom
            12, 13, 14, 12, 14, 15,
            // Right
            16, 17, 18, 16, 18, 19,
            // Left
            20, 21, 22, 20, 22, 23,
        ];

        _vertexBuffer = _device!.CreateBuffer<byte>(
            BufferUsage.Vertex | BufferUsage.CopyDestination,
            MemoryMarshal.AsBytes<float>(vertices));

        _indexBuffer = _device.CreateBuffer<byte>(
            BufferUsage.Index | BufferUsage.CopyDestination,
            MemoryMarshal.AsBytes<ushort>(indices));

        _uniformBuffer = _device.CreateBuffer(new BufferDescriptor
        {
            SizeInBytes = (uint)Marshal.SizeOf<UniformData>(),
            Usage = BufferUsage.Uniform | BufferUsage.CopyDestination,
            MemoryAccess = MemoryAccess.CpuToGpu,
            DebugName = "MVPUniformBuffer"
        });

        // Create depth texture
        CreateDepthTexture();

        // Shaders
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

        // Bind group layout
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

        // Pipeline with depth testing enabled
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
            Topology = PrimitiveTopology.TriangleList,
            RasterizerState = RasterizerStateDescriptor.Default,
            DepthStencilState = DepthStencilStateDescriptor.Default, // Enable depth testing
            BlendState = BlendStateDescriptor.Opaque,
            RenderPassLayout = new RenderPassLayout
            {
                ColorFormats = [TextureFormat.RGBA8Unorm],
                DepthStencilFormat = TextureFormat.Depth24Plus,
            },
            BindGroupLayouts = [_bindGroupLayout],
        });
    }

    private static void CreateDepthTexture()
    {
        _depthTexture?.Dispose();
        _depthTexture = _device!.CreateTexture(new TextureDescriptor
        {
            Dimension = TextureDimension.Texture2D,
            Width = _device.SwapchainWidth,
            Height = _device.SwapchainHeight,
            Depth = 1,
            MipLevels = 1,
            ArrayLayers = 1,
            Format = TextureFormat.Depth24Plus,
            Usage = TextureUsage.RenderTarget,
            SampleCount = SampleCount.Count1,
            DebugName = "DepthTexture"
        });
    }

    private static void OnRender(double deltaTime)
    {
        if (_device == null || _pipeline == null) return;

        _time += (float)deltaTime;

        // Update uniforms
        float aspect = (float)_device.SwapchainWidth / _device.SwapchainHeight;

        var uniformData = new UniformData
        {
            Model = Float4x4.RotateX(_time * 0.5f) * Float4x4.RotateY(_time),
            View = Float4x4.CreateLookAt(
                new Float3(0, 0, 3),   // Camera position
                new Float3(0, 0, 0),   // Look at origin
                new Float3(0, 1, 0)),  // Up vector
            Projection = Float4x4.CreatePerspectiveFov(
                MathF.PI / 4,  // 45 degrees FOV
                aspect,
                0.1f,          // Near plane
                100.0f),       // Far plane
        };

        _device.UpdateBuffer(_uniformBuffer!, 0, MemoryMarshal.CreateReadOnlySpan(ref uniformData, 1));

        var swapchainTexture = _device.GetSwapchainTexture();

        using var commandList = _device.CreateCommandList();
        commandList.Begin();

        // Render pass with color and depth attachments
        var colorAttachment = RenderPassColorAttachment.Clear(swapchainTexture, new Float4(0.1f, 0.1f, 0.15f, 1.0f));
        var depthAttachment = RenderPassDepthStencilAttachment.Clear(_depthTexture!, 1.0f);

        commandList.BeginRenderPass(new RenderPassDescriptor
        {
            ColorAttachments = [colorAttachment],
            DepthStencilAttachment = depthAttachment,
        });

        commandList.SetViewport(0, 0, _device.SwapchainWidth, _device.SwapchainHeight);

        commandList.SetPipeline(_pipeline);
        commandList.SetBindGroup(0, _bindGroup!);
        commandList.SetVertexBuffer(0, _vertexBuffer!);
        commandList.SetIndexBuffer(_indexBuffer!, IndexFormat.Uint16);

        commandList.DrawIndexed(36); // 6 faces * 6 indices

        commandList.EndRenderPass();
        commandList.End();

        _device.SubmitCommands(commandList);
    }

    private static void OnResize(Silk.NET.Maths.Vector2D<int> size)
    {
        if (_device == null || size.X == 0 || size.Y == 0) return;

        _device.ResizeSwapchain((uint)size.X, (uint)size.Y);
        CreateDepthTexture(); // Recreate depth texture at new size
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
