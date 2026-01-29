using System.Runtime.InteropServices;
using Prowl.Graphite;
using Prowl.Vector;
using Silk.NET.Input;
using Silk.NET.Windowing;

using GL = Silk.NET.OpenGL.GL;
using GBuffer = Prowl.Graphite.Buffer;

namespace GBufferSample;

/// <summary>
/// Sample 08: G-Buffer
/// Demonstrates Multiple Render Targets (MRT) for deferred rendering.
/// Renders scene data to Position, Normal, and Albedo buffers.
/// Press 1-4 to view different G-Buffer components.
/// </summary>
class Program
{
    private static IWindow? _window;
    private static GL? _gl;
    private static GraphiteDevice? _device;

    // G-Buffer textures
    private static Texture? _gPosition, _gNormal, _gAlbedo, _gDepth;

    // Geometry pass resources
    private static GBuffer? _vertexBuffer, _indexBuffer, _uniformBuffer;
    private static ShaderModule? _gBufferVS, _gBufferFS;
    private static PipelineState? _gBufferPipeline;
    private static BindGroupLayout? _gBufferLayout;
    private static BindGroup? _gBufferBindGroup;

    // Visualization pass resources
    private static ShaderModule? _quadVS, _quadFS;
    private static PipelineState? _quadPipeline;
    private static BindGroupLayout? _quadLayout;
    private static BindGroup? _quadBindGroup;
    private static GBuffer? _quadUniformBuffer;
    private static Sampler? _sampler;

    private static float _time;
    private static int _displayMode = 0;

    [StructLayout(LayoutKind.Sequential)]
    struct UniformData
    {
        public Float4x4 Model, View, Projection;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct QuadUniformData
    {
        public int DisplayMode;
        public int _pad0, _pad1, _pad2;
    }

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(1280, 720);
        options.Title = "08 - G-Buffer (Press 1-4 to view buffers)";
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
        Console.WriteLine("Press 1: Show all buffers | 2: Position | 3: Normal | 4: Albedo");

        var input = _window.CreateInput();
        foreach (var keyboard in input.Keyboards)
        {
            keyboard.KeyDown += (kb, key, code) =>
            {
                if (key == Key.Escape) _window.Close();
                if (key == Key.Number1) _displayMode = 0;
                if (key == Key.Number2) _displayMode = 1;
                if (key == Key.Number3) _displayMode = 2;
                if (key == Key.Number4) _displayMode = 3;
            };
        }

        CreateGBufferTextures();
        CreateResources();
    }

    private static void CreateGBufferTextures()
    {
        _gPosition?.Dispose();
        _gNormal?.Dispose();
        _gAlbedo?.Dispose();
        _gDepth?.Dispose();

        var (w, h) = (_device!.SwapchainWidth, _device.SwapchainHeight);

        // Use RenderTarget helper for color buffers, DepthStencil for depth
        _gPosition = _device.CreateTexture(TextureDescriptor.RenderTarget(w, h, TextureFormat.RGBA16Float));
        _gNormal = _device.CreateTexture(TextureDescriptor.RenderTarget(w, h, TextureFormat.RGBA16Float));
        _gAlbedo = _device.CreateTexture(TextureDescriptor.RenderTarget(w, h, TextureFormat.RGBA8Unorm));
        _gDepth = _device.CreateTexture(TextureDescriptor.DepthStencil(w, h));
    }

    private static void CreateResources()
    {
        // Cube geometry with normals and colors (CW winding)
        float[] vertices =
        [
            // Position              Normal                Color
            // Front face (+Z)
            -1, -1,  1,   0,  0,  1,   1.0f, 0.2f, 0.2f,
             1, -1,  1,   0,  0,  1,   1.0f, 0.2f, 0.2f,
             1,  1,  1,   0,  0,  1,   1.0f, 0.2f, 0.2f,
            -1,  1,  1,   0,  0,  1,   1.0f, 0.2f, 0.2f,
            // Back face (-Z)
             1, -1, -1,   0,  0, -1,   0.2f, 1.0f, 0.2f,
            -1, -1, -1,   0,  0, -1,   0.2f, 1.0f, 0.2f,
            -1,  1, -1,   0,  0, -1,   0.2f, 1.0f, 0.2f,
             1,  1, -1,   0,  0, -1,   0.2f, 1.0f, 0.2f,
            // Top face (+Y)
            -1,  1,  1,   0,  1,  0,   0.2f, 0.2f, 1.0f,
             1,  1,  1,   0,  1,  0,   0.2f, 0.2f, 1.0f,
             1,  1, -1,   0,  1,  0,   0.2f, 0.2f, 1.0f,
            -1,  1, -1,   0,  1,  0,   0.2f, 0.2f, 1.0f,
            // Bottom face (-Y)
            -1, -1, -1,   0, -1,  0,   1.0f, 1.0f, 0.2f,
             1, -1, -1,   0, -1,  0,   1.0f, 1.0f, 0.2f,
             1, -1,  1,   0, -1,  0,   1.0f, 1.0f, 0.2f,
            -1, -1,  1,   0, -1,  0,   1.0f, 1.0f, 0.2f,
            // Right face (+X)
             1, -1,  1,   1,  0,  0,   1.0f, 0.2f, 1.0f,
             1, -1, -1,   1,  0,  0,   1.0f, 0.2f, 1.0f,
             1,  1, -1,   1,  0,  0,   1.0f, 0.2f, 1.0f,
             1,  1,  1,   1,  0,  0,   1.0f, 0.2f, 1.0f,
            // Left face (-X)
            -1, -1, -1,  -1,  0,  0,   0.2f, 1.0f, 1.0f,
            -1, -1,  1,  -1,  0,  0,   0.2f, 1.0f, 1.0f,
            -1,  1,  1,  -1,  0,  0,   0.2f, 1.0f, 1.0f,
            -1,  1, -1,  -1,  0,  0,   0.2f, 1.0f, 1.0f,
        ];

        ushort[] indices =
        [
            0, 1, 2, 0, 2, 3,       // Front
            4, 5, 6, 4, 6, 7,       // Back
            8, 9, 10, 8, 10, 11,    // Top
            12, 13, 14, 12, 14, 15, // Bottom
            16, 17, 18, 16, 18, 19, // Right
            20, 21, 22, 20, 22, 23, // Left
        ];

        _vertexBuffer = _device!.CreateBuffer<byte>(BufferUsage.Vertex | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<float>(vertices));
        _indexBuffer = _device.CreateBuffer<byte>(BufferUsage.Index | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<ushort>(indices));
        _uniformBuffer = _device.CreateBuffer(BufferDescriptor.Uniform((uint)Marshal.SizeOf<UniformData>()));

        // G-Buffer shaders
        const string gBufferVS = """
            #version 430 core
            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec3 aNormal;
            layout(location = 2) in vec3 aColor;

            layout(std140, binding = 0) uniform Matrices { mat4 model, view, projection; };

            out vec3 vWorldPos, vNormal, vColor;

            void main() {
                vec4 worldPos = model * vec4(aPosition, 1.0);
                vWorldPos = worldPos.xyz;
                vNormal = mat3(transpose(inverse(model))) * aNormal;
                vColor = aColor;
                gl_Position = projection * view * worldPos;
            }
            """;

        const string gBufferFS = """
            #version 430 core
            in vec3 vWorldPos, vNormal, vColor;

            layout(location = 0) out vec4 gPosition;
            layout(location = 1) out vec4 gNormal;
            layout(location = 2) out vec4 gAlbedo;

            void main() {
                gPosition = vec4(vWorldPos, 1.0);
                gNormal = vec4(normalize(vNormal), 1.0);
                gAlbedo = vec4(vColor, 1.0);
            }
            """;

        _gBufferVS = _device.CreateShaderModule(ShaderModuleDescriptor.VertexGLSL(gBufferVS));
        _gBufferFS = _device.CreateShaderModule(ShaderModuleDescriptor.FragmentGLSL(gBufferFS));

        // Use helper methods for bind group layout entries
        _gBufferLayout = _device.CreateBindGroupLayout(new BindGroupLayoutDescriptor(
            BindGroupLayoutEntry.UniformBuffer(0, ShaderStage.Vertex)
        ));

        _gBufferBindGroup = _device.CreateBindGroup(new BindGroupDescriptor(_gBufferLayout,
            BindGroupEntry.ForBuffer(0, _uniformBuffer)
        ));

        _gBufferPipeline = _device.CreatePipelineState(new PipelineStateDescriptor
        {
            VertexShader = _gBufferVS,
            FragmentShader = _gBufferFS,
            VertexLayout = new VertexLayoutDescriptor(
                new VertexBufferLayout(36,
                    new VertexAttribute(0, VertexFormat.Float3, 0),
                    new VertexAttribute(1, VertexFormat.Float3, 12),
                    new VertexAttribute(2, VertexFormat.Float3, 24)
                )
            ),
            BlendState = new BlendStateDescriptor(BlendAttachment.Opaque, BlendAttachment.Opaque, BlendAttachment.Opaque),
            RenderPassLayout = new RenderPassLayout
            {
                ColorFormats = [TextureFormat.RGBA16Float, TextureFormat.RGBA16Float, TextureFormat.RGBA8Unorm],
                DepthStencilFormat = TextureFormat.Depth24Plus,
            },
            BindGroupLayouts = [_gBufferLayout],
        });

        CreateQuadResources();
    }

    private static void CreateQuadResources()
    {
        _sampler = _device!.CreateSampler(SamplerDescriptor.PointClamp);
        _quadUniformBuffer = _device.CreateBuffer(BufferDescriptor.Uniform(16));

        const string quadVS = """
            #version 430 core
            out vec2 vUV;
            void main() {
                vec2 positions[3] = vec2[](vec2(-1, -1), vec2(-1, 3), vec2(3, -1));
                gl_Position = vec4(positions[gl_VertexID], 0.0, 1.0);
                vUV = positions[gl_VertexID] * 0.5 + 0.5;
            }
            """;

        const string quadFS = """
            #version 430 core
            in vec2 vUV;
            out vec4 FragColor;

            layout(binding = 0) uniform sampler2D gPosition;
            layout(binding = 1) uniform sampler2D gNormal;
            layout(binding = 2) uniform sampler2D gAlbedo;
            layout(std140, binding = 3) uniform Params { int displayMode; };

            void main() {
                if (displayMode == 0) {
                    vec2 uv = vUV * 2.0;
                    if (vUV.x < 0.5 && vUV.y >= 0.5) {
                        vec3 pos = texture(gPosition, uv - vec2(0, 1)).rgb;
                        FragColor = vec4(pos * 0.1 + 0.5, 1.0);
                    } else if (vUV.x >= 0.5 && vUV.y >= 0.5) {
                        vec3 normal = texture(gNormal, uv - vec2(1, 1)).rgb;
                        FragColor = vec4(normal * 0.5 + 0.5, 1.0);
                    } else if (vUV.x < 0.5 && vUV.y < 0.5) {
                        FragColor = texture(gAlbedo, uv);
                    } else {
                        vec3 normal = texture(gNormal, uv - vec2(1, 0)).rgb;
                        vec3 albedo = texture(gAlbedo, uv - vec2(1, 0)).rgb;
                        float diff = max(dot(normal, normalize(vec3(1, 1, 1))), 0.0);
                        FragColor = vec4(albedo * (0.2 + diff * 0.8), 1.0);
                    }
                } else if (displayMode == 1) {
                    FragColor = vec4(texture(gPosition, vUV).rgb * 0.1 + 0.5, 1.0);
                } else if (displayMode == 2) {
                    FragColor = vec4(texture(gNormal, vUV).rgb * 0.5 + 0.5, 1.0);
                } else {
                    FragColor = texture(gAlbedo, vUV);
                }
            }
            """;

        _quadVS = _device.CreateShaderModule(ShaderModuleDescriptor.VertexGLSL(quadVS));
        _quadFS = _device.CreateShaderModule(ShaderModuleDescriptor.FragmentGLSL(quadFS));

        _quadLayout = _device.CreateBindGroupLayout(new BindGroupLayoutDescriptor(
            BindGroupLayoutEntry.CombinedTextureSampler(0),
            BindGroupLayoutEntry.CombinedTextureSampler(1),
            BindGroupLayoutEntry.CombinedTextureSampler(2),
            BindGroupLayoutEntry.UniformBuffer(3, ShaderStage.Fragment)
        ));

        RecreateQuadBindGroup();

        _quadPipeline = _device.CreatePipelineState(new PipelineStateDescriptor
        {
            VertexShader = _quadVS,
            FragmentShader = _quadFS,
            DepthStencilState = DepthStencilStateDescriptor.NoDepth,
            RenderPassLayout = RenderPassLayout.SingleColor(TextureFormat.RGBA8Unorm, null),
            BindGroupLayouts = [_quadLayout],
        });
    }

    private static void RecreateQuadBindGroup()
    {
        _quadBindGroup?.Dispose();
        _quadBindGroup = _device!.CreateBindGroup(new BindGroupDescriptor(_quadLayout!,
            BindGroupEntry.ForTextureSampler(0, _gPosition!, _sampler!),
            BindGroupEntry.ForTextureSampler(1, _gNormal!, _sampler!),
            BindGroupEntry.ForTextureSampler(2, _gAlbedo!, _sampler!),
            BindGroupEntry.ForBuffer(3, _quadUniformBuffer!)
        ));
    }

    private static void OnRender(double deltaTime)
    {
        if (_device == null || _gBufferPipeline == null) return;

        _time += (float)deltaTime;
        float aspect = (float)_device.SwapchainWidth / _device.SwapchainHeight;

        var uniformData = new UniformData
        {
            Model = Float4x4.RotateX(_time * 0.5f) * Float4x4.RotateY(_time),
            View = Float4x4.CreateLookAt(new Float3(0, 2, 6), Float3.Zero, Float3.UnitY),
            Projection = Float4x4.CreatePerspectiveFov(MathF.PI / 4, aspect, 0.1f, 100.0f),
        };
        _device.UpdateBuffer(_uniformBuffer!, 0, MemoryMarshal.CreateReadOnlySpan(ref uniformData, 1));

        var quadUniform = new QuadUniformData { DisplayMode = _displayMode };
        _device.UpdateBuffer(_quadUniformBuffer!, 0, MemoryMarshal.CreateReadOnlySpan(ref quadUniform, 1));

        using var cmd = _device.CreateCommandList();
        cmd.Begin();

        // Pass 1: Render to G-Buffer
        cmd.BeginRenderPass(new RenderPassDescriptor
        {
            ColorAttachments =
            [
                RenderPassColorAttachment.Clear(_gPosition!, Float4.Zero),
                RenderPassColorAttachment.Clear(_gNormal!, Float4.Zero),
                RenderPassColorAttachment.Clear(_gAlbedo!, Float4.Zero),
            ],
            DepthStencilAttachment = RenderPassDepthStencilAttachment.Clear(_gDepth!),
        });
        cmd.SetViewport(0, 0, _device.SwapchainWidth, _device.SwapchainHeight);
        cmd.SetPipeline(_gBufferPipeline);
        cmd.SetBindGroup(0, _gBufferBindGroup!);
        cmd.SetVertexBuffer(0, _vertexBuffer!);
        cmd.SetIndexBuffer(_indexBuffer!, IndexFormat.Uint16);
        cmd.DrawIndexed(36);
        cmd.EndRenderPass();

        // Pass 2: Visualize G-Buffer
        cmd.BeginRenderPass(RenderPassDescriptor.SingleColor(
            RenderPassColorAttachment.Clear(_device.GetSwapchainTexture(), Float4.UnitW)));
        cmd.SetViewport(0, 0, _device.SwapchainWidth, _device.SwapchainHeight);
        cmd.SetPipeline(_quadPipeline!);
        cmd.SetBindGroup(0, _quadBindGroup!);
        cmd.Draw(3);
        cmd.EndRenderPass();

        cmd.End();
        _device.SubmitCommands(cmd);
    }

    private static void OnResize(Silk.NET.Maths.Vector2D<int> size)
    {
        if (_device == null || size.X == 0 || size.Y == 0) return;
        _device.ResizeSwapchain((uint)size.X, (uint)size.Y);
        CreateGBufferTextures();
        RecreateQuadBindGroup();
    }

    private static void OnClose()
    {
        _quadPipeline?.Dispose();
        _quadBindGroup?.Dispose();
        _quadLayout?.Dispose();
        _quadFS?.Dispose();
        _quadVS?.Dispose();
        _quadUniformBuffer?.Dispose();
        _sampler?.Dispose();
        _gBufferPipeline?.Dispose();
        _gBufferBindGroup?.Dispose();
        _gBufferLayout?.Dispose();
        _gBufferFS?.Dispose();
        _gBufferVS?.Dispose();
        _uniformBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _vertexBuffer?.Dispose();
        _gDepth?.Dispose();
        _gAlbedo?.Dispose();
        _gNormal?.Dispose();
        _gPosition?.Dispose();
        _device?.Dispose();
    }
}
