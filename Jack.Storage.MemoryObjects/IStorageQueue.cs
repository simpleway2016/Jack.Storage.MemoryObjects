using System;
using System.Collections.Generic;
using System.Text;

namespace Jack.Storage.MemoryObjects
{
    interface IStorageQueue<T>:IDisposable
    {
        void Add(T item);
        void Remove(T item);
        void Update(T item);
        void Remove(IEnumerable<T> list);
    }
}
