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


        List<int> _freeIndex = new List<int>();

        List<DataItem<T>> _dataList = new List<DataItem<T>>();


        System.Reflection.PropertyInfo _propertyInfo;
        StorageDB<T> _db;
        bool _checkRepeatPrimaryKey;

        IStorageQueue<T> _storageQueue;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="storageName">保存的名称</param>
        /// <param name="primaryPropertyName">主键属性的名称，必须是long、int、string类型</param>
        /// <param name="asyncSaveFile">是否异步同步到文件当中,true写入性能较快，但数据存在丢失风险</param>
        /// <param name="checkRepeatPrimaryKey">是否检查主键重复，如果为true，会对写入性能有影响</param>
        /// <param name="logger"></param>
        public StorageContext(string storageName,string primaryPropertyName,bool asyncSaveFile = true, bool checkRepeatPrimaryKey = false, ILogger logger = null)
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

            _db = new StorageDB<T>(filepath , _propertyInfo , logger);

            _db.ReadData<T>((item) => {
                _dataList.Add(new DataItem<T>(item));
            });

            if (asyncSaveFile)
            {
                _storageQueue = new Local.AsyncStorageQueue<T>(_dataList, filepath, _propertyInfo, logger);
            }
            else
            {
                _storageQueue = new Local.SyncStorageQueue<T>(_dataList, filepath, _propertyInfo, logger);
            }
        }
        /// <summary>
        /// 通过网络服务保存为文件，持久化数据
        /// </summary>
        /// <param name="serverAddr">服务期地址</param>
        /// <param name="port">端口</param>
        /// <param name="storageName">保存的名称</param>
        /// <param name="primaryPropertyName">主键属性的名称，必须是long、int、string类型</param>
        /// <param name="isAsync">是否异步发送到服务器，同步对于文件写入有保证，异步对于性能有提升</param>
        public StorageContext(string serverAddr,int port,string storageName, string primaryPropertyName, bool isAsync = false)
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

            _storageQueue = new Net.NetStorageQueue<T>(_dataList, filepath, serverAddr, port, storageName, _propertyInfo, isAsync);

        }


        /// <summary>
        /// 释放对象，并保证所有数据已经同步到文件
        /// </summary>
        public void Dispose()
        {
            _storageQueue.Dispose();
            _dataList.Clear();
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
                    var _whereQuery = LinqHelper.InvokeWhereEquals(_dataList.Where(m=> m != null).Select(m=>m.Data).AsQueryable<T>(), _propertyInfo.Name, key);
                    if (LinqHelper.InvokeAny(_whereQuery))
                    {
                        throw new Exception($"{key} exist");
                    }
                    if (_freeIndex.Count > 0)
                    {
                        _dataList[_freeIndex[0]] = new DataItem<T>(item);
                        _freeIndex.RemoveAt(0);
                    }
                    else
                    {
                        _dataList.Add(new DataItem<T>(item));
                    }
                }
                else
                {
                    //这个判断影响写入性能
                    //if (_dataList.Contains(item))
                    //    throw new Exception($"此对象已经在集合当中，不能重复添加");

                    if (_freeIndex.Count > 0)
                    {
                        _dataList[_freeIndex[0]] = new DataItem<T>(item);
                        _freeIndex.RemoveAt(0);
                    }
                    else
                    {
                        _dataList.Add(new DataItem<T>(item));
                    }
                }

                _storageQueue.Add(item);
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
                    var _whereQuery = LinqHelper.InvokeWhereEquals(_dataList.Where(m=>m != null && m.Data.Equals(item) == false).Select(m=>m.Data).AsQueryable<T>(), _propertyInfo.Name, key);
                    if (LinqHelper.InvokeAny(_whereQuery))
                    {
                        throw new Exception($"{key} exist");
                    }
                }

                _storageQueue.Update(item);
            }
          

           
        }
        public void Remove(T item)
        {
            if (item == null)
                return;
            var index = _dataList.IndexOf(item);
            if (index < 0)
                return;
            
            lock (_dataList)
            {
                _dataList[index] = null;
                _freeIndex.Add(index);

                _storageQueue.Remove(item);
            }
        }
        public void Remove(IEnumerable<T> list)
        {
            if (list == null)
                return;
            var arr = list.ToArray();
            if (arr.Length == 0)
                return;

            lock (_dataList)
            {
                foreach( var item in arr)
                {
                    if(item != null)
                    {
                        var index = _dataList.IndexOf(item);
                        if (index >= 0)
                        {
                            _dataList[index] = null;
                            _freeIndex.Add(index);
                        }
                    }                    
                }

                _storageQueue.Remove(arr);
            }

        }

        public IEnumerator<T> GetEnumerator()
        {
              return new StorageContextEnumerator<T>(_dataList);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

      
    }
}
