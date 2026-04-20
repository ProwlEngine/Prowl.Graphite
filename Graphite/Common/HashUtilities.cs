using System;
using System.Collections.Generic;


namespace Prowl.Graphite;


public static class HashUtilities
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
}
