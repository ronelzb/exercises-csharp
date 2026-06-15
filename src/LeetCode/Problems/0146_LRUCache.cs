namespace LeetCode.Problems;

public class LRUCache(int capacity)
{
  private readonly LinkedList<(int key, int value)> _values = [];
  private readonly Dictionary<int, LinkedListNode<(int key, int value)>> _keyToValueMap = [];

  public int Get(int key)
  {
    if (_keyToValueMap.TryGetValue(key, out LinkedListNode<(int key, int value)>? node))
    {
      _values.Remove(node);
      _values.AddFirst(node);
      return node.Value.value;
    }

    return -1;
  }

  public void Put(int key, int value)
  {
    if (_keyToValueMap.TryGetValue(key, out LinkedListNode<(int key, int value)>? node))
    {
      _values.Remove(node);
      node.Value = (key, value);
      _values.AddFirst(node);
    }
    else
    {
      _keyToValueMap[key] = _values.AddFirst((key, value));

      if (_values.Count > capacity)
      {
        _keyToValueMap.Remove(_values.Last!.Value.key);
        _values.RemoveLast();
      }
    }
  }
}
