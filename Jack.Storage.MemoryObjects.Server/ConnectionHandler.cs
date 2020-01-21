using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Way.Lib;

namespace Jack.Storage.MemoryObjects.Server
{
    class ConnectionHandler
    {
        Way.Lib.NetStream _client;
        ConcurrentQueue<ContentAction> _backupQueue = new ConcurrentQueue<ContentAction>();
        System.Threading.ManualResetEvent _backupEvent = new System.Threading.ManualResetEvent(false);

        StorageDB _db;
        bool _exited = false;
        public ConnectionHandler(Socket socket)
        {
            _client = new Way.Lib.NetStream(socket);
        }


        public void Handle()
        {
            int len = _client.ReadInt();
            var header = Encoding.UTF8.GetString( _client.ReceiveDatas(len)).FromJson<CommandHeader>();

            _db = new StorageDB(header.FilePath, header.KeyName, header.KeyType);

            //读取现有数据
            _db.ReadData((content) => {
                var bs = Encoding.UTF8.GetBytes(content);
                _client.Write(bs.Length);
                _client.Write(bs);
            });
            _client.Write((int)-1);

            new Thread(processAction).Start();

            while(true)
            {
                len = _client.ReadInt();
                var action = Encoding.UTF8.GetString(_client.ReceiveDatas(len)).FromJson<ContentAction>();

                if( action.Type == ActionType.CheckSaved )
                {
                    while (_backupQueue.Count > 0)
                        Thread.Sleep(10);
                    Thread.Sleep(1000);

                    _client.Write(true);
                    break;
                }
                _backupQueue.Enqueue(action);
                _backupEvent.Set();
            }
            _exited = true;
            _client.Dispose();
        }

        void processAction()
        {
            while (!_exited || _backupQueue.Count > 0)
            {
                _backupEvent.WaitOne();
                _backupEvent.Reset();

                List<ContentAction> buffer = new List<ContentAction>(500);
                while (true)
                {
                    if (_backupQueue.TryDequeue(out ContentAction dataitem))
                    {
                        buffer.Add(dataitem);
                    }
                    else
                        break;
                }

                if (buffer.Count > 0)
                {
                    _db.Handle(buffer);
                }

            }

        }
    }

  
}
