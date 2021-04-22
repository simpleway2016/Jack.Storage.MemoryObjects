using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Jack.Storage.MemoryObjects.Local
{
    class SyncStorageQueue<T> : IStorageQueue<T>
    {
        List<DataItem<T>> _dataList;
        string _filepath;
        PropertyInfo _propertyInfo;
        ILogger _logger;
        StorageDB<T> _db;
        public SyncStorageQueue(List<DataItem<T>> dataList, string filepath, PropertyInfo propertyInfo, ILogger logger)
        {
            this._dataList = dataList;
            this._filepath = filepath;
            this._propertyInfo = propertyInfo;
            this._logger = logger;

            _db = new StorageDB<T>(filepath, _propertyInfo, logger);

            _db.ReadData<T>((item) => {
                _dataList.Add(new DataItem<T>(item));
            });
        }

        public void Add(T item)
        {
            _db.Add(item);
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public void Remove(T item)
        {
            _db.Delete(item);
        }

        public void Remove(IEnumerable<T> list)
        {
            _db.Delete(list);
        }

        public void Update(T item)
        {
            _db.Update(item);
        }
    }
}
