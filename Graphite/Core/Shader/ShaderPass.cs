// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;


namespace Prowl.Graphite;


public sealed class ShaderPass
{
    public readonly string Name;

    public readonly ShaderPassState State;

    private Dictionary<string, string> _tags;
    private Dictionary<GraphicsBackend, ShaderData> _shaderData;


    public ShaderPass(string name, Dictionary<string, string> tags, Dictionary<GraphicsBackend, ShaderData> shaderData)
    {
        Name = name;
        _tags = tags ?? [];
        _shaderData = shaderData;
    }


    public bool HasTag(string tag, string? tagValue = null)
    {
        if (_tags.TryGetValue(tag, out string? value))
            return tagValue == null || value == tagValue;

        return false;
    }


    public bool GetBackend(GraphicsBackend backend, out ShaderData? data)
    {
        bool hasValue = _shaderData.TryGetValue(backend, out data);

        if (data != null)
            data!.Pass = this;

        return hasValue;
    }
}
