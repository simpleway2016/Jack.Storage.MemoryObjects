using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Jack.Storage.MemoryObjects.Local
{
    class AsyncStorageQueue<T> : IStorageQueue<T>
    {
        List<DataItem<T>> _dataList;
        string _filepath;
        PropertyInfo _propertyInfo;
        ILogger _logger;
        StorageDB<T> _db;

        ConcurrentQueue<OpAction<T>> _backupQueue = new ConcurrentQueue<OpAction<T>>();
        System.Threading.ManualResetEvent _backupEvent = new System.Threading.ManualResetEvent(false);
        bool _backupExited = false;
        bool _disposed = false;

        public AsyncStorageQueue(List<DataItem<T>> dataList, string filepath, PropertyInfo propertyInfo, ILogger logger)
        {
            this._dataList = dataList;
            this._filepath = filepath;
            this._propertyInfo = propertyInfo;
            this._logger = logger;

            _db = new StorageDB<T>(filepath, _propertyInfo, logger);

            _db.ReadData<T>((item) => {
                _dataList.Add(new DataItem<T>(item));
            });

            new Thread(backupRunning).Start();
        }

        public void Dispose()
        {
            _disposed = true;
            _backupEvent.Set();

            while (!_backupExited)
                Thread.Sleep(100);
            _db.Dispose();
        }

        void backupRunning()
        {
            while (!_disposed || _backupQueue.Count > 0)
            {
                _backupEvent.WaitOne();
                _backupEvent.Reset();

                List<OpAction<T>> buffer = new List<OpAction<T>>(500);
                while (true)
                {
                    if (_backupQueue.TryDequeue(out OpAction<T> dataitem))
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
            _backupExited = true;
        }

        public void Add(T item)
        {
            _backupQueue.Enqueue(new OpAction<T>()
            {
                Type = ActionType.Add,
                Data = item
            });
            _backupEvent.Set();
        }

        public void Remove(T item)
        {
            _backupQueue.Enqueue(new OpAction<T>()
            {
                Type = ActionType.Remove,
                Data = item
            });
            _backupEvent.Set();
        }

        public void Update(T item)
        {
            _backupQueue.Enqueue(new OpAction<T>()
            {
                Type = ActionType.Update,
                Data = item
            });
            _backupEvent.Set();
        }

        public void Remove(IEnumerable<T> list)
        {
            foreach( var item in list )
            {
                _backupQueue.Enqueue(new OpAction<T>()
                {
                    Type = ActionType.Remove,
                    Data = item
                });
            }
            _backupEvent.Set();
        }
    }
}
