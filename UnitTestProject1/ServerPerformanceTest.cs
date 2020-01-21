using Jack.Storage.MemoryObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Linq;
using System;
using System.Diagnostics;
using System.Collections;

namespace UnitTestProject1
{
    [TestClass]
    public class ServerPerformanceTest
    {
        static ServerPerformanceTest()
        {
            var p = new ServerTest();
        }

        [TestMethod]
        public void Add()
        {
            var filepath = "server.PerformanceTest";
            var total = 1000000;

            var context = new StorageContext<MyDataItem>( "localhost" , 8227 , filepath, "Name");
            context.GetType().GetMethod("DeleteFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(context, null);

            var item2 = new MyDataItem();
            item2.Name = "awef0";
            context.Add(item2);
            context.Dispose();

            context = new StorageContext<MyDataItem>("localhost", 8227, filepath, "Name");
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch watch2 = new System.Diagnostics.Stopwatch();
            watch2.Start();
            watch.Start();            
            for (int i = 0; i < total; i ++)
            {
                var item = new MyDataItem();
                item.Name = "Jack" + i;
                context.Add(item);
            }

            watch.Stop();
            Debug.WriteLine($"{total / 10000}万数据写入总耗时：{watch.ElapsedMilliseconds}ms");

            context.Dispose();
            watch2.Stop();
            Debug.WriteLine($"写入文件总耗时：{watch2.ElapsedMilliseconds}ms");

        }
        [TestMethod]
        public void AddAsync()
        {
            var filepath = "server.PerformanceTest.async";
            var total = 1000000;

            var context = new StorageContext<MyDataItem>("localhost", 8227, filepath, "Name" , true);
            context.GetType().GetMethod("DeleteFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(context, null);

            var item2 = new MyDataItem();
            item2.Name = "awef0";
            context.Add(item2);
            context.Dispose();

            context = new StorageContext<MyDataItem>("localhost", 8227, filepath, "Name", true);
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch watch2 = new System.Diagnostics.Stopwatch();
            watch2.Start();
            watch.Start();            
            for (int i = 0; i < total; i++)
            {
                var item = new MyDataItem();
                item.Name = "Jack" + i;
                context.Add(item);
            }

            watch.Stop();
            Debug.WriteLine($"{total/10000}万数据写入总耗时：{watch.ElapsedMilliseconds}ms");

            context.Dispose();
            watch2.Stop();
            Debug.WriteLine($"写入文件总耗时：{watch2.ElapsedMilliseconds}ms");

        }
        [TestMethod]
        public void Read()
        {
            var filepath = "server.PerformanceTest2";
           

            var context = new StorageContext<MyDataItem>("localhost", 8227, filepath, "Name");
            context.GetType().GetMethod("DeleteFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(context, null);

            var item2 = new MyDataItem();
            item2.Name = "awef0";
            context.Add(item2);
            context.Dispose();

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            context = new StorageContext<MyDataItem>("localhost", 8227, filepath, "Name");
            for (int i = 0; i < 100000; i++)
            {
                var item = new MyDataItem();
                item.Name = "Jack" + i;
                context.Add(item);
            }

            watch.Stop();
            Debug.WriteLine($"总耗时：{watch.ElapsedMilliseconds}ms");

            watch.Reset();
            watch.Start();
            var list = context.Where(m => m.Name.Contains("ack1")).ToArray();
            watch.Stop();
            Debug.WriteLine($"读取总耗时：{watch.ElapsedMilliseconds}ms");
        }

    }

}
