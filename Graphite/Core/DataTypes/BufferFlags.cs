using System;


namespace Prowl.Graphite;


public enum BufferTarget : ushort
{
    Vertex,
    Index,
    Structured,
    Uniform
}


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
