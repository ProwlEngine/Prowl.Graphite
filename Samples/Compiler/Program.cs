using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

using Prowl.Graphite;


public class Program
{
    static string sourceShader =
"""
Shader "TestShader"
{
    Properties
    {
        _Integer("Example integer", Integer) = 1
        _Color("Example color", Color) = (0.25, 0.5, 0.5, 1)
        _Float("Example float", Float) = 0.5
        _Texture("Example Texture", 2D) = "" {}
        _RedTexture("Example Red Texture", 2D) = "red" {}
        _TextureArray("Example Texture Array", 2DArray) = "" {}
        _Texture3D("Example Texture 3D", 3D) = "" {}
        _Cubemap("Example Cubemap", Cube) = "" {}
        _CubeArray("Example Cubemap Array", CubeArray) = "" {}
        _Vector("Example vector", Vector) = (0.25, 0.5, 0.5, 1)
    }

    HLSLINCLUDE
        // Some appended shader code
    ENDHLSL

    Pass 0
    {
        Name "Pass0"
        Tags { "PassID" = "Pass Zero" "IsPass0" = "True" }

        HLSLPROGRAM
            // Some of my shader code
        ENDHLSL
    }

    Pass 1
    {
        Name "Pass0"
        Tags { "PassID" = "Pass One" "IsPass1" = "True" }

        HLSLPROGRAM
            // More of my shader code
        ENDHLSL
    }

    Fallback "FallbackShader"
}
""";

    static string sourceShader2 =
"""
Shader "TestShader"
{
    Properties
    {
        _Integer("Example integer", Integer) = 1
        _Color("Example color", Color) = (0.25, 0.5, 0.5, 1)
        _Float("Example float", Float) = 0.5
        _Texture("Example Texture", 2D) = "" {}
        _RedTexture("Example Red Texture", 2D) = "red" {}
        _TextureArray("Example Texture Array", 2DArray) = "" {}
        _Texture3D("Example Texture 3D", 3D) = "" {}
        _Cubemap("Example Cubemap", Cube) = "" {}
        _CubeArray("Example Cubemap Array", CubeArray) = "" {}
        _Vector("Example vector", Vector) = (0.25, 0.5, 0.5, 1)
    }

    HLSLINCLUDE
        // Some appended shader code
    ENDHLSL

    Pass 0
    {
        Name "Pass0"
        Tags { "PassID" = "Pass Zero" "IsPass0" = "True" }

        AlphaToMask Off/On
        Blend Some/Blend/Values
        BlendOp Some/Blend/Operation
        ColorMask Some/Channels
        Conservative Is/Donald/Trump
        Cull Back/Front
        Offset Factor/Units
        ZClip On/Off
        ZTest Operation
        ZWrite On/Off

        Stencil
        {
            Ref <ref>
            ReadMask <readMask>
            WriteMask <writeMask>
            Comp <comparisonOperation>
            Pass <passOperation>
            Fail <failOperation>
            ZFail <zFailOperation>
            CompBack <comparisonOperationBack>
            PassBack <passOperationBack>
            FailBack <failOperationBack>
            ZFailBack <zFailOperationBack>
            CompFront <comparisonOperationFront>
            PassFront <passOperationFront>
            FailFront <failOperationFront>
            ZFailFront <zFailOperationFront>
        }

        HLSLPROGRAM
            // Some of my shader code
        ENDHLSL
    }

    Pass 1
    {
        Name "Pass0"
        Tags { "PassID" = "Pass One" "IsPass1" = "True" }

        HLSLPROGRAM
            // More of my shader code
        ENDHLSL
    }

    Fallback "FallbackShader"
}
""";

    public static void Main()
    {
        ParsedShader parsed = ShaderParser.Parse(sourceShader);

        Console.WriteLine(JsonSerializer.Serialize(parsed, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IncludeFields = true,
        }));
    }
}
