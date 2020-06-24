using System;
using System.Collections.Generic;
using System.Text;

namespace Jack.Storage.MemoryObjects
{
    class DataItem<T>
    {
        public T Data;
        public DataItem(T data)
        {
            Data = data;
        }
    }
}
