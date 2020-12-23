using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Jack.Storage.MemoryObjects
{
    /// <summary>
    /// 内存数据集合，并且自动备份到指定的磁盘文件当中
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StorageContext<T> : IDisposable, IEnumerable<T>
    {
        public string StorageName { get; }

        public int Count => _datas.Count;


        bool _disposed = false;
        List<int> _freeIndex = new List<int>();

        ConcurrentDictionary<T, bool> _datas = new ConcurrentDictionary<T, bool>();
        ConcurrentQueue<OpAction<T>> _backupQueue = new ConcurrentQueue<OpAction<T>>();
        System.Threading.ManualResetEvent _backupEvent = new System.Threading.ManualResetEvent(false);
        bool _backupExited = false;


        System.Reflection.PropertyInfo _propertyInfo;
        StorageDB _db;
        bool _checkRepeatPrimaryKey;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storageName">保存的名称</param>
        /// <param name="primaryPropertyName">主键属性的名称，必须是long、int、string类型</param>
        /// <param name="dataPath">数据文件保存路径</param>
        /// <param name="checkRepeatPrimaryKey">是否检查主键重复，如果为true，会对写入性能有影响</param>
        public StorageContext(string storageName,string primaryPropertyName,string dataPath = "./Jack.Storage.MemoryObjects.datas" , bool checkRepeatPrimaryKey = false)
        {
            var filepath = $"{dataPath}/" + storageName + ".db";
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
            this.StorageName = storageName;

            _db = new StorageDB(filepath , _propertyInfo);

            _db.ReadData<T>((item) => {
                _datas.TryAdd(item, true);
            });

            new Thread(backupRunning).Start();
        }
       
      
        void backupRunning()
        {
            while(!_disposed || _backupQueue.Count > 0)
            {
                _backupEvent.WaitOne();
                _backupEvent.Reset();

                List<OpAction<T>> buffer = new List<OpAction<T>>(500);
                while(true)
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
                    _db.Handle<T>(buffer);
                }
                    
            }
            _backupExited = true;
        }
        /// <summary>
        /// 释放对象，并保证所有数据已经同步到文件
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            _backupEvent.Set();

            while (!_backupExited)
                Thread.Sleep(100);
            _db.Dispose();
            _datas.Clear();
        }

        
        
        public void Add(T item)
        {
            if (item == null)
                return;
            var key = _propertyInfo.GetValue(item);
            if (_checkRepeatPrimaryKey)
            {
                var _whereQuery = LinqHelper.InvokeWhereEquals(_datas.Select(m=>m.Key).Where(m => m != null).Select(m => m).AsQueryable<T>(), _propertyInfo.Name, key);
                if (LinqHelper.InvokeAny(_whereQuery))
                {
                    throw new Exception($"{key} exist");
                }

                _datas.TryAdd(item, true);
            }
            else
            {
                _datas.TryAdd(item, true);
            }

            _backupQueue.Enqueue(new OpAction<T>()
            {
                Type = ActionType.Add,
                Data = item
            });
            _backupEvent.Set();

        }
        /// <summary>
        /// 如果对象属性改变，调用此方法，更新到文件（注意：主键属性不要更改）
        /// </summary>
        /// <param name="item"></param>
        public void Update(T item)
        {
            if (item == null)
                return;
            var key = _propertyInfo.GetValue(item);

            if (_checkRepeatPrimaryKey)
            {
                var _whereQuery = LinqHelper.InvokeWhereEquals(_datas.Select(m=>m.Key).Where(m => m != null && m.Equals(item) == false).Select(m => m).AsQueryable<T>(), _propertyInfo.Name, key);
                if (LinqHelper.InvokeAny(_whereQuery))
                {
                    throw new Exception($"{key} exist");
                }
            }

            _backupQueue.Enqueue(new OpAction<T>()
            {
                Type = ActionType.Update,
                Data = item
            });
            _backupEvent.Set();

        }
        public void Remove(T item)
        {
            if (item == null)
                return;
            _datas.TryRemove(item, out bool o);
        }
        public void Remove(IEnumerable<T> list)
        {
            if (list == null)
                return;
            foreach (var item in list)
            {
                if (item != null)
                {
                    _datas.TryRemove(item, out bool o);
                }
            }

        }

        public IEnumerator<T> GetEnumerator()
        {
            return _datas.Select(m => m.Key).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

      
    }
}
