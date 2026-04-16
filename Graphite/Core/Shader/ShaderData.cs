using System;
using System.Collections.Generic;
using System.Threading;


namespace Prowl.Graphite;

/// <summary>
/// Represents raw shader data for a specific graphics backend.
/// <list type="bullet">
/// <item>
///     <description>For OpenGL, this is GLSL shader source code.</description>
/// </item>
/// <item>
///     <description>For DirectX11, this is HLSL shader source code.</description>
/// </item>
/// <item>
///     <description>For DirectX12, this is raw DXIL shader IL.</description>
/// </item>
/// <item>
///     <description>For Vulkan, this is raw SPIR-V.</description>
/// </item>
/// <item>
///     <description>For Metal, this is MSL shader source code.</description>
/// </item>
/// </list>
/// </summary>
public abstract class ShaderData
{
    private static uint s_nextShaderId = 1;

    private uint _shaderID = 0;

    private ShaderPass? _pass;

    public ShaderPass Pass
    {
        get
        {
            if (_pass == null)
                throw new NullReferenceException("Shader Data is missing pass information");

            return _pass;
        }

        internal set => _pass = value;
    }


    public uint GetShaderID()
    {
        if (_shaderID == 0)
            _shaderID = Interlocked.Increment(ref s_nextShaderId);

        return _shaderID;
    }
}
