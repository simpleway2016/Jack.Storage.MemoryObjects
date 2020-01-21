using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Way.Lib;

namespace Jack.Storage.MemoryObjects.Net
{
    class Client<T>:IDisposable
    {
        NetStream _stream;
        PropertyInfo _propertyInfo;
        public Client(string server, int port,PropertyInfo propertyInfo, CommandHeader header, Action<T> callback)
        {
            if (server == null)
                return;

            _propertyInfo = propertyInfo;
            _stream = new NetStream(server, port);
            var content = header.ToJsonString();
            var bs = Encoding.UTF8.GetBytes(content);
            _stream.Write(bs.Length);
            _stream.Write(bs);

            while (true)
            {
                int len = _stream.ReadInt();
                if (len == -1)
                    break;
                bs = _stream.ReceiveDatas(len);
                content = Encoding.UTF8.GetString(bs);
                callback(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content));
            }
        }


        public virtual void Send(OpAction<T> item)
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
        public virtual void CheckAllSaved()
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

        public virtual void Dispose()
        {
            _stream.Dispose();
        }
    }
}
