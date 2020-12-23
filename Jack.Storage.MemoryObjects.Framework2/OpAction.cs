using System;
using System.Collections.Generic;
using System.Text;

namespace Jack.Storage.MemoryObjects
{
    class OpAction<T>
    {
        public ActionType Type;
        public T Data;
        public Action CallBack;
    }
    enum ActionType
    {
        Add,
        Remove,
        Update,
        DeleteFile,
        CheckSaved,
    }
}
