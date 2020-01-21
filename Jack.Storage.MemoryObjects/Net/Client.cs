using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Way.Lib;

namespace Jack.Storage.MemoryObjects.Net
{
    class Client:IDisposable
    {
        NetStream _stream;
        PropertyInfo _propertyInfo;
        public Client(string server, int port,PropertyInfo propertyInfo, CommandHeader header)
        {
            _propertyInfo = propertyInfo;
            _stream = new NetStream(server, port);
            var content = header.ToJsonString();
            var bs = Encoding.UTF8.GetBytes(content);
            _stream.Write(bs.Length);
            _stream.Write(bs);


        }
        public void ReadData<T>(Action<T> callback)
        {
            while (true)
            {
                int len = _stream.ReadInt();
                if (len == -1)
                    break;
                var bs = _stream.ReceiveDatas(len);
                var content = Encoding.UTF8.GetString(bs);
                callback(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content));
            }

        }

        public void Send<T>(OpAction<T> item)
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
        }

        /// <summary>
        /// 检查是否已经全部保存
        /// </summary>
        public void CheckAllSaved()
        {
            ContentAction action = new ContentAction();
            action.Type = ActionType.CheckSaved;

            var bs = Encoding.UTF8.GetBytes(action.ToJsonString());
            _stream.Write(bs.Length);
            _stream.Write(bs);

            try
            {
                _stream.ReadBoolean();
            }
            catch
            {

            }
            
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
