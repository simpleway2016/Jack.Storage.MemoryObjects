using System;
using System.Net.Sockets;
using System.Text;

namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener tcpListener = new TcpListener(9900);
            tcpListener.Start();
            var socket = tcpListener.AcceptSocket();
            byte[] data = new byte[1024];
            var readed = socket.Receive(data);
            Console.WriteLine(Encoding.UTF8.GetString(data));
            Console.ReadKey();
        }
    }
}
