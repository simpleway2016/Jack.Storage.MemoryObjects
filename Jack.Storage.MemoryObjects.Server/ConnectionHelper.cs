using System;
using System.Collections.Generic;
using System.Text;

namespace Jack.Storage.MemoryObjects.Server
{
    class ConnectionHelper
    {
        static List<ConnectionHandler> ExistHandlers = new List<ConnectionHandler>();
        /// <summary>
        /// 注册ConnectionHandler
        /// </summary>
        /// <param name="handler"></param>
        public static void Register(ConnectionHandler handler)
        {
            lock(ExistHandlers)
            {
                ExistHandlers.Add(handler);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        public static void UnRegister(ConnectionHandler handler)
        {
            lock (ExistHandlers)
            {
                ExistHandlers.Remove(handler);
            }
        }

    }
}
