using System;

namespace Prowl.Graphite;


public readonly record struct TextureFormat(GraphicsFormat format)
{
    public static implicit operator GraphicsFormat(TextureFormat format) => format.format;

    // -------------------- 8-bit --------------------

    public static class R8
    {
        public static readonly TextureFormat SRGB = new(GraphicsFormat.R8_SRGB);
        public static readonly TextureFormat UNorm = new(GraphicsFormat.R8_UNorm);
    }

    public static class RG8
    {
        public static readonly TextureFormat SRGB = new(GraphicsFormat.R8G8_SRGB);
        public static readonly TextureFormat UNorm = new(GraphicsFormat.R8G8_UNorm);
    }

    public static class RGB8
    {
        public static readonly TextureFormat SRGB = new(GraphicsFormat.R8G8B8_SRGB);
        public static readonly TextureFormat UNorm = new(GraphicsFormat.R8G8B8_UNorm);
    }

    public static class RGBA8
    {
        public static readonly TextureFormat SRGB = new(GraphicsFormat.R8G8B8A8_SRGB);
        public static readonly TextureFormat UNorm = new(GraphicsFormat.R8G8B8A8_UNorm);
    }

    public static class BGRA8
    {
        public static readonly TextureFormat SRGB = new(GraphicsFormat.B8G8R8A8_SRGB);
        public static readonly TextureFormat UNorm = new(GraphicsFormat.B8G8R8A8_UNorm);
    }


    // -------------------- 16-bit --------------------

    public static class R16
    {
        public static readonly TextureFormat Float = new(GraphicsFormat.R16_SFloat);
        public static readonly TextureFormat UNorm = new(GraphicsFormat.R16_UNorm);
    }

    public static class RG16
    {
        public static readonly TextureFormat Float = new(GraphicsFormat.R16G16_SFloat);
        public static readonly TextureFormat UNorm = new(GraphicsFormat.R16G16_UNorm);
    }

    public static class RGBA16
    {
        public static readonly TextureFormat Float = new(GraphicsFormat.R16G16B16A16_SFloat);
        public static readonly TextureFormat UNorm = new(GraphicsFormat.R16G16B16A16_UNorm);
    }

    public static class D16
    {
        public static readonly TextureFormat UNorm = new(GraphicsFormat.D16_UNorm);
    }


    // -------------------- 32-bit --------------------

    public static class R32
    {
        public static readonly TextureFormat Float = new(GraphicsFormat.R32_SFloat);
    }

    public static class RG32
    {
        public static readonly TextureFormat Float = new(GraphicsFormat.R32G32_SFloat);
    }

    public static class RGBA32
    {
        public static readonly TextureFormat Float = new(GraphicsFormat.R32G32B32A32_SFloat);
    }

    public static class D24S8
    {
        public static readonly TextureFormat UNorm_UInt = new(GraphicsFormat.D24_UNorm_S8_UInt);
    }

    public static class D32
    {
        public static readonly TextureFormat SFloat = new(GraphicsFormat.D32_SFloat);
    }

    public static class D32S8
    {
        public static readonly TextureFormat SFloat_UInt = new(GraphicsFormat.D32_SFloat_S8_UInt);
    }
}
