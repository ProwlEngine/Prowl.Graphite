using System.Collections.Generic;

namespace Prowl.Graphite;

public sealed class ArrayEqualityComparer<T> : IEqualityComparer<T[]> where T : struct
{
    public bool Equals(T[]? x, T[]? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        if (x.Length != y.Length)
            return false;

        for (int i = 0; i < x.Length; i++)
        {
            if (x[i]!.Equals(y[i]))
                continue;

            return false;
        }

        return true;
    }

    public int GetHashCode(T[] obj)
    {
        ulong hash = 0x9e3779b97f4a7c15;

        for (int i = 0; i < obj.Length; i++)
        {
            ulong x = (ulong)obj[i].GetHashCode();

            x ^= x >> 30;
            x *= 0xbf58476d1ce4e5b9UL;
            x ^= x >> 27;
            x *= 0x94d049bb133111ebUL;
            x ^= x >> 31;

            hash ^= x;
            hash *= 0x9e3779b97f4a7c15;
        }

        return (int)(hash ^ (hash >> 32));
    }
}
