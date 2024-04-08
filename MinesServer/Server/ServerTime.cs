using MinesServer.GameShit.Entities.PlayerStaff;
using MinesServer.GameShit.GUI;
using MinesServer.GameShit.WorldSystem;
using System.Diagnostics;

namespace MinesServer.Server
{
    public class ServerTime
    {
        public delegate void GameAction();
        public Queue<(GameAction action,Player initiator)> gameActions;
        private List<TickAction> actions = new();
        public ServerTime()
        {
            StartTimeUpdate();
            programmatorUpdate();
            gameActions = new Queue<(GameAction,Player)>();
        }
        public void AddAction(GameAction action,Player p)
        {
            if (ServerTime.Now < directactiondelay) return;
            gameActions.Enqueue((action,p));
            directactiondelay = Now + TimeSpan.FromMicroseconds(5);
        }
        private static DateTimeOffset directactiondelay = ServerTime.Now;
        public static int offset;
        public static DateTimeOffset Now { get; private set; }
        public void StartTimeUpdate()
        {

            Task.Run(() =>
            {
                while (true)
                {
                    var d = DateTimeOffset.Now;
                    offset = (int)(Now-d).TotalMicroseconds;
                    Now = d;
                    Thread.Sleep(TimeSpan.FromMicroseconds(50));
                }
            });
        }
        const int tps = 128;
        public void AddTickRateUpdate(Action body)
        {
            Task.Run(() =>
            {
                    var lasttick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    while (true)
                    {
                        int ticksToProcess = (int)((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lasttick) / 1000f * tps);
                        if (ticksToProcess > 0)
                        {
                            if (ticksToProcess > 1)
                            {
                                Console.WriteLine($"overload {ticksToProcess}");;
                            }
                            while (ticksToProcess-- > 0) body();
                            lasttick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                    }
            });
        }
        public void programmatorUpdate()
        {
            Task.Run(() =>
            {
                while(true)
                { 
                var players = DataBase.activeplayers;
                for (int i = 0; i < players.Count; i++)
                {
                    if (players.Count > i)
                    {
                        players[i]?.ProgrammatorUpdate();
                    }
                }
                Thread.Sleep(TimeSpan.FromMicroseconds(50));
                    }
            });
        }
        public void Start()
        {
            actions.Add(new(() =>
            {
                for (int i = 0; i < gameActions.Count; i++)
                {
                    var item = gameActions.Dequeue();
                    /*try
                    {*/
                    if (item.action != null)
                    {
                        item.action();
                    }
                    /*}
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item.initiator.name}[{item.initiator.id}] caused {ex}");
                    }*/
                }
            }, "base"));
            actions.Add(new(() =>
            {
                var players = DataBase.activeplayers;
                for (int i = 0; i < players.Count; i++)
                {
                    if (players.Count > i)
                    {
                        players[i]?.Update();
                    }
                }
            }, "players"));
            actions.Add(new(() =>
            {
                for (int x = 0; x < World.ChunksW; x++)
                {
                    for (int y = 0; y < World.ChunksH; y++)
                    {
                        World.W.chunks[x, y].Update();
                    }
                }
                World.Update();
                World.W.cells.Commit();
                World.W.road.Commit();
                World.W.durability.Commit();
            },"chunks"));
            AddTickRateUpdate(Update);
        }
        public void Update()
        {
            if (!MServer.started)
                return;
            foreach (var i in actions)
                i.Call();
            using var db = new DataBase();
            foreach (var order in db.orders)
            {
                order.CheckReady();
            }
            db.SaveChanges();
        }
    }
}
