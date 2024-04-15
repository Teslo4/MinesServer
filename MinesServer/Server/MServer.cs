﻿using MinesServer.GameShit;
using MinesServer.GameShit.WorldSystem;
using NetCoreServer;
using System.Net;
using System.Net.Sockets;
namespace MinesServer.Server
{
    public class MServer : TcpServer
    {
        public System.Timers.Timer timer;
        public ServerTime time { get; private set; }
        public static MServer? Instance;
        public static bool started = false;

        public int online
        {
            get => DataBase.activeplayers.Count;
        }
        public MServer(IPAddress address, int port) : base(address, port)
        {
            Instance = this;
            MConsole.InitCommands();
            GameShit.SysCraft.RDes.Init();
            new World(Default.cfg.WorldName);
            time = new ServerTime();
            SessionsCheck();
            Authmk2.Start();
            OptionKeepAlive = true;
        }
        protected override TcpSession CreateSession()
        {
            var s = new Session(this);
            return s;
        }
        private void SessionsCheck()
        {
            var lastcheck = ServerTime.Now;
            Task.Run(() =>
            {
                while (true)
                {
                    if (ServerTime.Now - lastcheck > TimeSpan.FromSeconds(30))
                    {
                        foreach (Session i in Instance.Sessions.Values) i.CheckDisconnected();

                    }
                    Thread.Sleep(5);
                }
            });
        }
        protected override void OnError(SocketError error)
        {
            Default.WriteError(error.ToString());
        }
    }
}
