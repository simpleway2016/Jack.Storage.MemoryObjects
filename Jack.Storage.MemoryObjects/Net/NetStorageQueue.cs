using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Jack.Storage.MemoryObjects.Net
{
    class NetStorageQueue<T> : IStorageQueue<T>
    {
        List<DataItem<T>> _dataList;
        string _filepath;
        PropertyInfo _propertyInfo;
        string _serverAddr;
        int _port;
        string _storageName;
        bool _isAsync;
        Client<T> _netClient;
        public NetStorageQueue(List<DataItem<T>> dataList, string filepath, string serverAddr, int port, string storageName, PropertyInfo propertyInfo, bool isAsync = false)
        {
            this._dataList = dataList;
            this._filepath = filepath;
            this._propertyInfo = propertyInfo;
            this._serverAddr = serverAddr;
            this._port = port;
            this._storageName = storageName;
            this._isAsync = isAsync;

            if (isAsync)
            {
                _netClient = new ClientAsync<T>(serverAddr, port, _propertyInfo, new CommandHeader()
                {
                    FilePath = filepath,
                    IsAsync = true,
                    KeyName = propertyInfo.Name,
                    KeyType = _propertyInfo.PropertyType.FullName
                }, (item) =>
                {
                    _dataList.Add(new DataItem<T>(item));
                });
            }
            else
            {
                _netClient = new Client<T>(serverAddr, port, _propertyInfo, new CommandHeader()
                {
                    FilePath = filepath,
                    KeyName = propertyInfo.Name,
                    KeyType = _propertyInfo.PropertyType.FullName
                }, (item) =>
                {
                    _dataList.Add(new DataItem<T>(item));
                });
            }

        }
        public void Add(T item)
        {
            _netClient.Send(new OpAction<T>()
            {
                Type = ActionType.Add,
                Data = item
            });
        }

        public void Dispose()
        {
            _netClient.CheckAllSaved();
            _netClient.Dispose();
        }

        public void Remove(T item)
        {
            _netClient.Send(new OpAction<T>()
            {
                Type = ActionType.Remove,
                Data = item
            });
        }

        public void Remove(IEnumerable<T> list)
        {
            foreach( var item in list)
            {
                _netClient.Send(new OpAction<T>()
                {
                    Type = ActionType.Remove,
                    Data = item
                });
            }
        }

        public void Update(T item)
        {
            _netClient.Send(new OpAction<T>()
            {
                Type = ActionType.Update,
                Data = item
            });
        }
    }
}
