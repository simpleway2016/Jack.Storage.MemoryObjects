using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Jack.Storage.MemoryObjects;
using System.Diagnostics;
using System.Linq;

namespace UnitTestProject2
{
    class MyObject
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var ctx = new Jack.Storage.MemoryObjects.StorageContext<MyObject>("MyObject" , "Name");

            var item = ctx.FirstOrDefault(m => m.Name == "jack10");

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            for(int i = 0; i < 100000; i ++)
            {
                ctx.Add(new MyObject { 
                    Name = "jack" + i,
                    Age = i
                });
            }
            sw.Stop();
            Debug.WriteLine("耗时" + sw.ElapsedMilliseconds);
            ctx.Dispose();
        }
    }
}
