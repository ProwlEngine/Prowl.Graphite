// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;


namespace Prowl.Graphite;


public struct ShaderPassDescription
{
    public Dictionary<string, string>? Tags;

    public Dictionary<string, HashSet<string>>? Keywords;


    private static void SetDefault<T>(ref T? currentValue, T? defaultValue)
    {
        if (currentValue == null && defaultValue != null)
            currentValue = defaultValue;
    }

    public void ApplyDefaults(ShaderPassDescription defaults)
    {
        SetDefault(ref Tags, defaults.Tags);
        SetDefault(ref Keywords, defaults.Keywords);
    }
}


public sealed class ShaderPass
{
    public string Name;
    public Dictionary<string, string> Tags;
    public ShaderVariant ShaderData;


    public ShaderPass(string name, ShaderPassDescription description, ShaderVariant[] variants)
    {
        _name = name;
        _tags = description.Tags ?? [];
        _keywords = description.Keywords ?? [];
        _variants = [];

        foreach (ShaderVariant variant in variants)
        {
            _variants[variant.VariantKeywords] = variant;
        }
    }

    public ShaderVariant GetVariant(KeywordState? keywordID = null)
        => _variants[keywordID != null ? ValidateKeyword(keywordID) : KeywordState.Empty];

    public bool TryGetVariant(KeywordState? keywordID, out ShaderVariant? variant)
        => _variants.TryGetValue(keywordID ?? KeywordState.Empty, out variant);

    public bool HasTag(string tag, string? tagValue = null)
    {
        if (_tags.TryGetValue(tag, out string value))
            return tagValue == null || value == tagValue;

        return false;
    }

    public KeywordState ValidateKeyword(KeywordState key)
    {
        KeywordState combinedKey = new();

        foreach (KeyValuePair<string, HashSet<string>> definition in _keywords)
        {
            string defaultValue = definition.Value.First();
            string value = key.GetKey(definition.Key, defaultValue);
            value = definition.Value.Contains(value) ? value : defaultValue;

            combinedKey.SetKey(definition.Key, value);
        }

        return combinedKey;
    }


    [SerializeField, HideInInspector]
    private string[] _serializedKeywordKeys;

    [SerializeField, HideInInspector]
    private string[][] _serializedKeywordValues;


    [SerializeField, HideInInspector]
    private ShaderVariant[] _serializedVariants;
}
