using System;

namespace Prowl.Graphite;


/// <summary>
/// The raw possible formats of a <see cref="Texture"/>.
/// Not all formats are supported on a given platform. Check platform availability of a format with <see cref="GraphicsFormat.IsSupported()"/>
/// </summary>
public enum GraphicsFormat
{
    None,

    //  -------------- 8-bit --------------

    // SRGB
    R8_SRGB,
    R8G8_SRGB,
    R8G8B8_SRGB,
    R8G8B8A8_SRGB,

    // Unsigned Normalized
    R8_UNorm,
    R8G8_UNorm,
    R8G8B8_UNorm,
    R8G8B8A8_UNorm,

    // Signed Normalized
    R8_SNorm,
    R8G8_SNorm,
    R8G8B8A8_SNorm,

    // UInt
    R8_UInt,
    R8G8_UInt,
    R8G8B8A8_UInt,

    // SInt
    R8_SInt,
    R8G8_SInt,
    R8G8B8A8_SInt,


    // BGRA formats - only the useful ones.
    B8G8R8A8_UNorm,
    B8G8R8A8_SRGB,


    //  -------------- 16-bit --------------

    // Unsigned Normalized
    R16_UNorm,
    R16G16_UNorm,
    R16G16B16A16_UNorm,

    // Signed Normalized
    R16_SNorm,
    R16G16_SNorm,
    R16G16B16A16_SNorm,

    // UInt
    R16_UInt,
    R16G16_UInt,
    R16G16B16A16_UInt,

    // Int
    R16_SInt,
    R16G16_SInt,
    R16G16B16A16_SInt,

    // Float
    R16_SFloat,
    R16G16_SFloat,
    R16G16B16A16_SFloat,

    // Packed HDR / special
    R5G6B5_UNormPack16,
    A1R5G5B5_UNormPack16,
    A4R4G4B4_UNormPack16,

    // Depth / stencil
    D16_UNorm,

    //  -------------- 32-bit --------------

    // UInt
    R32_UInt,
    R32G32_UInt,
    R32G32B32A32_UInt,

    // Int
    R32_SInt,
    R32G32_SInt,
    R32G32B32A32_SInt,

    // Float
    R32_SFloat,
    R32G32_SFloat,
    R32G32B32A32_SFloat,

    // Packed HDR / special
    B10G11R11_UFloatPack32,
    A2B10G10R10_UNormPack32,
    A2B10G10R10_UIntPack32,

    // Depth / stencil
    D24_UNorm_S8_UInt,
    D32_SFloat,
    D32_SFloat_S8_UInt,
}
