using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Microsoft.Extensions.Logging;

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
        bool _checkRepeatPrimaryKey;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filepath">文件保存路径</param>
        /// <param name="primaryPropertyName">主键属性的名称，必须是long、int、string类型</param>
        /// <param name="checkRepeatPrimaryKey">是否检查主键重复，如果为true，会对写入性能有微弱的影响</param>
        /// <param name="logger"></param>
        public StorageContext(string filepath,string primaryPropertyName, bool checkRepeatPrimaryKey = false, ILogger logger = null)
        {
            _checkRepeatPrimaryKey = checkRepeatPrimaryKey;
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

            _db = new StorageDB(filepath , _propertyInfo , logger);

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
            var key = _propertyInfo.GetValue(item);
            lock (_dataList)
            {
                if (_checkRepeatPrimaryKey)
                {
                    var _whereQuery = LinqHelper.InvokeWhereEquals(_dataList.AsQueryable<T>(), _propertyInfo.Name, key);
                    if (LinqHelper.InvokeAny(_whereQuery))
                    {
                        throw new Exception($"{key} exist");
                    }
                    _dataList.Add(item);
                }
                else
                {
                    if (_dataList.Contains(item))
                        throw new Exception($"此对象已经在集合当中，不能重复添加");

                    _dataList.Add(item);
                }
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
        public void Remove(IEnumerable<T> list)
        {
            var arr = list.ToArray();
            lock (_dataList)
            {
                foreach( var item in arr)
                {
                    _dataList.Remove(item);
                }                
            }

            foreach (var item in arr)
            {
                _removeQueue.Enqueue(item);
            }
           
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
