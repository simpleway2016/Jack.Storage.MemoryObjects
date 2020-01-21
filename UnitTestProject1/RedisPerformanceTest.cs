using Jack.Storage.MemoryObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Linq;
using System;
using System.Diagnostics;

namespace UnitTestProject1
{
    [TestClass]
    public class RedisPerformanceTest
    {
      

        [TestMethod]
        public void Add()
        {
            var rds = new CSRedis.CSRedisClient("127.0.0.1:6379,password=,defaultDatabase=8,poolsize=50,ssl=false,writeBuffer=10240");

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch watch2 = new System.Diagnostics.Stopwatch();
            watch2.Start();
            watch.Start();

            for(int i = 0; i < 1000000; i ++)
            {
                var item = new MyDataItem();
                item.Name = "Jack" + i;

                rds.Set(item.Name, item);
            }

            watch.Stop();
            Debug.WriteLine($"×ÜºÄÊ±£º{watch.ElapsedMilliseconds}ms");

           
        }

       

    }

}
