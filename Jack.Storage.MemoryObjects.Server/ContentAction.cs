using System;
using System.Collections.Generic;
using System.Text;

namespace Jack.Storage.MemoryObjects.Server
{
    class ContentAction
    {
        public ActionType Type;
        public string KeyValue;
        public string Data;
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
