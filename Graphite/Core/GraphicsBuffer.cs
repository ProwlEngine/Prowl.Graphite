using System;

using Prowl.Vector;


namespace Prowl.Graphite;


public abstract class GraphicsBuffer : IDisposable
{
    public int Count { get; private set; }

    public string Name { get; set; }

    public int Stride { get; private set; }

    public abstract void Dispose();
}
