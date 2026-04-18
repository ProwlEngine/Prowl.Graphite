using System;


namespace Prowl.Graphite;


[Flags]
public enum CullFace : byte
{
    Front = 1 << 0,
    Back = 1 << 1,
}

public enum FrontFace : byte
{
    Clockwise,
    CounterClockwise,
}

public enum BlendFactor
{
    Zero,
    One,
    SrcColor,
    OneMinusSrcColor,
    SrcAlpha,
    OneMinusSrcAlpha,
    DstAlpha,
    OneMinusDstAlpha,
    DstColor,
    OneMinusDstColor,
    SrcAlphaSaturate,
    ConstantColor,
    OneMinusConstantColor,
    ConstantAlpha,
    OneMinusConstantAlpha,
    Src1Color,
    OneMinusSrc1Color,
    Src1Alpha,
    OneMinusSrc1Alpha,
}

public enum BlendEquation
{
    Add,
    Subtract,
    ReverseSubtract,
    Min,
    Max,
}

public enum DepthFunc
{
    Disabled,
    Never,
    Less,
    Equal,
    LessEqual,
    Greater,
    NotEqual,
    GreaterEqual,
    Always,
}

public enum StencilFunc
{
    Never,
    Less,
    Equal,
    LessEqual,
    Greater,
    NotEqual,
    GreaterEqual,
    Always,
}

public enum StencilOp
{
    Keep,
    Zero,
    Replace,
    Increment,
    Decrement,
    Invert,
    IncrementWrap,
    DecrementWrap,
}

public enum LogicOp
{
    Clear,
    And,
    AndReverse,
    Copy,
    AndInverted,
    NoOp,
    Xor,
    Or,
    Nor,
    Equiv,
    Invert,
    OrReverse,
    CopyInverted,
    OrInverted,
    Nand,
    Set,
}

[Flags]
public enum ColorWriteMask
{
    None = 0,
    All = R | G | B | A,
    R = 1 << 0,
    G = 1 << 1,
    B = 1 << 2,
    A = 1 << 3,
}
