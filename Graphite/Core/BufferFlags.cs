using System;


namespace Prowl.Graphite;


[Flags]
public enum BufferTarget : ushort
{
    Vertex,
    Index,
    Structured,
    Uniform,
}


[Flags]
public enum BufferUsage
{
    None,
    MapForWrite
}


public enum MapState
{
    NotMappable,
    Mapped,
    Unmapped
}
