// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;


namespace Prowl.Graphite;


public sealed class ShaderPass
{
    public string Name;
    public Dictionary<string, string> Tags;
    public Dictionary<GraphicsBackend, ShaderData> ShaderData;


    public ShaderPass(string name, Dictionary<string, string> tags, Dictionary<GraphicsBackend, ShaderData> shaderData)
    {
        Name = name;
        Tags = tags ?? [];
        ShaderData = shaderData;
    }


    public bool HasTag(string tag, string? tagValue = null)
    {
        if (Tags.TryGetValue(tag, out string? value))
            return tagValue == null || value == tagValue;

        return false;
    }
}
