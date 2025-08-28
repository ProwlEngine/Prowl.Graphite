// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

namespace Prowl.Graphite;


public readonly record struct VertexInputDescriptor(string semantic, VertexInputFormat format) : IEquatable<VertexInputDescriptor>
{
    public readonly string Semantic = semantic;
    public readonly VertexInputFormat Format = format;
}
