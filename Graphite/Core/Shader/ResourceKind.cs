namespace Prowl.Graphite;


public enum ResourceKind : byte
{
    UniformBuffer,
    StructuredBufferReadOnly,
    StructuredBufferReadWrite,
    TextureReadOnly,
    TextureReadWrite,
    Sampler,
}
