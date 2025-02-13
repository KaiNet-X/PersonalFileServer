using System.Collections;

namespace Common;

public readonly struct DictionaryView<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    private readonly Dictionary<TKey, TValue> _dictionary;
    
    public DictionaryView(Dictionary<TKey, TValue> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        _dictionary = dictionary;
    }
    
    public TValue this[TKey key] => _dictionary[key];
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
}