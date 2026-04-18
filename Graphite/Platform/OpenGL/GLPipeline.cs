using System;
using System.Collections.Generic;

using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal class GLPipeline
{
    private static Dictionary<uint, GLPipeline> s_pipelineCache = [];


    private GLShaderData _shaderData;

    private ShaderPassState _renderState;




    private GLPipeline(GLShaderData shaderData)
    {
        _shaderData = shaderData;
        _renderState = shaderData.Pass.State;
    }


    public void ApplyRenderState(GLDispatcher dispatcher, GL gl)
    {
        GLPipeline? previous = dispatcher.ActivePipeline;

        if (previous == this)
            return;

        dispatcher.ActivePipeline = this;

        ShaderPassState prev = previous?._renderState ?? default;
        bool noPrev = previous == null;

        // Program
        if (noPrev || previous!._shaderData != _shaderData)
            gl.UseProgram(_shaderData.GetProgram(gl).Handle);

        // ---- Culling ----
        if (noPrev || prev.EnableCulling != _renderState.EnableCulling)
        {
            if (_renderState.EnableCulling) gl.Enable(EnableCap.CullFace);
            else gl.Disable(EnableCap.CullFace);
        }

        if (_renderState.EnableCulling)
        {
            if (noPrev || prev.CullFace != _renderState.CullFace)
                gl.CullFace(_renderState.CullFace.ToGLCullFace());
            if (noPrev || prev.FrontFace != _renderState.FrontFace)
                gl.FrontFace(_renderState.FrontFace.ToGLFrontFace());
        }

        // ---- Polygon Offset ----
        if (noPrev || prev.EnablePolygonOffsetFill != _renderState.EnablePolygonOffsetFill)
        {
            if (_renderState.EnablePolygonOffsetFill) gl.Enable(EnableCap.PolygonOffsetFill);
            else gl.Disable(EnableCap.PolygonOffsetFill);
        }

        if (_renderState.EnablePolygonOffsetFill)
        {
            if (noPrev || prev.PolygonOffsetFactor != _renderState.PolygonOffsetFactor || prev.PolygonOffsetUnits != _renderState.PolygonOffsetUnits)
                gl.PolygonOffset(_renderState.PolygonOffsetFactor, _renderState.PolygonOffsetUnits);
        }

        // ---- Depth ----
        if (noPrev || prev.EnableDepthTest != _renderState.EnableDepthTest)
        {
            if (_renderState.EnableDepthTest) gl.Enable(EnableCap.DepthTest);
            else gl.Disable(EnableCap.DepthTest);
        }

        if (_renderState.EnableDepthTest)
        {
            if (noPrev || prev.DepthFunc != _renderState.DepthFunc)
                gl.DepthFunc(_renderState.DepthFunc.ToGLDepthFunc());
            if (noPrev || prev.DepthWriteMask != _renderState.DepthWriteMask)
                gl.DepthMask(_renderState.DepthWriteMask);
        }

        if (noPrev || prev.EnableDepthClamp != _renderState.EnableDepthClamp)
        {
            if (_renderState.EnableDepthClamp) gl.Enable(EnableCap.DepthClamp);
            else gl.Disable(EnableCap.DepthClamp);
        }

        // ---- Stencil ----
        if (noPrev || prev.EnableStencilTest != _renderState.EnableStencilTest)
        {
            if (_renderState.EnableStencilTest) gl.Enable(EnableCap.StencilTest);
            else gl.Disable(EnableCap.StencilTest);
        }

        if (_renderState.EnableStencilTest)
        {
            if (noPrev || prev.StencilFrontFunc != _renderState.StencilFrontFunc || prev.StencilFrontRef != _renderState.StencilFrontRef || prev.StencilFrontReadMask != _renderState.StencilFrontReadMask)
                gl.StencilFuncSeparate(GLEnum.Front, _renderState.StencilFrontFunc.ToGLStencilFunc(), _renderState.StencilFrontRef, _renderState.StencilFrontReadMask);
            if (noPrev || prev.StencilFrontFailOp != _renderState.StencilFrontFailOp || prev.StencilFrontDepthFailOp != _renderState.StencilFrontDepthFailOp || prev.StencilFrontPassOp != _renderState.StencilFrontPassOp)
                gl.StencilOpSeparate(GLEnum.Front, _renderState.StencilFrontFailOp.ToGLStencilOp(), _renderState.StencilFrontDepthFailOp.ToGLStencilOp(), _renderState.StencilFrontPassOp.ToGLStencilOp());
            if (noPrev || prev.StencilFrontWriteMask != _renderState.StencilFrontWriteMask)
                gl.StencilMaskSeparate(GLEnum.Front, _renderState.StencilFrontWriteMask);

            if (noPrev || prev.StencilBackFunc != _renderState.StencilBackFunc || prev.StencilBackRef != _renderState.StencilBackRef || prev.StencilBackReadMask != _renderState.StencilBackReadMask)
                gl.StencilFuncSeparate(GLEnum.Back, _renderState.StencilBackFunc.ToGLStencilFunc(), _renderState.StencilBackRef, _renderState.StencilBackReadMask);
            if (noPrev || prev.StencilBackFailOp != _renderState.StencilBackFailOp || prev.StencilBackDepthFailOp != _renderState.StencilBackDepthFailOp || prev.StencilBackPassOp != _renderState.StencilBackPassOp)
                gl.StencilOpSeparate(GLEnum.Back, _renderState.StencilBackFailOp.ToGLStencilOp(), _renderState.StencilBackDepthFailOp.ToGLStencilOp(), _renderState.StencilBackPassOp.ToGLStencilOp());
            if (noPrev || prev.StencilBackWriteMask != _renderState.StencilBackWriteMask)
                gl.StencilMaskSeparate(GLEnum.Back, _renderState.StencilBackWriteMask);
        }

        if (noPrev || prev.EnableBlend != _renderState.EnableBlend)
        {
            if (_renderState.EnableBlend) gl.Enable(EnableCap.Blend);
            else gl.Disable(EnableCap.Blend);
        }

        // ---- Blending ----

        if (_renderState.EnableBlend)
        {
            if (noPrev || prev.BlendEquationRgb != _renderState.BlendEquationRgb || prev.BlendEquationAlpha != _renderState.BlendEquationAlpha)
                gl.BlendEquationSeparate(_renderState.BlendEquationRgb.ToGLBlendEquation(), _renderState.BlendEquationAlpha.ToGLBlendEquation());
            if (noPrev || prev.BlendSrcRgb != _renderState.BlendSrcRgb || prev.BlendDstRgb != _renderState.BlendDstRgb || prev.BlendSrcAlpha != _renderState.BlendSrcAlpha || prev.BlendDstAlpha != _renderState.BlendDstAlpha)
                gl.BlendFuncSeparate(_renderState.BlendSrcRgb.ToGLBlendFactor(), _renderState.BlendDstRgb.ToGLBlendFactor(), _renderState.BlendSrcAlpha.ToGLBlendFactor(), _renderState.BlendDstAlpha.ToGLBlendFactor());
        }

        // ---- Multisampling ----
        if (noPrev || prev.AlphaToMask != _renderState.AlphaToMask)
        {
            if (_renderState.AlphaToMask) gl.Enable(EnableCap.SampleAlphaToCoverage);
            else gl.Disable(EnableCap.SampleAlphaToCoverage);
        }

        // ---- Color Write Mask ----
        if (noPrev || prev.WriteMask != _renderState.WriteMask)
            gl.ColorMask(
                _renderState.WriteMask.HasFlag(ColorWriteMask.R),
                _renderState.WriteMask.HasFlag(ColorWriteMask.G),
                _renderState.WriteMask.HasFlag(ColorWriteMask.B),
                _renderState.WriteMask.HasFlag(ColorWriteMask.A));
    }


    public void BindAttributes(GL gl, GLMesh mesh)
    {
        VertexInputDescriptor[] shaderInputs = _shaderData.VertexInputs;

        if (mesh.BuffersBoundLegacy)
        {
            BindAttributesLegacy(gl, mesh);
            return;
        }

        VertexArray vertexArray = mesh.VertexArray;
        VertexInputDescriptor[] meshInputs = mesh.InputLayout;

        for (uint i = 0; i < meshInputs.Length; i++)
        {
            VertexInputDescriptor descriptor = meshInputs[i];

            uint attribIndex = 0;
            for (; attribIndex < shaderInputs.Length; attribIndex++)
                if (shaderInputs[attribIndex].SemanticID == descriptor.SemanticID) break;

            // No binding target found
            if (attribIndex == shaderInputs.Length)
                continue;

            gl.VertexArrayAttribBinding(vertexArray.Handle, attribIndex, i);
        }
    }


    /// <summary>
    /// Old legacy binding path - recomputes VAO binding every mesh-shader pair at draw time.
    /// Slow and nasty but I couldn't care less about OpenGL 4.1 < as long as it runs.
    /// You can optimize this by drawing using instancing, which will be supported, but fuck-all for individual meshes.
    /// </summary>
    private void BindAttributesLegacy(GL gl, GLMesh mesh)
    {
        VertexInputDescriptor[] shaderInputs = _shaderData.VertexInputs;
        VertexInputDescriptor[] meshInputs = mesh.InputLayout;
        VertexArray vertexArray = mesh.VertexArray;
        GLGraphicsBuffer?[] inputBuffers = mesh.VertexInputBuffers;

        gl.BindVertexArray(vertexArray.Handle);

        for (int i = 0; i < inputBuffers.Length; i++)
        {
            if (inputBuffers[i] == null)
                continue;

            VertexInputDescriptor descriptor = meshInputs[i];

            uint attribIndex = 0;
            for (; attribIndex < shaderInputs.Length; attribIndex++)
                if (shaderInputs[attribIndex].SemanticID == descriptor.SemanticID) break;

            // No binding target found
            if (attribIndex == shaderInputs.Length)
                continue;

            gl.EnableVertexAttribArray(attribIndex);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, inputBuffers[i]!._buffer.Handle);
            gl.VertexAttribPointer(attribIndex, descriptor.Format.Dimension(), descriptor.Format.ToGLEnum(), false, (uint)descriptor.Format.Size(), 0);
        }

        gl.BindVertexArray(0);
    }


    public static GLPipeline GetPipeline(GLShaderData shaderData)
    {
        if (!s_pipelineCache.TryGetValue(shaderData.GetShaderID(), out GLPipeline? pipeline))
        {
            pipeline = new GLPipeline(shaderData);
            s_pipelineCache.Add(shaderData.GetShaderID(), pipeline);
        }

        return pipeline;
    }
}
