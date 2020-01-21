using Jack.Storage.MemoryObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Linq;
using System;
using System.Diagnostics;

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
            

            var context = new StorageContext<MyDataItem>( "localhost" , 8227 , filepath, "Name");
            context.GetType().GetMethod("DeleteFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(context, null);

            var item2 = new MyDataItem();
            item2.Name = "awef0";
            context.Add(item2);
            context.Dispose();

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch watch2 = new System.Diagnostics.Stopwatch();
            watch2.Start();
            watch.Start();
            context = new StorageContext<MyDataItem>("localhost", 8227, filepath, "Name");
            for (int i = 0; i < 100000; i ++)
            {
                var item = new MyDataItem();
                item.Name = "Jack" + i;
                context.Add(item);
            }

            watch.Stop();
            Debug.WriteLine($"�ܺ�ʱ��{watch.ElapsedMilliseconds}ms");

            context.Dispose();
            watch2.Stop();
            Debug.WriteLine($"д���ļ��ܺ�ʱ��{watch2.ElapsedMilliseconds}ms");

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
            Debug.WriteLine($"�ܺ�ʱ��{watch.ElapsedMilliseconds}ms");

            watch.Reset();
            watch.Start();
            var list = context.Where(m => m.Name.Contains("ack1")).ToArray();
            watch.Stop();
            Debug.WriteLine($"��ȡ�ܺ�ʱ��{watch.ElapsedMilliseconds}ms");
        }

    }

}
