using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using Way.Lib;

namespace Jack.Storage.MemoryObjects.Net
{
    class ClientAsync<T> : Client<T>
    {
        NetStream _stream;
        PropertyInfo _propertyInfo;
        ConcurrentQueue<OpAction<T>> _queue = new ConcurrentQueue<OpAction<T>>();
        ManualResetEvent _event = new ManualResetEvent(false);
        bool _disposed = false;
        string _server;
        int _port;
        CommandHeader _header;
        public ClientAsync(string server, int port, PropertyInfo propertyInfo, CommandHeader header, Action<T> callback):base(null,0,null,null,null)
        {
            _server = server;
            _port = port;
            _header = header;
            header.ReadData = true;
            _propertyInfo = propertyInfo;
            init();

            while (true)
            {
                int len = _stream.ReadInt();
                if (len == -1)
                    break;
                var bs = _stream.ReceiveDatas(len);
                var content = Encoding.UTF8.GetString(bs);
                callback(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content));
            }

            header.ReadData = false;
            new Thread(runForSend).Start();
        }
        void init()
        {
            _stream = new NetStream(_server, _port);
            var content = _header.ToJsonString();
            var bs = Encoding.UTF8.GetBytes(content);
            _stream.Write(bs.Length);
            _stream.Write(bs);
        }
        void runForSend()
        {
            while (!_disposed || _queue.Count > 0)
            {
                _event.WaitOne();
                _event.Reset();

                while (_queue.TryDequeue(out OpAction<T> item))
                {
                    while (!_disposed)
                    {
                        try
                        {                           
                            ContentAction action = new ContentAction();
                            action.Type = item.Type;
                            
                            if (item.Data != null)
                            {
                                action.KeyValue = _propertyInfo.GetValue(item.Data).ToString();
                                action.Data = item.Data.ToJsonString();
                            }
                            var bs = Encoding.UTF8.GetBytes(action.ToJsonString());
                            _stream.Write(bs.Length);
                            _stream.Write(bs);
                            if (item.CallBack != null)
                            {
                                item.CallBack();
                            }

                            if (item.Type == ActionType.CheckSaved)
                            {
                                try
                                {
                                    _stream.ReadBoolean();
                                }
                                catch
                                {

                                }
                                _stream.Dispose();
                                return;
                            }
                            break;
                        }
                        catch
                        {
                            _stream.Dispose();
                            try
                            {
                                init();
                            }
                            catch
                            {
                                _stream = null;
                            }

                        }
                    }
                }
            }
            _stream.Dispose();
        }


        public override void Send(OpAction<T> item)
        {
            _queue.Enqueue(item);
            _event.Set();
        }

        /// <summary>
        /// 检查是否已经全部保存
        /// </summary>
        public override void CheckAllSaved()
        {
            ManualResetEvent mywait = new ManualResetEvent(false);
            _queue.Enqueue(new OpAction<T>()
            {
                Type = ActionType.CheckSaved,
                CallBack = () => {
                    mywait.Set();
                }

            });
            _event.Set();

            mywait.WaitOne();


        }

        public override void Dispose()
        {
            _disposed = true;

        }
    }
}
