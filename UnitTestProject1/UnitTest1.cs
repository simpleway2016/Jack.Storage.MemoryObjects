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

                throw new Exception("�ظ�����Ҳ�����");
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
                throw new Exception("��������");

            context.Dispose();

            context = new StorageContext<MyDataItem>(filepath, "Name");
            if (context.Count != 10000)
                throw new Exception("��������");

            for(int i = 0; i < 10000; i ++)
            {
                if(context.Any(m=>m.Name == "Jack" + i) == false)
                    throw new Exception("û���������");
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
                throw new Exception("��������");

            var obj = context.FirstOrDefault(m => m.Name == "Jack5");
            obj.Company = "abc";
            context.Update(obj);

            context.Dispose();



            context = new StorageContext<MyDataItem>(filepath, "Name");
            if (context.Count != 10)
                throw new Exception("��������");

            obj = context.FirstOrDefault(m => m.Name == "Jack5");
            if (obj.Company != "abc")
                throw new Exception("���ݴ���");

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
                throw new Exception("��������");

            context.Dispose();

            context = new StorageContext<MyDataItem>(filepath, "Name");
            if (context.Count != 10000)
                throw new Exception("��������");

            var datas = context.ToArray();
            System.Threading.Tasks.Parallel.For(0, datas.Length, (i) => {
                context.Remove(datas[i]);
            });


            context.Dispose();

            context = new StorageContext<MyDataItem>(filepath, "Name");
            if (context.Count != 0)
                throw new Exception("��������");


            System.IO.File.Delete(filepath);
        }
    }

    class MyDataItem
    {
        public string Name { get; set; }
        public string Company { get; set; }
    }
}
