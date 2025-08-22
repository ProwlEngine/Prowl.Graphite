using System;
using System.Diagnostics;

using Silk.NET.OpenGL;


namespace Prowl.Graphite.OpenGL;


internal interface GLDeferredResource
{
    bool Created { get; }
    void CreateResource(GL gl);
    void DestroyResource(GL gl);
}
