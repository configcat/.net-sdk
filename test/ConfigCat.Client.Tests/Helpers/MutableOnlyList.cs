using System.Collections;
using System.Collections.Generic;

namespace ConfigCat.Client.Tests.Helpers;

internal sealed class MutableOnlyList<T> : IList<T>
{
    private readonly IList<T> list;

    public MutableOnlyList(IList<T> list)
    {
        this.list = list;
    }

    public T this[int index]
    {
        get => this.list[index];
        set => this.list[index] = value;
    }

    public int Count => this.list.Count;
    public bool IsReadOnly => this.list.IsReadOnly;

    public void Add(T item) => this.list.Add(item);
    public void Clear() => this.list.Clear();
    public bool Contains(T item) => this.list.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => this.list.CopyTo(array, arrayIndex);
    public IEnumerator<T> GetEnumerator() => this.list.GetEnumerator();
    public int IndexOf(T item) => this.list.IndexOf(item);
    public void Insert(int index, T item) => this.list.Insert(index, item);
    public bool Remove(T item) => this.list.Remove(item);
    public void RemoveAt(int index) => this.list.RemoveAt(index);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
