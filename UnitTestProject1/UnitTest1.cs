using Jack.Storage.MemoryObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Linq;
using System;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Add()
        {
            var filepath = "./test.db";
            System.IO.File.Delete(filepath);

            var context = new StorageContext<MyDataItem>(filepath, "Name");
            System.Threading.Tasks.Parallel.For(0, 10000, (i) => {
                var item = new MyDataItem();
                item.Name = "Jack" + i;
                context.Add(item);
            });

            if (context.Count != 10000)
                throw new Exception("数量不对");

            context.Dispose();

            context = new StorageContext<MyDataItem>("./test.db", "Name");
            if (context.Count != 10000)
                throw new Exception("数量不对");

            for(int i = 0; i < 10000; i ++)
            {
                if(context.Any(m=>m.Name == "Jack" + i) == false)
                    throw new Exception("没有这个对象");
            }
        }

        [TestMethod]
        public void Remove()
        {
            var filepath = "./test2.db";
            System.IO.File.Delete(filepath);

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
        }
    }

    class MyDataItem
    {
        public string Name { get; set; }

    }
}
