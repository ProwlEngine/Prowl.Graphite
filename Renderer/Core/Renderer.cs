using System;


namespace Prowl.Graphite;


public abstract class Renderer
{
    private static Renderer s_current;
    public static Renderer Current
    {
        get => s_current;

        set
        {
            s_current = value;

            if (OnCurrentRendererChanged != null)
                OnCurrentRendererChanged.Invoke();
        }
    }


    public static Action OnCurrentRendererChanged;


    public abstract void SubmitRenderable(IRenderable renderable);

    public abstract void DrawMesh(Mesh mesh, Material material);

    public abstract void ImmediateDrawMesh(Mesh mesh, Material material);

    public abstract void Render();
}
