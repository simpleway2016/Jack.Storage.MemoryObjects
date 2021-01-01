using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Jack.Storage.MemoryObjects;
using System.Diagnostics;
using System.Linq;
using Jack.Storage.MemoryObjects;
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

        [TestMethod]
        public void TestDelete()
        {
          
            var ctx = new StorageContext<PlayListItem>("PlayList", "Id", @"C:\Users\89687\AppData\Local\Monster Audio\KaraokeData");

            foreach( var item in ctx )
            {
                ctx.Remove(item);
            }
           
            ctx.Dispose();
        }
    }

    public class PlayListItem 
    {
        long _Id;

        public long Id
        {


            get
            {
                return _Id;
            }
            set
            {
                if (_Id != value)
                {
                    _Id = value;
                   
                }
            }
        }

        long _SongId;
        public long SongId
        {
            get
            {
                return _SongId;
            }
            set
            {
                if (_SongId != value)
                {
                    _SongId = value;
                }
            }
        }

        int _OrderNumber;
        public int OrderNumber
        {
            get
            {
                return _OrderNumber;
            }
            set
            {
                if (_OrderNumber != value)
                {
                    _OrderNumber = value;
                }
            }
        }

        bool _IsPlaying;
        public bool IsPlaying
        {
            get
            {
                return _IsPlaying;
            }
            set
            {
                if (_IsPlaying != value)
                {
                    _IsPlaying = value;
                }
            }
        }

    }
}
