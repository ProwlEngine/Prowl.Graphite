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

    // Resources
    private static GBuffer? _vertexBuffer;
    private static GBuffer? _indexBuffer;
    private static Texture? _texture;
    private static Sampler? _sampler;
    private static ShaderModule? _vertexShader;
    private static ShaderModule? _fragmentShader;
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
        // Quad vertices: position (3) + texcoord (2)
        float[] vertices =
        [
            // Position          // TexCoord
            -0.5f,  0.5f, 0.0f,  0.0f, 0.0f,  // Top-left
             0.5f,  0.5f, 0.0f,  1.0f, 0.0f,  // Top-right
             0.5f, -0.5f, 0.0f,  1.0f, 1.0f,  // Bottom-right
            -0.5f, -0.5f, 0.0f,  0.0f, 1.0f,  // Bottom-left
        ];

        ushort[] indices = [0, 1, 2, 0, 2, 3]; // CW winding (Unity convention)

        _vertexBuffer = _device!.CreateBuffer<byte>(
            BufferUsage.Vertex | BufferUsage.CopyDestination,
            MemoryMarshal.AsBytes<float>(vertices));

        _indexBuffer = _device.CreateBuffer<byte>(
            BufferUsage.Index | BufferUsage.CopyDestination,
            MemoryMarshal.AsBytes<ushort>(indices));

        // Create a checkerboard texture procedurally
        const int texSize = 64;
        const int checkerSize = 8;
        byte[] textureData = new byte[texSize * texSize * 4];

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                int index = (y * texSize + x) * 4;
                bool isWhite = ((x / checkerSize) + (y / checkerSize)) % 2 == 0;
                byte color = isWhite ? (byte)255 : (byte)64;
                textureData[index + 0] = color;     // R
                textureData[index + 1] = color;     // G
                textureData[index + 2] = color;     // B
                textureData[index + 3] = 255;       // A
            }
        }

        _texture = _device.CreateTexture(new TextureDescriptor
        {
            Dimension = TextureDimension.Texture2D,
            Width = texSize,
            Height = texSize,
            Depth = 1,
            MipLevels = 1,
            ArrayLayers = 1,
            Format = TextureFormat.RGBA8Unorm,
            Usage = TextureUsage.Sampled | TextureUsage.CopyDestination,
            SampleCount = SampleCount.Count1,
            DebugName = "CheckerboardTexture"
        });

        _device.UpdateTexture(_texture, new TextureUpdateDescriptor
        {
            X = 0, Y = 0, Z = 0,
            Width = texSize, Height = texSize, Depth = 1,
            MipLevel = 0, ArrayLayer = 0
        }, textureData);

        _sampler = _device.CreateSampler(new SamplerDescriptor
        {
            MinFilter = TextureFilter.Nearest,
            MagFilter = TextureFilter.Nearest,
            MipmapFilter = TextureFilter.Nearest,
            AddressModeU = TextureAddressMode.Repeat,
            AddressModeV = TextureAddressMode.Repeat,
            AddressModeW = TextureAddressMode.Repeat,
        });

        // Shaders with texture sampling
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

        // Create bind group layout for texture + sampler
        _bindGroupLayout = _device.CreateBindGroupLayout(new BindGroupLayoutDescriptor
        {
            Entries =
            [
                new BindGroupLayoutEntry
                {
                    Binding = 0,
                    Visibility = ShaderStage.Fragment,
                    Type = BindingType.CombinedTextureSampler,
                }
            ]
        });

        // Create bind group with actual resources
        _bindGroup = _device.CreateBindGroup(new BindGroupDescriptor
        {
            Layout = _bindGroupLayout,
            Entries =
            [
                new BindGroupEntry
                {
                    Binding = 0,
                    Texture = _texture,
                    Sampler = _sampler,
                }
            ]
        });

        // Create pipeline
        _pipeline = _device.CreatePipelineState(new PipelineStateDescriptor
        {
            VertexShader = _vertexShader,
            FragmentShader = _fragmentShader,
            VertexLayout = new VertexLayoutDescriptor(
                new VertexBufferLayout(20, // 5 floats * 4 bytes
                    new VertexAttribute(0, VertexFormat.Float3, 0),   // Position
                    new VertexAttribute(1, VertexFormat.Float2, 12)   // TexCoord
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

        var swapchainTexture = _device.GetSwapchainTexture();

        using var commandList = _device.CreateCommandList();
        commandList.Begin();

        var colorAttachment = RenderPassColorAttachment.Clear(swapchainTexture, new Float4(0.2f, 0.2f, 0.3f, 1.0f));
        commandList.BeginRenderPass(RenderPassDescriptor.SingleColor(colorAttachment));

        commandList.SetViewport(0, 0, _device.SwapchainWidth, _device.SwapchainHeight);

        commandList.SetPipeline(_pipeline);
        commandList.SetBindGroup(0, _bindGroup!);
        commandList.SetVertexBuffer(0, _vertexBuffer!);
        commandList.SetIndexBuffer(_indexBuffer!, IndexFormat.Uint16);

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
