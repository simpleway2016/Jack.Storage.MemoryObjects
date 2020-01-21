using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Microsoft.Extensions.Logging;
using Jack.Storage.MemoryObjects.Net;

namespace Jack.Storage.MemoryObjects
{
    /// <summary>
    /// 内存数据集合，并且自动备份到指定的磁盘文件当中
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StorageContext<T> : IDisposable, IEnumerable<T>
    {
        public string StorageName { get; }

        public int Count => _dataList.Count;


        bool _disposed = false;
        List<T> _dataList = new List<T>();
        ConcurrentQueue<OpAction<T>> _backupQueue = new ConcurrentQueue<OpAction<T>>();
        System.Threading.ManualResetEvent _backupEvent = new System.Threading.ManualResetEvent(false);
        bool _backupExited = false;


        System.Reflection.PropertyInfo _propertyInfo;
        StorageDB _db;
        bool _checkRepeatPrimaryKey;

        Client _netClient;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="storageName">保存的名称</param>
        /// <param name="primaryPropertyName">主键属性的名称，必须是long、int、string类型</param>
        /// <param name="checkRepeatPrimaryKey">是否检查主键重复，如果为true，会对写入性能有影响</param>
        /// <param name="logger"></param>
        public StorageContext(string storageName,string primaryPropertyName, bool checkRepeatPrimaryKey = false, ILogger logger = null)
        {
            var filepath = "./Jack.Storage.MemoryObjects.datas/" + storageName + ".db";
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

            _db = new StorageDB(filepath , _propertyInfo , logger);

            _db.ReadData<T>((item) => {
                _dataList.Add(item);
            });

            new Thread(backupRunning).Start();
        }
        /// <summary>
        /// 通过网络服务保存为文件，持久化数据
        /// </summary>
        /// <param name="serverAddr">服务期地址</param>
        /// <param name="port">端口</param>
        /// <param name="storageName">保存的名称</param>
        /// <param name="primaryPropertyName">主键属性的名称，必须是long、int、string类型</param>
        public StorageContext(string serverAddr,int port,string storageName, string primaryPropertyName)
        {
            var filepath = "./data/" + storageName + ".db";
            if (string.IsNullOrEmpty(filepath))
                throw new Exception("filepath is empty");
            if (string.IsNullOrEmpty(primaryPropertyName))
                throw new Exception("primaryPropertyName is empty");

            _propertyInfo = typeof(T).GetProperty(primaryPropertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (_propertyInfo == null)
                throw new Exception($"{typeof(T).FullName} can not find property: {primaryPropertyName}");

            if (_propertyInfo.PropertyType != typeof(string) &&
                _propertyInfo.PropertyType != typeof(int) &&
                _propertyInfo.PropertyType != typeof(long)
                )
            {
                throw new Exception("主键属性必须是long、int、string类型");
            }
            this.StorageName = storageName;

            _netClient = new Client(serverAddr, port, _propertyInfo, new CommandHeader() { 
            FilePath = filepath,
            KeyName = primaryPropertyName,
            KeyType = _propertyInfo.PropertyType.FullName
            });

            _netClient.ReadData<T>((item) => {
                _dataList.Add(item);
            });
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

        public void Dispose()
        {
            if (_netClient == null)
            {
                _disposed = true;
                _backupEvent.Set();

                while (!_backupExited)
                    Thread.Sleep(100);
                _db.Dispose();
            }
            else
            {
                _netClient.CheckAllSaved();
                _netClient.Dispose();
            }
        }

        void DeleteFile()
        {
            if(_netClient != null)
            {
                lock (_netClient)
                {
                    _netClient.Send<T>(new OpAction<T>()
                    {
                        Type = ActionType.DeleteFile,
                    });
                }
            }
            lock(_dataList)
            {
                this._dataList.Clear();

            }
          
        }

        
        public void Add(T item)
        {
            if (item == null)
                return;
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
                    //这个判断影响写入性能
                    //if (_dataList.Contains(item))
                    //    throw new Exception($"此对象已经在集合当中，不能重复添加");

                    _dataList.Add(item);
                }

                if (_netClient == null)
                {
                    _backupQueue.Enqueue(new OpAction<T>()
                    {
                        Type = ActionType.Add,
                        Data = item
                    });
                    _backupEvent.Set();
                }
                else
                {
                    _netClient.Send<T>(new OpAction<T>()
                    {
                        Type = ActionType.Add,
                        Data = item
                    });
                }
            }
           
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
            lock (_dataList)
            {
                if (_checkRepeatPrimaryKey)
                {
                    var _whereQuery = LinqHelper.InvokeWhereEquals(_dataList.Where(m=>m.Equals(item) == false).AsQueryable<T>(), _propertyInfo.Name, key);
                    if (LinqHelper.InvokeAny(_whereQuery))
                    {
                        throw new Exception($"{key} exist");
                    }
                }

                if (_netClient == null)
                {
                    _backupQueue.Enqueue(new OpAction<T>()
                    {
                        Type = ActionType.Update,
                        Data = item
                    });
                    _backupEvent.Set();
                }
                else
                {
                    _netClient.Send<T>(new OpAction<T>()
                    {
                        Type = ActionType.Update,
                        Data = item
                    });
                }
            }
          

           
        }
        public void Remove(T item)
        {
            if (item == null)
                return;
            lock (_dataList)
            {
                _dataList.Remove(item);

                if (_netClient == null)
                {
                    _backupQueue.Enqueue(new OpAction<T>()
                    {
                        Type = ActionType.Remove,
                        Data = item
                    });
                    _backupEvent.Set();
                }
                else
                {
                    _netClient.Send<T>(new OpAction<T>()
                    {
                        Type = ActionType.Remove,
                        Data = item
                    });
                }
            } 
        }
        public void Remove(IEnumerable<T> list)
        {
            if (list == null)
                return;
            var arr = list.ToArray();
            lock (_dataList)
            {
                foreach( var item in arr)
                {
                    if(item != null)
                    {
                        _dataList.Remove(item);
                    }                    
                }

                if (_netClient == null)
                {
                    foreach (var item in arr)
                    {
                        if (item != null)
                        {
                            _backupQueue.Enqueue(new OpAction<T>()
                            {
                                Type = ActionType.Remove,
                                Data = item
                            });
                        }
                    }

                    _backupEvent.Set();
                }
                else
                {
                    foreach (var item in arr)
                    {
                        if (item != null)
                        {
                            _netClient.Send<T>(new OpAction<T>()
                            {
                                Type = ActionType.Remove,
                                Data = item
                            });
                        }
                    }
                }
            }
           
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
