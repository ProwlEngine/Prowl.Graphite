using System.Collections.Generic;

using Prowl.Vector;


namespace Prowl.Graphite;


public abstract class Material
{
    public static Material Create(Shader shader)
    {
        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLMaterial(shader)
        };
    }


    public Shader Shader { get; set; }

    internal Dictionary<string, object> _properties;


    private void SetProperty(string name, object value)
    {
        if (Shader.Properties.Contains(name))
            _properties[name] = value;
    }


    public void SetFloat(string name, float value) => SetProperty(name, value);
    public void SetInt(string name, int value) => SetProperty(name, value);
    public void SetVector(string name, Float3 value) => SetProperty(name, value);
    public void SetMatrix(string name, Float4x4 value) => SetProperty(name, value);
    public void SetTexture(string name, Texture value) => SetProperty(name, value);
    public void SetBuffer(string name, GraphicsBuffer value) => SetProperty(name, value);
}
