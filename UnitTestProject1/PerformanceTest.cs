using Jack.Storage.MemoryObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Linq;
using System;
using System.Diagnostics;

namespace UnitTestProject1
{
    [TestClass]
    public class PerformanceTest
    {
      

        [TestMethod]
        public void Add()
        {
            var filepath = "./PerformanceTest.db";
            System.IO.File.Delete(filepath);

            var context = new StorageContext<MyDataItem>(filepath, "Name");
            var item2 = new MyDataItem();
            item2.Name = "awef0";
            context.Add(item2);
            context.Dispose();

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch watch2 = new System.Diagnostics.Stopwatch();
            watch2.Start();
            watch.Start();
            context = new StorageContext<MyDataItem>(filepath, "Name");
            for(int i = 0; i < 1000000; i ++)
            {
                var item = new MyDataItem();
                item.Name = "Jack" + i;
                context.Add(item);
            }

            watch.Stop();
            Debug.WriteLine($"总耗时：{watch.ElapsedMilliseconds}ms");

            context.Dispose();
            watch2.Stop();
            Debug.WriteLine($"写入文件总耗时：{watch2.ElapsedMilliseconds}ms");

        }

       
    }

    class MyDataItem2
    {
        public string Name { get; set; }
    }
}
