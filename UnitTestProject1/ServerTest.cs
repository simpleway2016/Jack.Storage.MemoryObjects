using Jack.Storage.MemoryObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Linq;
using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Jack.Storage.MemoryObjects.Server;

namespace UnitTestProject1
{
    [TestClass]
    public class ServerTest
    {
        static Server Server;
        static ServiceCollection Services;
        static IServiceProvider Provider;
        static ServerTest()
        {
            Services = new ServiceCollection();
            Services.AddSingleton<Config>(Config.GetAppConfig());
            Provider = Services.BuildJackServiceProvider();

            Server = Provider.GetService<Server>();

            Server.Run();
        }

        [TestMethod]
        public void Test()
        {
            var name = "ServerTest.Test";

            var context = new StorageContext<MyDataItem>("localhost" , 8227 , name, "Name");
            context.GetType().GetMethod("DeleteFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(context, null);

            context.Add(new MyDataItem()
            {
                Name = "MyName1",
                Company = "Microsoft"
            });
            context.Add(new MyDataItem()
            {
                Name = "MyName2",
                Company = "abc"
            });

            context.Dispose();

             context = new StorageContext<MyDataItem>("localhost", 8227, name, "Name");
            if (context.Count != 2)
                throw new Exception("��������");

            if(context.FirstOrDefault(m=>m.Name == "MyName2") == null)
                throw new Exception("���ݲ���");

          
            context.Dispose();
        }

        [TestMethod]
        public void Add()
        {
            var name = "ServerTest.Add";


            var context = new StorageContext<MyDataItem>("localhost", 8227, name, "Name");
            context.GetType().GetMethod("DeleteFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(context, null);

            System.Threading.Tasks.Parallel.For(0, 100, (i) => {
                var item = new MyDataItem();
                item.Name = "Jack" + i;
                context.Add(item);
            });

            context.Dispose();

            context = new StorageContext<MyDataItem>("localhost", 8227, name, "Name");
            if (context.Count != 100)
                throw new Exception("��������,��ǰ����:" + context.Count);

            for (int i = 0; i < 100; i++)
            {
                if (context.Any(m => m.Name == "Jack" + i) == false)
                    throw new Exception("û���������");
            }

            context.Dispose();
        }

        [TestMethod]
        public void Update()
        {
            var name = "ServerTest.Update";


            var context = new StorageContext<MyDataItem>("localhost", 8227, name, "Name");
            context.GetType().GetMethod("DeleteFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(context, null);

            System.Threading.Tasks.Parallel.For(0, 10, (i) => {
                var item = new MyDataItem();
                item.Name = "Jack" + i;
                context.Add(item);
            });

            if (context.Count != 10)
                throw new Exception("��������");

            var obj = context.FirstOrDefault(m => m.Name == "Jack5");
            obj.Company = "abc";
            context.Update(obj);

            context.Dispose();



            context = new StorageContext<MyDataItem>("localhost", 8227, name, "Name");
            if (context.Count != 10)
                throw new Exception("��������");

            obj = context.FirstOrDefault(m => m.Name == "Jack5");
            if (obj.Company != "abc")
                throw new Exception("���ݴ���");

            context.Dispose();
        }


        [TestMethod]
        public void Remove()
        {
            var name = "ServerTest.Remove";


            var context = new StorageContext<MyDataItem>("localhost", 8227, name, "Name");
            context.GetType().GetMethod("DeleteFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(context, null);

            System.Threading.Tasks.Parallel.For(0, 100, (i) => {
                var item = new MyDataItem();
                item.Name = "Jack" + i;
                context.Add(item);
            });

            if (context.Count != 100)
                throw new Exception("��������");

            context.Dispose();

            context = new StorageContext<MyDataItem>("localhost", 8227, name, "Name");
            if (context.Count != 100)
                throw new Exception("��������");

            var datas = context.ToArray();
            System.Threading.Tasks.Parallel.For(0, datas.Length, (i) => {
                context.Remove(datas[i]);
            });


            context.Dispose();

            context = new StorageContext<MyDataItem>("localhost", 8227, name, "Name");
            if (context.Count != 0)
                throw new Exception("��������");


            context.Dispose();
        }
    }

}
