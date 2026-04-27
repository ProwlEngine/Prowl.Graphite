module ShaderVariables;


public interface IShaderVariables
{
    public static float4x4 View();
    public static float4x4 InverseView();
    public static float4x4 Projection();
    public static float4x4 ViewProjection();
    public static float4x4 PreviousViewProj();

    // Camera parameters
    public static float3 WorldSpaceCameraPos();

    public static float4 ProjectionParams();
    public static float4 ScreenParams();
    public static float2 CameraJitter();
    public static float2 CameraPreviousJitter();

    // Time parameters
    public static float4 Time();
    public static float4 SinTime();
    public static float4 CosTime();
    public static float4 DeltaTime();


    public static float4x4 ObjectToWorld();
    public static float4x4 WorldToObject();
    public static float4x4 PrevObjectToWorld();
    public static int ObjectID();


    // For some bizzare reason ObjectToWorld is also a core method,
    // so we need to specify we want 'this' interface's ObjectToWorld
    public static float4x4 MV() { return mul(View(), this.ObjectToWorld()); }
    public static float4x4 MVP() { return mul(ViewProjection(), this.ObjectToWorld()); }
    public static float4x4 TMV() { return transpose(mul(View(), this.ObjectToWorld())); }
    public static float4x4 InverseTMV() { return transpose(mul(this.WorldToObject(), InverseView())); }
}
