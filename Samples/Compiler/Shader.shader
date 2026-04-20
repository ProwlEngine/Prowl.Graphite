Shader "ExampleShader"
{
    Properties
    {
        _Integer("Example integer", Integer) = 1
        _Color("Example color", Color) = (0.25, 0.5, 0.5, 1)
        _Float("Example float", Float) = 0.5
        _Texture("Example Texture", Texture2D) = "" {} // Texture2D -> 2D
        _RedTexture("Example Red Texture", Texture2D) = "red" {}
        _TextureArray("Example Texture Array", Texture2DArray) = "" {} // Texture2DArray -> 2DArray
        _Texture3D("Example Texture 3D", Texture3D) = "" {} // Texture3D -> 3D
        _Cubemap("Example Cubemap", TextureCubemap) = "" {} // TextureCubemap -> Cube
        _CubeArray("Example Cubemap Array", TextureCubemapArray) = "" {} // TextureCubemapArray -> CubeArray
        _Vector("Example vector", Vector) = (0.25, 0.5, 0.5, 1)
    }

    HLSLINCLUDE
        // Some appended shader code
    ENDHLSL

    Pass 0
    {
        Name "Pass0"
        Tags { "PassID" = "Pass Zero" "IsPass0" = "True" }

        AlphaToMask On
        BlendOp ReverseSubtract
        ZTest Greater
        Cull Back
        ZClip On
        ZWrite Off

        Blend One One

        Stencil
        {
            Ref 20
            ReadMask 12
            WriteMask 11
            Comp Equal
            PassBack Zero
            FailFront Invert
            ZFail Invert
        }

        HLSLPROGRAM
            // This is some of my HLSL code

            #pragma vertex vert
            #pragma fragment frag


            struct appdata
            {
                float3 wpos : POSITION;
                float3 norm : NORMAL;
                float2 uv : UV0;
            };


            struct v2f
            {
                float3 clippos : POSITION;
                float3 norm : NORMAL;
                float2 uv : UV0;
            };


            int _Integer;
            float4 _Color;
            float _Float;
            float4 _Vector;

            Texture2D<half4> _Texture;
            Texture2D<half4> _RedTexture;
            Texture2DArray<half4> _TextureArray;
            Texture3D<half4> _Texture3D;
            TextureCube<half4> _Cubemap;
            TextureCubeArray<half4> _CubeArray;


            v2f vert(appdata i)
            {
                v2f o;

                o.clippos = TransformWorldToClipSpace(i.wpos);
                o.norm = i.norm;
                o.uv = i.uv;

                return o;
            }


            float4 frag(v2f i)
            {
                return _Texture.Sample(i.uv);
            }
        ENDHLSL
    }

    Pass 1
    {
        Name "Pass1"
        Tags { "PassID" = "Pass One" "IsPass1" = "True" }

        ZTest Disabled
        BlendOp Add
        ColorMask RBA
        ZClip Off
        ZWrite On
        Cull Front
        Offset 1 1

        BlendRGB One Zero
        BlendAlpha One SrcColor

        HLSLPROGRAM
            // This is some of my HLSL code

            #pragma vertex vert
            #pragma fragment frag


            struct appdata
            {
                float3 wpos : POSITION;
                float3 norm : NORMAL;
                float2 uv : UV0;
            };


            struct v2f
            {
                float3 clippos : POSITION;
                float3 norm : NORMAL;
                float2 uv : UV0;
            };


            int _Integer;
            float4 _Color;
            float _Float;
            float4 _Vector;

            Texture2D<half4> _Texture;
            Texture2D<half4> _RedTexture;
            Texture2DArray<half4> _TextureArray;
            Texture3D<half4> _Texture3D;
            TextureCube<half4> _Cubemap;
            TextureCubeArray<half4> _CubeArray;


            v2f vert(appdata i)
            {
                v2f o;

                o.clippos = TransformWorldToClipSpace(i.wpos);
                o.norm = i.norm;
                o.uv = i.uv;

                return o;
            }


            float4 frag(v2f i)
            {
                return _Texture.Sample(i.uv);
            }
        ENDHLSL
    }

    Fallback "FallbackShader"
}
