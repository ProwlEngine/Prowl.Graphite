// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Prowl.Graphite;


public readonly record struct VertexInputDescriptor(string semantic, VertexInputFormat format) : IEquatable<VertexInputDescriptor>
{
    private static Interner<string, uint> s_semanticInterner = new((x) => x + 1);

    public readonly string Semantic = semantic;
    public readonly uint SemanticID = s_semanticInterner.GetInternedValue(semantic);
    public readonly VertexInputFormat Format = format;
}
