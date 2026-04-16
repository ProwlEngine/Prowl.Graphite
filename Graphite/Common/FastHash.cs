using System;

namespace Prowl.Graphite;


public static unsafe class FastHash64
{
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
