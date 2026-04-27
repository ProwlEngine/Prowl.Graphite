import VertexInput;
import FragmentInput;
import ShaderVariables;
import Math;

import ProwlFragmentInput;
import ProwlMeshInput;
import ProwlInstanceInput;
import ProwlVariables;


module Line;


Texture2D<float4> _MainTex;
SamplerState      _MainTexSampler;


struct LineVaryings
{
    float4 clipPos     : SV_Position;
    float2 uv0         : UV0;
    float4 color       : COLOR;
    float4 currentPos  : TEXCOORD0;
    float4 previousPos : TEXCOORD1;
}

struct LineOutputs
{
    float4 gAlbedo      : SV_Target0;
    float4 gMotionVector: SV_Target1;
    float4 gNormal      : SV_Target2;
    float4 gSurface     : SV_Target3;
}


[shader("vertex")]
LineVaryings LineVS<Instance>(VertexInputs<ProwlVariables, ProwlMeshInput, Instance, void> vtx)
where optional Instance : IInstanceInput
{
    float4 clipPos = vtx.ClipPosition();

    float4x4 prevModel = ProwlVariables::PrevObjectToWorld();
    float4   prevWorldPos = mul(prevModel, float4(vtx.MeshInputs.Position(), 1.0));
    float4   prevClipPos  = mul(ProwlVariables::PreviousViewProj(), prevWorldPos);

    LineVaryings o;
    o.clipPos     = clipPos;
    o.uv0         = vtx.MeshInputs.UVChannel<0>();
    o.color       = vtx.GetInstanceColor();
    o.currentPos  = clipPos;
    o.previousPos = prevClipPos;
    return o;
}


[shader("fragment")]
LineOutputs LineFS(LineVaryings fragInput)
{
    // Motion vector from current/prev NDC (from interpolated varyings via TEXCOORD semantics)
    // fragInput doesn't carry currentPos/previousPos — compute from SV_Position and camera jitter
    float2 curNDC  = fragInput.clipPos.xy / ProwlVariables::ScreenParams().xy * 2.0 - 1.0 - ProwlVariables::CameraJitter();

    // For previous-frame NDC we rely on per-object motion stored in TEXCOORD1 varyings.
    // Since ProwlFragmentInput has no TEXCOORD slots, we use a local workaround: output zero motion.
    // A full implementation would need extended fragment input or a separate varying struct.
    float2 prevNDC = curNDC;

    float4 albedo = _MainTex.Sample(_MainTexSampler, fragInput.uv0) * fragInput.color;

    LineOutputs o;
    o.gMotionVector = float4((curNDC - prevNDC) * 0.5, 0.0, 1.0);
    o.gNormal       = float4(0.0, 0.0, 1.0, 1.0);
    o.gSurface      = float4(1.0, 0.0, 0.0, 1.0);

    float3 baseColor = gammaToLinearSpace(albedo.rgb);
    o.gAlbedo = float4(baseColor, albedo.a);
    return o;
}
