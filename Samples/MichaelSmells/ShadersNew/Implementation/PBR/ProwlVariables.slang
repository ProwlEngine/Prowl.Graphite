import ShaderVariables;


module ProwlVariables;


// Global uniforms (set per frame)
struct GlobalUniforms
{
    // Camera matrices
    float4x4 prowl_View;
    float4x4 prowl_InverseView;
    float4x4 prowl_Projection;
    float4x4 prowl_ViewProjection;
    float4x4 prowl_PreviousViewProj;

    // Camera parameters
    float3 prowl_WorldSpaceCameraPos;

    float4 prowl_ProjectionParams;
    float4 prowl_ScreenParams;
    float2 prowl_CameraJitter;
    float2 prowl_CameraPreviousJitter;

    // Time parameters
    float4 prowl_Time;
    float4 prowl_SinTime;
    float4 prowl_CosTime;
    float4 prowl_DeltaTime;
}
ParameterBlock<GlobalUniforms> Globals;


// Per-object uniforms (set per draw call)
struct ObjectUniforms
{
    float4x4 prowl_ObjectToWorld;
    float4x4 prowl_WorldToObject;
    float4x4 prowl_PrevObjectToWorld;
    int prowl_ObjectID;
}
ParameterBlock<ObjectUniforms> Object;



public struct ProwlVariables : IShaderVariables
{
    public static float4x4 View() { return Globals.prowl_View; }
    public static float4x4 InverseView() { return Globals.prowl_InverseView; }
    public static float4x4 Projection() { return Globals.prowl_Projection; }
    public static float4x4 ViewProjection() { { return Globals.prowl_ViewProjection; }}
    public static float4x4 PreviousViewProj() { { return Globals.prowl_PreviousViewProj; }}

    // Camera parameters
    public static float3 WorldSpaceCameraPos() { return Globals.prowl_WorldSpaceCameraPos; }

    public static float4 ProjectionParams() { { return Globals.prowl_ProjectionParams; }}
    public static float4 ScreenParams() { { return Globals.prowl_ScreenParams; }}
    public static float2 CameraJitter() { { return Globals.prowl_CameraJitter; }}
    public static float2 CameraPreviousJitter() { { return Globals.prowl_CameraPreviousJitter; }}

    // Time parameters
    public static float4 Time() { return Globals.prowl_Time; }
    public static float4 SinTime() { return Globals.prowl_SinTime; }
    public static float4 CosTime() { { return Globals.prowl_CosTime; }}
    public static float4 DeltaTime() { { return Globals.prowl_DeltaTime; }}


    public static float4x4 ObjectToWorld() { return Object.prowl_ObjectToWorld; }
    public static float4x4 WorldToObject() { return Object.prowl_WorldToObject; }
    public static float4x4 PrevObjectToWorld() { return Object.prowl_ObjectToWorld; }
    public static int ObjectID() { return Object.prowl_ObjectID; }
}
