// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;


namespace Prowl.Graphite;


[Flags]
public enum ShaderStages : byte
{
    None = 0,
    Vertex = 1 << 0,
    Fragment = 1 << 4,
    Compute = 1 << 5,
}
