using System.Runtime.InteropServices;
using Prowl.Graphite;
using Prowl.Vector;
using Silk.NET.Input;
using Silk.NET.Windowing;

using GL = Silk.NET.OpenGL.GL;
using GBuffer = Prowl.Graphite.Buffer;

namespace DeferredRendering;

/// <summary>
/// Sample 09: Deferred Rendering
/// Full deferred pipeline with 32 animated point lights.
/// </summary>
class Program
{
    private static IWindow? _window;
    private static GL? _gl;
    private static GraphiteDevice? _device;

    private static Texture? _gPosition, _gNormal, _gAlbedo, _gDepth;

    private static GBuffer? _cubeVB, _cubeIB, _planeVB, _planeIB, _geometryUB;
    private static ShaderModule? _geometryVS, _geometryFS;
    private static PipelineState? _geometryPipeline;
    private static BindGroupLayout? _geometryLayout;
    private static BindGroup? _geometryBindGroup;

    private static ShaderModule? _lightingVS, _lightingFS;
    private static PipelineState? _lightingPipeline;
    private static BindGroupLayout? _lightingLayout;
    private static BindGroup? _lightingBindGroup;
    private static GBuffer? _lightingUB;
    private static Sampler? _sampler;

    private static float _time;
    private const int NUM_LIGHTS = 32;

    [StructLayout(LayoutKind.Sequential)]
    struct GeometryUniformData { public Float4x4 Model, View, Projection; }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct LightingUniformData { public Float4 ViewPosAndNumLights; public fixed float Lights[NUM_LIGHTS * 8]; }

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(1280, 720);
        options.Title = $"09 - Deferred Rendering ({NUM_LIGHTS} lights)";
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

        CreateGBufferTextures();
        CreateGeometryResources();
        CreateLightingResources();
    }

    private static void CreateGBufferTextures()
    {
        _gPosition?.Dispose(); _gNormal?.Dispose(); _gAlbedo?.Dispose(); _gDepth?.Dispose();
        var (w, h) = (_device!.SwapchainWidth, _device.SwapchainHeight);
        _gPosition = _device.CreateTexture(TextureDescriptor.RenderTarget(w, h, TextureFormat.RGBA16Float));
        _gNormal = _device.CreateTexture(TextureDescriptor.RenderTarget(w, h, TextureFormat.RGBA16Float));
        _gAlbedo = _device.CreateTexture(TextureDescriptor.RenderTarget(w, h, TextureFormat.RGBA8Unorm));
        _gDepth = _device.CreateTexture(TextureDescriptor.DepthStencil(w, h));
    }

    private static void CreateGeometryResources()
    {
        // Cube (CW winding)
        float[] cubeVerts =
        [
            -0.5f,-0.5f, 0.5f,  0, 0, 1,   0.5f,-0.5f, 0.5f,  0, 0, 1,   0.5f, 0.5f, 0.5f,  0, 0, 1,  -0.5f, 0.5f, 0.5f,  0, 0, 1,
             0.5f,-0.5f,-0.5f,  0, 0,-1,  -0.5f,-0.5f,-0.5f,  0, 0,-1,  -0.5f, 0.5f,-0.5f,  0, 0,-1,   0.5f, 0.5f,-0.5f,  0, 0,-1,
            -0.5f, 0.5f, 0.5f,  0, 1, 0,   0.5f, 0.5f, 0.5f,  0, 1, 0,   0.5f, 0.5f,-0.5f,  0, 1, 0,  -0.5f, 0.5f,-0.5f,  0, 1, 0,
            -0.5f,-0.5f,-0.5f,  0,-1, 0,   0.5f,-0.5f,-0.5f,  0,-1, 0,   0.5f,-0.5f, 0.5f,  0,-1, 0,  -0.5f,-0.5f, 0.5f,  0,-1, 0,
             0.5f,-0.5f, 0.5f,  1, 0, 0,   0.5f,-0.5f,-0.5f,  1, 0, 0,   0.5f, 0.5f,-0.5f,  1, 0, 0,   0.5f, 0.5f, 0.5f,  1, 0, 0,
            -0.5f,-0.5f,-0.5f, -1, 0, 0,  -0.5f,-0.5f, 0.5f, -1, 0, 0,  -0.5f, 0.5f, 0.5f, -1, 0, 0,  -0.5f, 0.5f,-0.5f, -1, 0, 0,
        ];
        ushort[] cubeIdx = [0,1,2,0,2,3, 4,5,6,4,6,7, 8,9,10,8,10,11, 12,13,14,12,14,15, 16,17,18,16,18,19, 20,21,22,20,22,23];
        float[] planeVerts = [-15, 0, -15, 0, 1, 0,  -15, 0, 15, 0, 1, 0,  15, 0, 15, 0, 1, 0,  15, 0, -15, 0, 1, 0];
        ushort[] planeIdx = [0, 1, 2, 0, 2, 3];

        _cubeVB = _device!.CreateBuffer<byte>(BufferUsage.Vertex | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<float>(cubeVerts));
        _cubeIB = _device.CreateBuffer<byte>(BufferUsage.Index | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<ushort>(cubeIdx));
        _planeVB = _device.CreateBuffer<byte>(BufferUsage.Vertex | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<float>(planeVerts));
        _planeIB = _device.CreateBuffer<byte>(BufferUsage.Index | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<ushort>(planeIdx));
        _geometryUB = _device.CreateBuffer(BufferDescriptor.Uniform((uint)Marshal.SizeOf<GeometryUniformData>()));

        const string geometryVS = """
            #version 430 core
            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec3 aNormal;
            layout(std140, binding = 0) uniform Matrices { mat4 model, view, projection; };
            out vec3 vWorldPos, vNormal;
            void main() {
                vec4 worldPos = model * vec4(aPosition, 1.0);
                vWorldPos = worldPos.xyz;
                vNormal = mat3(transpose(inverse(model))) * aNormal;
                gl_Position = projection * view * worldPos;
            }
            """;

        const string geometryFS = """
            #version 430 core
            in vec3 vWorldPos, vNormal;
            layout(location = 0) out vec4 gPosition;
            layout(location = 1) out vec4 gNormal;
            layout(location = 2) out vec4 gAlbedo;
            uniform vec3 uAlbedo;
            void main() {
                gPosition = vec4(vWorldPos, 1.0);
                gNormal = vec4(normalize(vNormal), 1.0);
                gAlbedo = vec4(uAlbedo, 1.0);
            }
            """;

        _geometryVS = _device.CreateShaderModule(ShaderModuleDescriptor.VertexGLSL(geometryVS));
        _geometryFS = _device.CreateShaderModule(ShaderModuleDescriptor.FragmentGLSL(geometryFS));

        _geometryLayout = _device.CreateBindGroupLayout(new BindGroupLayoutDescriptor(BindGroupLayoutEntry.UniformBuffer(0, ShaderStage.Vertex)));
        _geometryBindGroup = _device.CreateBindGroup(new BindGroupDescriptor(_geometryLayout, BindGroupEntry.ForBuffer(0, _geometryUB)));

        _geometryPipeline = _device.CreatePipelineState(new PipelineStateDescriptor
        {
            VertexShader = _geometryVS,
            FragmentShader = _geometryFS,
            VertexLayout = new VertexLayoutDescriptor(new VertexBufferLayout(24, new VertexAttribute(0, VertexFormat.Float3, 0), new VertexAttribute(1, VertexFormat.Float3, 12))),
            BlendState = new BlendStateDescriptor(BlendAttachment.Opaque, BlendAttachment.Opaque, BlendAttachment.Opaque),
            RenderPassLayout = new RenderPassLayout { ColorFormats = [TextureFormat.RGBA16Float, TextureFormat.RGBA16Float, TextureFormat.RGBA8Unorm], DepthStencilFormat = TextureFormat.Depth24Plus },
            BindGroupLayouts = [_geometryLayout],
        });
    }

    private static void CreateLightingResources()
    {
        _sampler = _device!.CreateSampler(SamplerDescriptor.PointClamp);
        _lightingUB = _device.CreateBuffer(BufferDescriptor.Uniform((uint)Marshal.SizeOf<LightingUniformData>()));

        const string lightingVS = """
            #version 430 core
            out vec2 vUV;
            void main() {
                vec2 p[3] = vec2[](vec2(-1, -1), vec2(-1, 3), vec2(3, -1));
                gl_Position = vec4(p[gl_VertexID], 0.0, 1.0);
                vUV = p[gl_VertexID] * 0.5 + 0.5;
            }
            """;

        string lightingFS = $$"""
            #version 430 core
            in vec2 vUV;
            out vec4 FragColor;
            layout(binding = 0) uniform sampler2D gPosition;
            layout(binding = 1) uniform sampler2D gNormal;
            layout(binding = 2) uniform sampler2D gAlbedo;
            struct PointLight { vec4 positionAndRadius, colorAndIntensity; };
            layout(std140, binding = 3) uniform LightingData { vec4 viewPosAndNumLights; PointLight lights[{{NUM_LIGHTS}}]; };
            void main() {
                vec3 fragPos = texture(gPosition, vUV).rgb, normal = normalize(texture(gNormal, vUV).rgb), albedo = texture(gAlbedo, vUV).rgb;
                if (texture(gPosition, vUV).a < 0.5) { FragColor = vec4(0.02, 0.02, 0.03, 1.0); return; }
                vec3 viewDir = normalize(viewPosAndNumLights.xyz - fragPos), lighting = albedo * 0.05;
                for (int i = 0; i < int(viewPosAndNumLights.w); i++) {
                    vec3 lp = lights[i].positionAndRadius.xyz, lc = lights[i].colorAndIntensity.xyz;
                    float r = lights[i].positionAndRadius.w, intensity = lights[i].colorAndIntensity.w;
                    vec3 ld = lp - fragPos; float dist = length(ld); ld = normalize(ld);
                    float att = smoothstep(r, 0.0, dist) / (1.0 + 0.09 * dist + 0.032 * dist * dist);
                    float diff = max(dot(normal, ld), 0.0), spec = pow(max(dot(normal, normalize(ld + viewDir)), 0.0), 32.0);
                    lighting += (diff * albedo + spec * 0.5) * lc * intensity * att;
                }
                lighting = lighting / (lighting + vec3(1.0));
                FragColor = vec4(pow(lighting, vec3(1.0/2.2)), 1.0);
            }
            """;

        _lightingVS = _device.CreateShaderModule(ShaderModuleDescriptor.VertexGLSL(lightingVS));
        _lightingFS = _device.CreateShaderModule(ShaderModuleDescriptor.FragmentGLSL(lightingFS));

        _lightingLayout = _device.CreateBindGroupLayout(new BindGroupLayoutDescriptor(
            BindGroupLayoutEntry.CombinedTextureSampler(0),
            BindGroupLayoutEntry.CombinedTextureSampler(1),
            BindGroupLayoutEntry.CombinedTextureSampler(2),
            BindGroupLayoutEntry.UniformBuffer(3, ShaderStage.Fragment)
        ));

        RecreateLightingBindGroup();

        _lightingPipeline = _device.CreatePipelineState(new PipelineStateDescriptor
        {
            VertexShader = _lightingVS,
            FragmentShader = _lightingFS,
            DepthStencilState = DepthStencilStateDescriptor.NoDepth,
            RenderPassLayout = RenderPassLayout.SingleColor(TextureFormat.RGBA8Unorm, null),
            BindGroupLayouts = [_lightingLayout],
        });
    }

    private static void RecreateLightingBindGroup()
    {
        _lightingBindGroup?.Dispose();
        _lightingBindGroup = _device!.CreateBindGroup(new BindGroupDescriptor(_lightingLayout!,
            BindGroupEntry.ForTextureSampler(0, _gPosition!, _sampler!),
            BindGroupEntry.ForTextureSampler(1, _gNormal!, _sampler!),
            BindGroupEntry.ForTextureSampler(2, _gAlbedo!, _sampler!),
            BindGroupEntry.ForBuffer(3, _lightingUB!)
        ));
    }

    private static unsafe void OnRender(double deltaTime)
    {
        if (_device == null || _geometryPipeline == null) return;

        _time += (float)deltaTime;
        float aspect = (float)_device.SwapchainWidth / _device.SwapchainHeight;
        float camX = MathF.Sin(_time * 0.2f) * 12, camY = 8, camZ = MathF.Cos(_time * 0.2f) * 12;
        var view = Float4x4.CreateLookAt(new Float3(camX, camY, camZ), new Float3(0, 1, 0), Float3.UnitY);
        var projection = Float4x4.CreatePerspectiveFov(MathF.PI / 4, aspect, 0.1f, 100.0f);

        var lightingData = new LightingUniformData { ViewPosAndNumLights = new Float4(camX, camY, camZ, NUM_LIGHTS) };
        for (int i = 0; i < NUM_LIGHTS; i++)
        {
            float angle = (i / (float)NUM_LIGHTS) * MathF.PI * 2 + _time * (0.3f + (i % 5) * 0.1f);
            float radius = 3 + (i % 7) * 1.5f, height = 1 + MathF.Sin(_time * 2 + i) * 0.5f + (i % 3);
            int idx = i * 8;
            lightingData.Lights[idx + 0] = MathF.Cos(angle) * radius;
            lightingData.Lights[idx + 1] = height;
            lightingData.Lights[idx + 2] = MathF.Sin(angle) * radius;
            lightingData.Lights[idx + 3] = 8.0f;
            lightingData.Lights[idx + 4] = 0.5f + 0.5f * MathF.Sin(i * 0.7f);
            lightingData.Lights[idx + 5] = 0.5f + 0.5f * MathF.Sin(i * 1.3f + 2);
            lightingData.Lights[idx + 6] = 0.5f + 0.5f * MathF.Sin(i * 0.9f + 4);
            lightingData.Lights[idx + 7] = 2.0f + (i % 4) * 0.5f;
        }
        _device.UpdateBuffer(_lightingUB!, 0, MemoryMarshal.CreateReadOnlySpan(ref lightingData, 1));

        using var cmd = _device.CreateCommandList();
        cmd.Begin();

        // Geometry Pass
        cmd.BeginRenderPass(new RenderPassDescriptor
        {
            ColorAttachments = [RenderPassColorAttachment.Clear(_gPosition!, Float4.Zero), RenderPassColorAttachment.Clear(_gNormal!, Float4.Zero), RenderPassColorAttachment.Clear(_gAlbedo!, Float4.Zero)],
            DepthStencilAttachment = RenderPassDepthStencilAttachment.Clear(_gDepth!),
        });
        cmd.SetViewport(0, 0, _device.SwapchainWidth, _device.SwapchainHeight);
        cmd.SetPipeline(_geometryPipeline);
        cmd.SetBindGroup(0, _geometryBindGroup!);

        // Floor
        var floorU = new GeometryUniformData { Model = Float4x4.Identity, View = view, Projection = projection };
        _device.UpdateBuffer(_geometryUB!, 0, MemoryMarshal.CreateReadOnlySpan(ref floorU, 1));
        cmd.SetVertexBuffer(0, _planeVB!);
        cmd.SetIndexBuffer(_planeIB!, IndexFormat.Uint16);
        cmd.DrawIndexed(6);

        // Cube
        cmd.SetVertexBuffer(0, _cubeVB!);
        cmd.SetIndexBuffer(_cubeIB!, IndexFormat.Uint16);
        var cubeU = new GeometryUniformData { Model = Float4x4.RotateX(_time * 0.5f) * Float4x4.RotateY(_time), View = view, Projection = projection };
        _device.UpdateBuffer(_geometryUB!, 0, MemoryMarshal.CreateReadOnlySpan(ref cubeU, 1));
        cmd.DrawIndexed(36);
        cmd.EndRenderPass();

        // Lighting Pass
        cmd.BeginRenderPass(RenderPassDescriptor.SingleColor(RenderPassColorAttachment.Clear(_device.GetSwapchainTexture(), Float4.Zero)));
        cmd.SetViewport(0, 0, _device.SwapchainWidth, _device.SwapchainHeight);
        cmd.SetPipeline(_lightingPipeline!);
        cmd.SetBindGroup(0, _lightingBindGroup!);
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
        RecreateLightingBindGroup();
    }

    private static void OnClose()
    {
        _lightingPipeline?.Dispose(); _lightingBindGroup?.Dispose(); _lightingLayout?.Dispose();
        _lightingFS?.Dispose(); _lightingVS?.Dispose(); _lightingUB?.Dispose(); _sampler?.Dispose();
        _geometryPipeline?.Dispose(); _geometryBindGroup?.Dispose(); _geometryLayout?.Dispose();
        _geometryFS?.Dispose(); _geometryVS?.Dispose(); _geometryUB?.Dispose();
        _planeIB?.Dispose(); _planeVB?.Dispose(); _cubeIB?.Dispose(); _cubeVB?.Dispose();
        _gDepth?.Dispose(); _gAlbedo?.Dispose(); _gNormal?.Dispose(); _gPosition?.Dispose();
        _device?.Dispose();
    }
}
