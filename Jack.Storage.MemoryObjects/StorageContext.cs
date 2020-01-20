using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Jack.Storage.MemoryObjects
{
    /// <summary>
    /// 内存数据集合，并且自动备份到指定的磁盘文件当中
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StorageContext<T> : IDisposable, IEnumerable<T>
    {
        public string FilePath { get; }

        public int Count => _dataList.Count;


        bool _disposed = false;
        List<T> _dataList = new List<T>();
        ConcurrentQueue<T> _backupQueue = new ConcurrentQueue<T>();
        System.Threading.ManualResetEvent _backupEvent = new System.Threading.ManualResetEvent(false);
        bool _backupExited = false;

        ConcurrentQueue<T> _removeQueue = new ConcurrentQueue<T>();
        System.Threading.ManualResetEvent _removeEvent = new System.Threading.ManualResetEvent(false);
        bool _removeExited = false;

        System.Reflection.PropertyInfo _propertyInfo;
        StorageDB _db;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath">文件保存路径</param>
        /// <param name="primaryPropertyName">主键属性的名称，必须是long、int、string类型</param>
        public StorageContext(string filepath,string primaryPropertyName)
        {
            if (string.IsNullOrEmpty(filepath))
                throw new Exception("filepath is empty");
            if (string.IsNullOrEmpty(primaryPropertyName))
                throw new Exception("primaryPropertyName is empty");

            _propertyInfo = typeof(T).GetProperty(primaryPropertyName , System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if(_propertyInfo == null)
                throw new Exception($"{typeof(T).FullName} can not find property: {primaryPropertyName}");

            if (_propertyInfo.PropertyType != typeof(string) &&
                _propertyInfo.PropertyType != typeof(int) &&
                _propertyInfo.PropertyType != typeof(long)
                )
            {
                throw new Exception("主键属性必须是long、int、string类型");
            }
            this.FilePath = filepath;
            _db = new StorageDB(filepath , _propertyInfo);

            _db.ReadData<T>((item) => {
                _dataList.Add(item);
            });

            new Thread(backupRunning).Start();
            new Thread(removeRunning).Start();
        }

      
        void backupRunning()
        {
            while(!_disposed || _backupQueue.Count > 0)
            {
                _backupEvent.WaitOne();
                _backupEvent.Reset();

                List<T> buffer = new List<T>(100);
                while(true)
                {
                    if (_backupQueue.TryDequeue(out T dataitem))
                    {
                        buffer.Add(dataitem);
                    }
                    else
                        break;
                }

                if (buffer.Count > 0)
                {
                    _db.Insert(buffer);
                }
                    
            }
            _backupExited = true;
        }
        void removeRunning()
        {
            while (!_disposed || _removeQueue.Count > 0)
            {
                _removeEvent.WaitOne();
                _removeEvent.Reset();

                List<T> buffer = new List<T>(100);
                while (true)
                {
                    if (_removeQueue.TryDequeue(out T dataitem))
                    {
                        buffer.Add(dataitem);
                    }
                    else
                        break;
                }

                if (buffer.Count > 0)
                {
                    _db.DeleteData(buffer);
                }
            }
            _removeExited = false;
        }
        public void Dispose()
        {
            _disposed = true;
            _backupEvent.Set();
            _removeEvent.Set();
            while (!_backupExited || _removeExited)
                Thread.Sleep(100);
            _db.Dispose();
        }

        
        public void Add(T item)
        {
            lock (_dataList)
            {
                _dataList.Add(item);
            }
            _backupQueue.Enqueue(item);
            _backupEvent.Set();
        }


        public void Remove(T item)
        {
            lock (_dataList)
            {
                _dataList.Remove(item);
            }

            _removeQueue.Enqueue(item);
            _removeEvent.Set();
        }


        public IEnumerator<T> GetEnumerator()
        {
            return _dataList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dataList.GetEnumerator();
        }
    }
}
