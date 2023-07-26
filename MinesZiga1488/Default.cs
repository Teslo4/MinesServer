﻿using MinesServer.GameShit;
using MinesServer.Server;
using Newtonsoft.Json;

namespace MinesServer
{
    public static class Default
    {
        public static int port = 8090;
        private static Dictionary<string, Action> commands = new Dictionary<string, Action>();
        public static void Main(string[] args)
        {
            CellsSerializer.Load();
            var configPath = "config.json";
            if (File.Exists(configPath))
            {
                cfg = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
            }
            else
            {
                cfg = new Config();
                File.WriteAllText(configPath, JsonConvert.SerializeObject(cfg, Formatting.Indented));
            }
            server = new MServer(System.Net.IPAddress.Any, port);
            server.Start();
            Loop();

        }
        private static void Loop()
        {
            commands.Add("restart", () => { server.Stop(); Console.WriteLine("kinda restart"); server.Start(); });
            commands.Add("players", () => {
                Console.WriteLine($"online {server.players.Count}");
                for (int i = 0; i < server.players.Count;i++)
                {
                    Console.WriteLine($"id: {server.players.ElementAt(i).Value.player.Id}\n name :[{server.players.ElementAt(i).Value.player.name}]");
                }
            });
            for (; ; )
            {
                var l = Console.ReadLine();
                if (commands.Keys.Contains(l))
                {
                    commands[l]();
                }
            }
        }
        public static Config cfg;
        public static void WriteError(string ex)
        {
            var trace = new System.Diagnostics.StackTrace();
            var method = trace.GetFrame(1).GetMethod().Name;
            Console.WriteLine($"{method} caused error {ex}");
        }

        public static MServer server { get; set; }
    }
}