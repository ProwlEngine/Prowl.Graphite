using System;

using Prowl.Vector;


namespace Prowl.Graphite;


public abstract class GraphicsDevice
{
    public abstract void SumbitCommands(CommandBuffer buffer);
}
