using System;
using System.Collections.Generic;


namespace Prowl.Graphite;


public abstract class Shader
{
    public ShaderProperty[] Properties;
    public ShaderPass[] Passes;


    public ShaderPass GetPass(int passIndex)
    {
        return Passes[passIndex];
    }

    public ShaderPass GetPass(string passName)
    {
        return Array.Find(Passes, x => x.Name == passName) ?? throw new Exception("Could not find pass with name: " + passName);
    }

    public int GetPassIndex(string passName)
    {
        return Array.FindIndex(Passes, x => x.Name == passName);
    }

    public int GetPassWithTag(string tag, string? tagValue = null)
    {
        return Array.FindIndex(Passes, x => x.HasTag(tag, tagValue));
    }

    public IEnumerable<int> GetPassesWithTag(string tag, string? tagValue = null)
    {
        for (int i = 0; i < Passes.Length; i++)
        {
            if (Passes[i].HasTag(tag, tagValue))
                yield return i;
        }
    }


    public static Shader Create(GraphicsDevice? device = null)
    {
        device ??= GraphicsDevice.Instance;

        return GraphicsDevice.Backend switch
        {
            GraphicsBackend.OpenGL => new OpenGL.GLShader()
        };
    }
}


