using Prowl.Vector;


namespace Prowl.Graphite;


public enum ShaderPropertyType
{
    Vector,
    Matrix,
    Texture
}


public struct ShaderProperty
{
    public string Name;
    public string DisplayName;

    public ShaderPropertyType PropertyType { get; private set; }

    public Float4 Value;
    public Float4x4 MatrixValue;

    public Texture TextureValue;


    public void Set(ShaderProperty other)
    {
        if (other.PropertyType != PropertyType)
            return;

        Value = other.Value;
        MatrixValue = other.MatrixValue;
        TextureValue = other.TextureValue;
    }


    public ShaderProperty(Float4 value)
    {
        Value = value;
        PropertyType = ShaderPropertyType.Vector;
    }


    public static implicit operator ShaderProperty(Float4 value)
        => new ShaderProperty(value);

    public static implicit operator Float4(ShaderProperty value)
        => value.Value;

    public ShaderProperty(Float4x4 value)
    {
        MatrixValue = value;
        PropertyType = ShaderPropertyType.Matrix;
    }


    public static implicit operator ShaderProperty(Float4x4 value)
        => new ShaderProperty(value);

    public static implicit operator Float4x4(ShaderProperty value)
        => value.MatrixValue;


    public ShaderProperty(Texture value)
    {
        TextureValue = value;
        PropertyType = ShaderPropertyType.Texture;
    }


    public static implicit operator ShaderProperty(Texture value)
        => new ShaderProperty(value);

    public static implicit operator Texture(ShaderProperty value)
        => value.TextureValue;
}
