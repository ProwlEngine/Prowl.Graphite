using System;
using Prowl.Graphite;
using Prowl.Vector;


public class Program
{
    const string ShaderSource = """


""";

    public GraphicsDevice device;

    public Shader shader;
    public Mesh mesh;
    public Material material;
    public CommandBuffer buffer;
    public RenderTexture colorTarget;
    public RenderTexture depthTarget;


    public void Main()
    {
        device = new GraphicsDevice();

        if (!ShaderCompiler.CompileShader(ShaderSource, out shader))
            return;

        mesh = new Mesh();

        mesh.SetVertices(new List<Vector3>());
        mesh.SetIndices(new List<int>());

        material = new Material(shader);
        colorTarget = new RenderTexture(1960, 1080, RenderTextureFormat.RGBA32);
        depthTarget = new RenderTexture(1960, 1080, RenderTextureFormat.R8);

        buffer = new CommandBuffer("Main Buffer");

        while (true)
        {
            buffer.Clear();

            buffer.SetRenderTarget(colorTarget, depthTarget);
            buffer.ClearRenderTarget(Color.Black, 1.0);

            material.SetVector("Color", new Vector4(0.5, 0.5, 1.0, 1.0));
            material.SetMatrix("MVP", Matrix4x4.Identity);

            buffer.SetMaterial(material, 0);

            buffer.DrawMesh(mesh, material);

            device.SumbitCommands(buffer);

            device.SwapBuffers();
        }
    }
}
