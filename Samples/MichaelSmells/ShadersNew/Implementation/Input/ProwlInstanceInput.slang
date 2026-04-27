import VertexInput;


module ProwlInstanceInput;


public struct ProwlInstanceInput : IInstanceInput
{
    float4 _row0 : PROWL_INSTANCE0;
    float4 _row1 : PROWL_INSTANCE1;
    float4 _row2 : PROWL_INSTANCE2;
    float4 _row3 : PROWL_INSTANCE3;

    float4 _instanceColor : PROWL_INSTANCECOLOR;
    float4 _customData : PROWL_CUSTOMINSTANCE;

    public float4 CustomData() { return _customData; }
    public float4 InstanceColor() { return _instanceColor; }

    // Generics allow this to compile to a static branch
    public float4 Row<int index>()
    {
        switch (index)
        {
            case 1: return _row1;
            case 2: return _row2;
            case 3: return _row3;
            default: return _row0;
        }
    }
}
