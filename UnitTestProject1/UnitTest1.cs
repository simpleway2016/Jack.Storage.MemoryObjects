using Jack.Storage.MemoryObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Normal()
        {
            var filepath = "temp";

            var context = new StorageContext<MyDataItem>(filepath, "Name" );
            context.Add(new MyDataItem() { 
                Name = "MyName1",
                Company = "Microsoft"
            });

            context.Add(new MyDataItem()
            {
                Name = "MyName1",
                Company = "Microsoft"
            });

            context.Remove(context.Where(m => m.Name.Contains("My")));

            context.Dispose();
            System.IO.File.Delete(filepath);
        }

        [TestMethod]
        public void RepeatCheck()
        {
            var filepath = "RepeatCheck";
            

            var context = new StorageContext<MyDataItem>(filepath, "Name" , true);
            context.Add(new MyDataItem()
            {
                Name = "MyName1",
                Company = "Microsoft"
            });

            try
            {
                context.Add(new MyDataItem()
                {
                    Name = "MyName1",
                    Company = "Microsoft"
                });

                throw new Exception("重复数据也能添加");
            }
            catch
            {

            }

            context.Dispose();

            System.IO.File.Delete(filepath);
        }

        [TestMethod]
        public void Add()
        {
            var filepath = "Add";
           

            var context = new StorageContext<MyDataItem>(filepath, "Name");
            System.Threading.Tasks.Parallel.For(0, 10000, (i) => {
                var item = new MyDataItem();
                item.Name = "Jack" + i;
                context.Add(item);
            });

            if (context.Count != 10000)
                throw new Exception("数量不对");

            context.Dispose();

            context = new StorageContext<MyDataItem>(filepath, "Name");
            if (context.Count != 10000)
                throw new Exception("数量不对");

            for(int i = 0; i < 10000; i ++)
            {
                if(context.Any(m=>m.Name == "Jack" + i) == false)
                    throw new Exception("没有这个对象");
            }

            System.IO.File.Delete(filepath);
        }

        [TestMethod]
        public void Update()
        {
            var filepath = "Update";
          

            var context = new StorageContext<MyDataItem>(filepath, "Name");
            System.Threading.Tasks.Parallel.For(0, 10, (i) => {
                var item = new MyDataItem();
                item.Name = "Jack" + i;
                context.Add(item);
            });

            if (context.Count != 10)
                throw new Exception("数量不对");

            var obj = context.FirstOrDefault(m => m.Name == "Jack5");
            obj.Company = "abc";
            context.Update(obj);

            context.Dispose();



            context = new StorageContext<MyDataItem>(filepath, "Name");
            if (context.Count != 10)
                throw new Exception("数量不对");

            obj = context.FirstOrDefault(m => m.Name == "Jack5");
            if (obj.Company != "abc")
                throw new Exception("数据错误");

            System.IO.File.Delete(filepath);
        }

        [TestMethod]
        public void Remove()
        {
            var filepath = "Remove";
         

            var context = new StorageContext<MyDataItem>(filepath, "Name");
            System.Threading.Tasks.Parallel.For(0, 10000, (i) => {
                var item = new MyDataItem();
                item.Name = "Jack" + i;
                context.Add(item);
            });

            if (context.Count != 10000)
                throw new Exception("数量不对");

            context.Dispose();

            context = new StorageContext<MyDataItem>(filepath, "Name");
            if (context.Count != 10000)
                throw new Exception("数量不对");

            var datas = context.ToArray();
            System.Threading.Tasks.Parallel.For(0, datas.Length, (i) => {
                context.Remove(datas[i]);
            });


            context.Dispose();

            context = new StorageContext<MyDataItem>(filepath, "Name");
            if (context.Count != 0)
                throw new Exception("数量不对");


            System.IO.File.Delete(filepath);
        }

        [TestMethod]
        public void Test()
        {
            List<int> data = new List<int>();
            Task.Run(()=> {
                int index = 0;
               while(true)
                {
                    data.Add(index++);
                }
            });

            Task.Run(() => {
                var enumtor = new MyEnumable<int>(data);
               while (true)
                {
                    try
                    {
                       var d = enumtor.Where(m => m % 2 == 0).Take(100).ToArray();
                    }
                    catch(System.InvalidOperationException ex)
                    {
                        if(ex.HResult == -2146233079)
                        {

                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
            });

            Thread.Sleep(1000000);
        }
    }

    class MyEnumable<T> : IEnumerable<T>
    {
        List<T> _source;
        public MyEnumable(List<T> source)
        {
            _source = source;
        }
        public IEnumerator<T> GetEnumerator()
        {
            return new MyEnumerator<T>(_source);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    class MyEnumerator<T>: IEnumerator<T>
    {
        List<T> _source;
        int _position = 0;
        public MyEnumerator(List<T> source)
        {
            _source = source;
        }

        public T Current
        {
            get
            {
                if( _position < _source.Count )
                {
                    try
                    {
                        return _source[_position];
                    }
                    catch 
                    {
                        return default(T);
                    }
                }

                return default(T);
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return this.Current;
            }
        }

        public void Dispose()
        {
           
        }

        public bool MoveNext()
        {
            if (_position < _source.Count - 1)
            {
                _position++;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _position = 0;
        }
    }

    class MyDataItem
    {
        public string Name { get; set; }
        public string Company { get; set; }
    }
}
