using System;
using System.Collections.Generic;

public static class DictionaryExtensions
{
    public static Dictionary<TKey, TValue> DeepClone<TKey, TValue>
        (this Dictionary<TKey, TValue> dic) where TValue : ICloneable
    {
        Dictionary<TKey, TValue> ret = new Dictionary<TKey, TValue>(dic.Count, dic.Comparer);
        foreach(KeyValuePair<TKey, TValue> entry in dic)
        {
            ret.Add(entry.Key, (TValue)entry.Value.Clone());
        }

        return ret;
    }
}