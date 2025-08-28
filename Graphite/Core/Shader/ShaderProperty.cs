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

    public ShaderPropertyType PropertyType;

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
}
