# Jack.Storage.MemoryObjects
内存对象数据库，内存中对象，自动同步到磁盘文件中，重启后，对象不丢失

```
            var filepath = "./temp.db";

            var context = new StorageContext<MyDataItem>(filepath, "Name");
            context.Add(new MyDataItem() { 
                Name = "MyName1",
                Company = "Microsoft"
            });

            context.Add(new MyDataItem()
            {
                Name = "MyName2",
                Company = "Microsoft"
            });

            context.Remove(context.Where(m => m.Name.Contains("My")));

            context.Dispose();

```
