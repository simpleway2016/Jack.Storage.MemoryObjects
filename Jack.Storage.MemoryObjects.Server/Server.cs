using Jack.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Jack.Storage.MemoryObjects.Server
{
    [DependencyInjection]
    public class Server
    {
        public Config Config { get; private set; }
        int activeHandleCount = 0;
       
        TcpListener _tcpListener;
        public Server()
        {

        }

        void handleSocket(object s)
        {
            Interlocked.Increment(ref activeHandleCount);
            try
            {
                var socket = (Socket)s;
                var connect = new ConnectionHandler(socket);
                connect.Handle();
            }
            catch (Exception ex)
            {
                Way.Lib.CLog log = new Way.Lib.CLog("Handle Error", false);
                log.Log(ex.ToString());
            }
            Interlocked.Decrement(ref activeHandleCount);
        }

        void start()
        {
           
            while(true)
            {
                try
                {
                    var socket = _tcpListener.AcceptSocket();
                    new Thread(handleSocket).Start(socket);
                }
                catch(Exception ex)
                {
                    Way.Lib.CLog log = new Way.Lib.CLog("ServerRunning Error", false);
                    log.Log(ex.ToString());
                }
            }
        }

        public void Run()
        {
            _tcpListener = new TcpListener(Config.Port);
            _tcpListener.Start();

            new Thread(start).Start();

        }
    }
}
