using System.Runtime.InteropServices;
using Prowl.Graphite;
using Prowl.Vector;
using Silk.NET.Input;
using Silk.NET.Windowing;

using GL = Silk.NET.OpenGL.GL;
using GBuffer = Prowl.Graphite.Buffer;

namespace Texturing;

/// <summary>
/// Sample 03: Texturing
/// Demonstrates loading and sampling a 2D texture on a quad.
/// Uses a procedurally generated checkerboard pattern.
/// </summary>
class Program
{
    private static IWindow? _window;
    private static GL? _gl;
    private static GraphiteDevice? _device;

    private static GBuffer? _vertexBuffer, _indexBuffer;
    private static Texture? _texture;
    private static Sampler? _sampler;
    private static ShaderModule? _vertexShader, _fragmentShader;
    private static PipelineState? _pipeline;
    private static BindGroupLayout? _bindGroupLayout;
    private static BindGroup? _bindGroup;

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(800, 600);
        options.Title = "03 - Texturing";
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
        // Quad vertices: position (3) + texcoord (2)
        float[] vertices =
        [
            -0.5f,  0.5f, 0.0f,  0.0f, 0.0f,
             0.5f,  0.5f, 0.0f,  1.0f, 0.0f,
             0.5f, -0.5f, 0.0f,  1.0f, 1.0f,
            -0.5f, -0.5f, 0.0f,  0.0f, 1.0f,
        ];

        ushort[] indices = [0, 1, 2, 0, 2, 3]; // CW winding

        _vertexBuffer = _device!.CreateBuffer<byte>(BufferUsage.Vertex | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<float>(vertices));
        _indexBuffer = _device.CreateBuffer<byte>(BufferUsage.Index | BufferUsage.CopyDestination, MemoryMarshal.AsBytes<ushort>(indices));

        // Create checkerboard texture
        const int texSize = 64, checkerSize = 8;
        byte[] textureData = new byte[texSize * texSize * 4];
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                int i = (y * texSize + x) * 4;
                byte c = ((x / checkerSize) + (y / checkerSize)) % 2 == 0 ? (byte)255 : (byte)64;
                textureData[i] = textureData[i + 1] = textureData[i + 2] = c;
                textureData[i + 3] = 255;
            }
        }

        _texture = _device.CreateTexture(TextureDescriptor.Texture2D(texSize, texSize, TextureFormat.RGBA8Unorm, TextureUsage.Sampled | TextureUsage.CopyDestination));
        _device.UpdateTexture(_texture, TextureUpdateDescriptor.FullMip(texSize, texSize), textureData);
        _sampler = _device.CreateSampler(SamplerDescriptor.PointRepeat);

        const string vertexShaderSource = """
            #version 430 core
            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec2 aTexCoord;
            out vec2 vTexCoord;
            void main() {
                gl_Position = vec4(aPosition, 1.0);
                vTexCoord = aTexCoord;
            }
            """;

        const string fragmentShaderSource = """
            #version 430 core
            in vec2 vTexCoord;
            out vec4 FragColor;
            layout(binding = 0) uniform sampler2D uTexture;
            void main() {
                FragColor = texture(uTexture, vTexCoord);
            }
            """;

        _vertexShader = _device.CreateShaderModule(ShaderModuleDescriptor.VertexGLSL(vertexShaderSource));
        _fragmentShader = _device.CreateShaderModule(ShaderModuleDescriptor.FragmentGLSL(fragmentShaderSource));

        _bindGroupLayout = _device.CreateBindGroupLayout(new BindGroupLayoutDescriptor(
            BindGroupLayoutEntry.CombinedTextureSampler(0)
        ));

        _bindGroup = _device.CreateBindGroup(new BindGroupDescriptor(_bindGroupLayout,
            BindGroupEntry.ForTextureSampler(0, _texture, _sampler)
        ));

        _pipeline = _device.CreatePipelineState(new PipelineStateDescriptor
        {
            VertexShader = _vertexShader,
            FragmentShader = _fragmentShader,
            VertexLayout = new VertexLayoutDescriptor(
                new VertexBufferLayout(20,
                    new VertexAttribute(0, VertexFormat.Float3, 0),
                    new VertexAttribute(1, VertexFormat.Float2, 12)
                )
            ),
            DepthStencilState = DepthStencilStateDescriptor.NoDepth,
            RenderPassLayout = RenderPassLayout.SingleColor(TextureFormat.RGBA8Unorm, null),
            BindGroupLayouts = [_bindGroupLayout],
        });
    }

    private static void OnRender(double deltaTime)
    {
        if (_device == null || _pipeline == null) return;

        using var cmd = _device.CreateCommandList();
        cmd.Begin();
        cmd.BeginRenderPass(RenderPassDescriptor.SingleColor(
            RenderPassColorAttachment.Clear(_device.GetSwapchainTexture(), new Float4(0.2f, 0.2f, 0.3f, 1.0f))));
        cmd.SetViewport(0, 0, _device.SwapchainWidth, _device.SwapchainHeight);
        cmd.SetPipeline(_pipeline);
        cmd.SetBindGroup(0, _bindGroup!);
        cmd.SetVertexBuffer(0, _vertexBuffer!);
        cmd.SetIndexBuffer(_indexBuffer!, IndexFormat.Uint16);
        cmd.DrawIndexed(6);
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
        _sampler?.Dispose();
        _texture?.Dispose();
        _indexBuffer?.Dispose();
        _vertexBuffer?.Dispose();
        _device?.Dispose();
    }
}
