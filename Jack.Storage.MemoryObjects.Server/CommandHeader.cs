using System;
using System.Collections.Generic;
using System.Text;

namespace Jack.Storage.MemoryObjects.Server
{
    class CommandHeader
    {
        public string FilePath;
        public string KeyName;
        public string KeyType;
        public bool IsAsync = false;
        public bool ReadData = true;
    }
}
