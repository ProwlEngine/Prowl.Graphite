using System;
using System.Collections.Generic;
using System.Text;

using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


public class GLShaderData : ShaderData
{
    public struct ShaderStage
    {
        public ShaderStages Stage;
        public string Source;
    }


    private VertexInputDescriptor[] _vertexInputs;
    private ShaderStage[] _stages;
    private Program _program;


    public VertexInputDescriptor[] VertexInputs => _vertexInputs;


    public GLShaderData(VertexInputDescriptor[] vertexInputs, ShaderStage[] stages)
    {
        _vertexInputs = vertexInputs;
        _stages = stages;
    }


    internal Program GetProgram(GL gl)
    {
        if (_program.Handle != 0)
            return _program;

        _program = new Program(gl.CreateProgram());

        // stackalloc for the shit of it - an array allocation is NOT where we would be losing performance
        Span<Silk.NET.OpenGL.Shader> shaders = stackalloc Silk.NET.OpenGL.Shader[_stages.Length];

        try
        {
            for (int i = 0; i < _stages.Length; i++)
            {
                ShaderStage stage = _stages[i];

                ShaderType shaderType = ToGLShaderType(stage.Stage);
                Silk.NET.OpenGL.Shader shader = new(gl.CreateShader(shaderType));

                gl.ShaderSource(shader.Handle, stage.Source);
                gl.CompileShader(shader.Handle);

                gl.GetShader(shader.Handle, ShaderParameterName.CompileStatus, out int success);
                if (success == 0)
                {
                    string log = gl.GetShaderInfoLog(shader.Handle);
                    throw new Exception($"Runtime OpenGL shader compile error ({stage.Stage}):\n{log}");
                }

                gl.AttachShader(_program.Handle, shader.Handle);
                shaders[i] = shader;
            }

            gl.LinkProgram(_program.Handle);
            gl.GetProgram(_program.Handle, ProgramPropertyARB.LinkStatus, out int linkStatus);

            if (linkStatus == 0)
            {
                string log = gl.GetProgramInfoLog(_program.Handle);
                throw new Exception($"Runtime OpenGL shader link error:\n{log}");
            }
        }
        finally
        {
            for (int i = 0; i < _stages.Length; i++)
            {
                gl.DetachShader(_program.Handle, shaders[i].Handle);
                gl.DeleteShader(shaders[i].Handle);
            }
        }

        return _program;
    }


    private static ShaderType ToGLShaderType(ShaderStages stage)
    {
        return stage switch
        {
            ShaderStages.Vertex => ShaderType.VertexShader,
            ShaderStages.Fragment => ShaderType.FragmentShader,
            ShaderStages.Compute => ShaderType.ComputeShader,
            _ => throw new NotSupportedException($"Unsupported stage: {stage}")
        };
    }
}
