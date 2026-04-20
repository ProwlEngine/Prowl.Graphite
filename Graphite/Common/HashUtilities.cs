using System;
using System.Collections.Generic;


namespace Prowl.Graphite;


public static unsafe class HashUtilities
{
    // From https://stackoverflow.com/questions/670063/getting-hash-of-a-list-of-strings-regardless-of-order
    public static int OrderlessHash<T>(IEnumerable<T> source, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;

        int hash = 0;
        int curHash;

        var valueCounts = new Dictionary<T, int>();

        foreach (T? element in source)
        {
            curHash = comparer.GetHashCode(element);

            if (valueCounts.TryGetValue(element, out int bitOffset))
                valueCounts[element] = bitOffset + 1;
            else
                valueCounts.Add(element, bitOffset);

            hash = unchecked(hash + (curHash << bitOffset | curHash >> 32 - bitOffset) * 37);
        }

        return hash;
    }


    public static ulong Hash<T>(ReadOnlySpan<T> values, delegate*<T, ulong> selector)
    {
        ulong hash = 0x9e3779b97f4a7c15;

        // Splitmix hash
        for (int i = 0; i < values.Length; i++)
        {
            ulong x = selector(values[i]);

            x ^= x >> 30;
            x *= 0xbf58476d1ce4e5b9UL;
            x ^= x >> 27;
            x *= 0x94d049bb133111ebUL;
            x ^= x >> 31;

            hash ^= x;
            hash *= 0x9e3779b97f4a7c15;
        }

        return hash;
    }

    public static ulong Hash(ReadOnlySpan<ulong> values)
    {
        return Hash(values, &Identity);
    }

    public static ulong Identity(ulong v) => v;
}
