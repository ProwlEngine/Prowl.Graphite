using System.Runtime.InteropServices;
using Prowl.Graphite;
using Prowl.Vector;
using Silk.NET.Input;
using Silk.NET.Windowing;

using GL = Silk.NET.OpenGL.GL;
using GBuffer = Prowl.Graphite.Buffer;

namespace Instancing;

/// <summary>
/// Sample 07: Instancing
/// Demonstrates hardware instancing to draw 1000 cubes with a single draw call.
/// </summary>
class Program
{
    private static IWindow? _window;
    private static GL? _gl;
    private static GraphiteDevice? _device;

    private static GBuffer? _vertexBuffer, _indexBuffer, _instanceBuffer, _uniformBuffer;
    private static Texture? _depthTexture;
    private static ShaderModule? _vertexShader, _fragmentShader;
    private static PipelineState? _pipeline;
    private static BindGroupLayout? _bindGroupLayout;
    private static BindGroup? _bindGroup;

    private static float _time;
    private const int INSTANCE_COUNT = 1000, GRID_SIZE = 10;

    [StructLayout(LayoutKind.Sequential)]
    struct UniformData { public Float4x4 View, Projection; public float Time, _p0, _p1, _p2; }

    [StructLayout(LayoutKind.Sequential)]
    struct InstanceData { public Float3 Position; public float RotationSpeed; public Float3 Color; public float RotationOffset; }

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(1280, 720);
        options.Title = "07 - Instancing (1000 cubes)";
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
        Console.WriteLine($"Drawing {INSTANCE_COUNT} cubes with 1 draw call");

        var input = _window.CreateInput();
        foreach (var keyboard in input.Keyboards)
            keyboard.KeyDown += (kb, key, code) => { if (key == Key.Escape) _window.Close(); };

        CreateResources();
    }

    private static void CreateResources()
    {
        // Cube vertices: position (3) + normal (3), CW winding
        float[] vertices =
        [
            // Front
            -0.5f, -0.5f,  0.5f,  0, 0, 1,   0.5f, -0.5f,  0.5f,  0, 0, 1,
             0.5f,  0.5f,  0.5f,  0, 0, 1,  -0.5f,  0.5f,  0.5f,  0, 0, 1,
            // Back
             0.5f, -0.5f, -0.5f,  0, 0,-1,  -0.5f, -0.5f, -0.5f,  0, 0,-1,
            -0.5f,  0.5f, -0.5f,  0, 0,-1,   0.5f,  0.5f, -0.5f,  0, 0,-1,
            // Top
            -0.5f,  0.5f,  0.5f,  0, 1, 0,   0.5f,  0.5f,  0.5f,  0, 1, 0,
             0.5f,  0.5f, -0.5f,  0, 1, 0,  -0.5f,  0.5f, -0.5f,  0, 1, 0,
            // Bottom
            -0.5f, -0.5f, -0.5f,  0,-1, 0,   0.5f, -0.5f, -0.5f,  0,-1, 0,
             0.5f, -0.5f,  0.5f,  0,-1, 0,  -0.5f, -0.5f,  0.5f,  0,-1, 0,
            // Right
             0.5f, -0.5f,  0.5f,  1, 0, 0,   0.5f, -0.5f, -0.5f,  1, 0, 0,
             0.5f,  0.5f, -0.5f,  1, 0, 0,   0.5f,  0.5f,  0.5f,  1, 0, 0,
            // Left
            -0.5f, -0.5f, -0.5f, -1, 0, 0,  -0.5f, -0.5f,  0.5f, -1, 0, 0,
            -0.5f,  0.5f,  0.5f, -1, 0, 0,  -0.5f,  0.5f, -0.5f, -1, 0, 0,
        ];

        ushort[] indices =
        [
            0, 1, 2, 0, 2, 3,       4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,    12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19, 20, 21, 22, 20, 22, 23,
        ];

        _vertexBuffer = _device!.CreateBuffer<byte>(BufferUsage.Vertex | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<float>(vertices));
        _indexBuffer = _device.CreateBuffer<byte>(BufferUsage.Index | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<ushort>(indices));

        // Generate instance data
        var random = new Random(42);
        var instances = new InstanceData[INSTANCE_COUNT];
        float spacing = 2.5f, offset = (GRID_SIZE - 1) * spacing * 0.5f;

        for (int i = 0; i < INSTANCE_COUNT; i++)
        {
            int x = i % GRID_SIZE, y = (i / GRID_SIZE) % GRID_SIZE, z = i / (GRID_SIZE * GRID_SIZE);
            instances[i] = new InstanceData
            {
                Position = new Float3(x * spacing - offset + (float)(random.NextDouble() - 0.5) * 0.5f,
                                      y * spacing - offset + (float)(random.NextDouble() - 0.5) * 0.5f,
                                      z * spacing - offset + (float)(random.NextDouble() - 0.5) * 0.5f),
                RotationSpeed = 0.5f + (float)random.NextDouble() * 2.0f,
                Color = new Float3(0.3f + (float)random.NextDouble() * 0.7f, 0.3f + (float)random.NextDouble() * 0.7f, 0.3f + (float)random.NextDouble() * 0.7f),
                RotationOffset = (float)random.NextDouble() * MathF.PI * 2.0f,
            };
        }

        _instanceBuffer = _device.CreateBuffer<byte>(BufferUsage.Vertex | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<InstanceData>(instances));
        _uniformBuffer = _device.CreateBuffer(BufferDescriptor.Uniform((uint)Marshal.SizeOf<UniformData>()));

        CreateDepthTexture();

        const string vertexShaderSource = """
            #version 430 core
            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec3 aNormal;
            layout(location = 2) in vec3 iPosition;
            layout(location = 3) in float iRotationSpeed;
            layout(location = 4) in vec3 iColor;
            layout(location = 5) in float iRotationOffset;

            layout(std140, binding = 0) uniform Uniforms { mat4 view, projection; float time; };

            out vec3 vNormal, vColor, vWorldPos;

            mat3 rotateY(float a) { float c = cos(a), s = sin(a); return mat3(c, 0, s, 0, 1, 0, -s, 0, c); }
            mat3 rotateX(float a) { float c = cos(a), s = sin(a); return mat3(1, 0, 0, 0, c, -s, 0, s, c); }

            void main() {
                float angle = time * iRotationSpeed + iRotationOffset;
                mat3 rot = rotateY(angle) * rotateX(angle * 0.7);
                vec3 worldPos = rot * (aPosition * 0.8) + iPosition;
                gl_Position = projection * view * vec4(worldPos, 1.0);
                vNormal = rot * aNormal;
                vColor = iColor;
                vWorldPos = worldPos;
            }
            """;

        const string fragmentShaderSource = """
            #version 430 core
            in vec3 vNormal, vColor, vWorldPos;
            out vec4 FragColor;
            void main() {
                vec3 lightDir = normalize(vec3(1, 1, 0.5));
                float diff = max(dot(normalize(vNormal), lightDir), 0.0);
                vec3 color = vColor * (0.2 + diff * 0.8);
                float fog = exp(-length(vWorldPos) * 0.02);
                FragColor = vec4(mix(vec3(0.1, 0.1, 0.15), color, fog), 1.0);
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
                new VertexBufferLayout(24, VertexStepMode.Vertex,
                    new VertexAttribute(0, VertexFormat.Float3, 0),
                    new VertexAttribute(1, VertexFormat.Float3, 12)),
                new VertexBufferLayout((uint)Marshal.SizeOf<InstanceData>(), VertexStepMode.Instance,
                    new VertexAttribute(2, VertexFormat.Float3, 0),
                    new VertexAttribute(3, VertexFormat.Float, 12),
                    new VertexAttribute(4, VertexFormat.Float3, 16),
                    new VertexAttribute(5, VertexFormat.Float, 28))
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
        float camDist = 30.0f;
        float camX = MathF.Sin(_time * 0.3f) * camDist, camZ = MathF.Cos(_time * 0.3f) * camDist;
        float camY = 10.0f + MathF.Sin(_time * 0.2f) * 5.0f;

        var uniformData = new UniformData
        {
            View = Float4x4.CreateLookAt(new Float3(camX, camY, camZ), Float3.Zero, Float3.UnitY),
            Projection = Float4x4.CreatePerspectiveFov(MathF.PI / 4, aspect, 0.1f, 200.0f),
            Time = _time,
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
        cmd.SetVertexBuffer(1, _instanceBuffer!);
        cmd.SetIndexBuffer(_indexBuffer!, IndexFormat.Uint16);
        cmd.DrawIndexed(36, INSTANCE_COUNT);
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
        _instanceBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _vertexBuffer?.Dispose();
        _device?.Dispose();
    }
}
