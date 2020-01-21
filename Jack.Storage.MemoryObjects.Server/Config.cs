using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Way.Lib;
namespace Jack.Storage.MemoryObjects.Server
{
    public class Config
    {
		public int Port
		{
			get;
			set;
		}

		public static Config GetAppConfig()
		{
			return File.ReadAllText("./appsetting.json", Encoding.UTF8).FromJson<Config>();
		}
    }
}
