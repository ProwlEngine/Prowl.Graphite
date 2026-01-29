using System.Runtime.InteropServices;
using Prowl.Graphite;
using Prowl.Vector;
using Silk.NET.Input;
using Silk.NET.Windowing;

using GL = Silk.NET.OpenGL.GL;
using GBuffer = Prowl.Graphite.Buffer;

namespace Raymarching;

/// <summary>
/// Sample 06: Raymarching
/// Demonstrates a fullscreen shader that raymarches a 3D scene.
/// Features spheres, a plane, soft shadows, and ambient occlusion.
/// </summary>
class Program
{
    private static IWindow? _window;
    private static GL? _gl;
    private static GraphiteDevice? _device;

    private static GBuffer? _uniformBuffer;
    private static ShaderModule? _vertexShader, _fragmentShader;
    private static PipelineState? _pipeline;
    private static BindGroupLayout? _bindGroupLayout;
    private static BindGroup? _bindGroup;

    private static float _time;

    [StructLayout(LayoutKind.Sequential)]
    struct UniformData
    {
        public Float2 Resolution;
        public float Time;
        public float _pad;
    }

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(800, 600);
        options.Title = "06 - Raymarching";
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
        _uniformBuffer = _device!.CreateBuffer(BufferDescriptor.Uniform((uint)Marshal.SizeOf<UniformData>()));

        const string vertexShaderSource = """
            #version 430 core
            out vec2 vUV;
            void main() {
                vec2 positions[3] = vec2[](vec2(-1, -1), vec2(-1, 3), vec2(3, -1));
                gl_Position = vec4(positions[gl_VertexID], 0.0, 1.0);
                vUV = positions[gl_VertexID] * 0.5 + 0.5;
            }
            """;

        const string fragmentShaderSource = """
            #version 430 core
            in vec2 vUV;
            out vec4 FragColor;

            layout(std140, binding = 0) uniform Uniforms { vec2 uResolution; float uTime; };

            const int MAX_STEPS = 100;
            const float MAX_DIST = 100.0;
            const float SURF_DIST = 0.001;

            float sdSphere(vec3 p, float r) { return length(p) - r; }
            float sdPlane(vec3 p, vec3 n, float h) { return dot(p, n) + h; }
            float smin(float a, float b, float k) {
                float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0);
                return mix(b, a, h) - k * h * (1.0 - h);
            }

            float sceneSDF(vec3 p) {
                float s1 = sdSphere(p - vec3(sin(uTime) * 1.5, 0.5, cos(uTime) * 1.5), 0.5);
                float s2 = sdSphere(p - vec3(-sin(uTime * 0.7) * 1.2, 0.3 + sin(uTime * 2.0) * 0.2, cos(uTime * 0.7) * 1.2), 0.3);
                float s3 = sdSphere(p - vec3(0.0, 0.4 + sin(uTime * 1.5) * 0.3, 0.0), 0.4);
                float plane = sdPlane(p, vec3(0.0, 1.0, 0.0), 0.0);
                return min(smin(s1, smin(s2, s3, 0.3), 0.3), plane);
            }

            vec3 calcNormal(vec3 p) {
                vec2 e = vec2(0.001, 0.0);
                return normalize(vec3(sceneSDF(p + e.xyy) - sceneSDF(p - e.xyy),
                                      sceneSDF(p + e.yxy) - sceneSDF(p - e.yxy),
                                      sceneSDF(p + e.yyx) - sceneSDF(p - e.yyx)));
            }

            float raymarch(vec3 ro, vec3 rd) {
                float d = 0.0;
                for (int i = 0; i < MAX_STEPS; i++) {
                    float ds = sceneSDF(ro + rd * d);
                    d += ds;
                    if (d > MAX_DIST || ds < SURF_DIST) break;
                }
                return d;
            }

            float softShadow(vec3 ro, vec3 rd, float mint, float maxt, float k) {
                float res = 1.0, t = mint;
                for (int i = 0; i < 32; i++) {
                    float h = sceneSDF(ro + rd * t);
                    res = min(res, k * h / t);
                    t += clamp(h, 0.02, 0.2);
                    if (h < 0.001 || t > maxt) break;
                }
                return clamp(res, 0.0, 1.0);
            }

            float ambientOcclusion(vec3 p, vec3 n) {
                float occ = 0.0, sca = 1.0;
                for (int i = 0; i < 5; i++) {
                    float h = 0.01 + 0.12 * float(i);
                    occ += (h - sceneSDF(p + h * n)) * sca;
                    sca *= 0.95;
                }
                return clamp(1.0 - 3.0 * occ, 0.0, 1.0);
            }

            void main() {
                vec2 uv = (gl_FragCoord.xy - 0.5 * uResolution) / uResolution.y;
                vec3 ro = vec3(0.0, 2.0, 5.0), lookAt = vec3(0.0, 0.5, 0.0);
                vec3 forward = normalize(lookAt - ro);
                vec3 right = normalize(cross(vec3(0.0, 1.0, 0.0), forward));
                vec3 up = cross(forward, right);
                vec3 rd = normalize(uv.x * right + uv.y * up + 1.5 * forward);

                vec3 lightPos = vec3(3.0, 5.0, 4.0), lightCol = vec3(1.0, 0.95, 0.9);
                float d = raymarch(ro, rd);
                vec3 col = vec3(0.0);

                if (d < MAX_DIST) {
                    vec3 p = ro + rd * d, n = calcNormal(p), l = normalize(lightPos - p);
                    vec3 v = normalize(ro - p), h = normalize(l + v);
                    vec3 matCol = p.y < 0.01 ? mix(vec3(0.2), vec3(0.8), mod(floor(p.x) + floor(p.z), 2.0)) : vec3(0.8, 0.3, 0.2);
                    float diff = max(dot(n, l), 0.0), spec = pow(max(dot(n, h), 0.0), 32.0);
                    float shadow = softShadow(p + n * 0.02, l, 0.02, length(lightPos - p), 16.0);
                    float ao = ambientOcclusion(p, n);
                    col = vec3(0.1, 0.15, 0.2) * matCol * ao + (matCol * diff + vec3(0.5) * spec) * lightCol * shadow;
                    col = mix(col, vec3(0.5, 0.6, 0.7), 1.0 - exp(-0.01 * d * d));
                } else {
                    col = mix(vec3(0.5, 0.6, 0.7), vec3(0.2, 0.3, 0.5), uv.y + 0.5);
                }

                FragColor = vec4(pow(col, vec3(0.4545)), 1.0);
            }
            """;

        _vertexShader = _device.CreateShaderModule(ShaderModuleDescriptor.VertexGLSL(vertexShaderSource));
        _fragmentShader = _device.CreateShaderModule(ShaderModuleDescriptor.FragmentGLSL(fragmentShaderSource));

        _bindGroupLayout = _device.CreateBindGroupLayout(new BindGroupLayoutDescriptor(
            BindGroupLayoutEntry.UniformBuffer(0, ShaderStage.Fragment)
        ));

        _bindGroup = _device.CreateBindGroup(new BindGroupDescriptor(_bindGroupLayout,
            BindGroupEntry.ForBuffer(0, _uniformBuffer)
        ));

        _pipeline = _device.CreatePipelineState(new PipelineStateDescriptor
        {
            VertexShader = _vertexShader,
            FragmentShader = _fragmentShader,
            DepthStencilState = DepthStencilStateDescriptor.NoDepth,
            RenderPassLayout = RenderPassLayout.SingleColor(TextureFormat.RGBA8Unorm, null),
            BindGroupLayouts = [_bindGroupLayout],
        });
    }

    private static void OnRender(double deltaTime)
    {
        if (_device == null || _pipeline == null) return;

        _time += (float)deltaTime;
        var uniformData = new UniformData { Resolution = new Float2(_device.SwapchainWidth, _device.SwapchainHeight), Time = _time };
        _device.UpdateBuffer(_uniformBuffer!, 0, MemoryMarshal.CreateReadOnlySpan(ref uniformData, 1));

        using var cmd = _device.CreateCommandList();
        cmd.Begin();
        cmd.BeginRenderPass(RenderPassDescriptor.SingleColor(
            RenderPassColorAttachment.Clear(_device.GetSwapchainTexture(), Float4.Zero)));
        cmd.SetViewport(0, 0, _device.SwapchainWidth, _device.SwapchainHeight);
        cmd.SetPipeline(_pipeline);
        cmd.SetBindGroup(0, _bindGroup!);
        cmd.Draw(3);
        cmd.EndRenderPass();
        cmd.End();
        _device.SubmitCommands(cmd);
    }

    private static void OnResize(Silk.NET.Maths.Vector2D<int> size) => _device?.ResizeSwapchain((uint)size.X, (uint)size.Y);

    private static void OnClose()
    {
        _pipeline?.Dispose();
        _bindGroup?.Dispose();
        _bindGroupLayout?.Dispose();
        _fragmentShader?.Dispose();
        _vertexShader?.Dispose();
        _uniformBuffer?.Dispose();
        _device?.Dispose();
    }
}
