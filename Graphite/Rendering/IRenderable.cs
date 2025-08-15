using System;

using Prowl.Vector;


namespace Prowl.Graphite;


public interface IRenderable
{
    // Renderable is expected to set its uniforms like position and color on the material.
    // Renderer handles lighting and shadow information.
    public void GetRenderInformation(Renderer renderer, out Mesh mesh, out Material material, out Bounds cullingBounds);
}
