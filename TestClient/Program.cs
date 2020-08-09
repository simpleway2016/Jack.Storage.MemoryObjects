using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(2000);
            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("localhost", 9900);
            tcpClient.Client.NoDelay = true;
            
            tcpClient.Client.Send(Encoding.UTF8.GetBytes("abc"));

            System.Diagnostics.Process.GetCurrentProcess().Kill();
            Console.WriteLine("Hello World!");
        }
    }
}
