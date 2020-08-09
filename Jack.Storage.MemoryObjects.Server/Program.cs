using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace Jack.Storage.MemoryObjects.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton<Config>(Config.GetAppConfig());
            var provider = services.BuildJackServiceProvider();

            var server = provider.GetService<Server>();
            try
            {
                server.Run();
                Console.WriteLine("server started,port:" + server.Config.Port);
                ManualResetEvent _mainEvent = new ManualResetEvent(false);
                _mainEvent.WaitOne();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
          
        }
    }
}
