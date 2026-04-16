using System;
using System.Collections.Generic;

namespace Prowl.Graphite;


public class Interner<T, T2> where T : notnull where T2 : unmanaged
{
    private Dictionary<T, T2> _internDictionary;
    private T2 _currentValue;

    private Func<T2, T2> _increment;


    public Interner(Func<T2, T2> increment)
    {
        _internDictionary = [];
        _currentValue = default;
        _increment = increment;
    }


    public Interner(Func<T2, T2> increment, IEqualityComparer<T> comparer)
    {
        _internDictionary = new(comparer);
        _currentValue = default;
        _increment = increment;
    }


    public T2 GetInternedValue(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (_internDictionary.TryGetValue(value, out T2 result))
            return result;

        return _internDictionary[value] = _currentValue = _increment.Invoke(_currentValue);
    }
}
