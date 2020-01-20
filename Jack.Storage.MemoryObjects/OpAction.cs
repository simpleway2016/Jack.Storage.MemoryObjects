using System;
using System.Collections.Generic;
using System.Text;

namespace Jack.Storage.MemoryObjects
{
    class OpAction<T>
    {
        public ActionType Type;
        public T Data;
    }
    enum ActionType
    {
        Add,
        Remove,
        Update
    }
}
