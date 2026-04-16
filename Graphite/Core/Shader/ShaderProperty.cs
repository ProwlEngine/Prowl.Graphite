using System.Text.Json.Serialization;

using Prowl.Vector;


namespace Prowl.Graphite;


public enum ShaderPropertyType
{
    Integer,
    Float,
    Vector,
    Color,
    Matrix,
    Texture2D,
    Texture3D,
    Texture2DArray,
    TextureCubemap,
    TextureCubemapArray
}


public struct ShaderProperty
{
    public string Name;
    public string DisplayName;

    public ShaderPropertyType PropertyType;

    [JsonIgnore]
    public Float4 Value;

    [JsonIgnore]
    public Float4x4 MatrixValue;

    public string TextureValue;


    public void Set(ShaderProperty other)
    {
        if (other.PropertyType != PropertyType)
            return;

        Value = other.Value;
        MatrixValue = other.MatrixValue;
        TextureValue = other.TextureValue;
    }


    public static implicit operator ShaderProperty(float value)
        => new() { PropertyType = ShaderPropertyType.Float, Value = new(value) };

    public static implicit operator ShaderProperty(Float2 value)
        => new() { PropertyType = ShaderPropertyType.Float, Value = new(value) };

    public static implicit operator ShaderProperty(Float3 value)
        => new() { PropertyType = ShaderPropertyType.Float, Value = new(value) };

    public static implicit operator ShaderProperty(Float4 value)
        => new() { PropertyType = ShaderPropertyType.Float, Value = new(value) };

    public static implicit operator ShaderProperty(Float4x4 value)
        => new() { PropertyType = ShaderPropertyType.Float, MatrixValue = value };

    public static implicit operator ShaderProperty(string value)
        => new() { PropertyType = ShaderPropertyType.Float, TextureValue = value };
}
