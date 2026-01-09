using System.Collections.Generic;

namespace ViciOne.Suite.DataPort;

internal sealed class HttpCacheMessage(int size)
{
    private readonly Queue<object> _data = new(size);
    private readonly int _size = size;

    public IReadOnlyCollection<object> GetData() => _data;

    public void AddData(object data)
    {
        if (_size <= _data.Count)
            _data.Dequeue();

        _data.Enqueue(data);
    }
}
