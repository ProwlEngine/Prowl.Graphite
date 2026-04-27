using Prowl.Vector.Geometry;


namespace Prowl.Graphite;


public interface IRenderable
{
    // Renderable is expected to set its uniforms like position and color on the material.
    // Renderer handles lighting and shadow information.
    public void GetRenderInformation(Renderer renderer, out VertexInput input, out ParameterBlock material, out AABBFloat cullingBounds);
}
