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
            gameActions = new Queue<(GameAction,Player)>();
        }
        public void AddAction(GameAction action,Player p)
        {
            gameActions.Enqueue((action,p));
        }
        public static int offset;
        public static DateTimeOffset Now { get; private set; }
        public void StartTimeUpdate()
        {

            Task.Run(() =>
            {
                while (true)
                {
                    var d = DateTimeOffset.UtcNow;
                    offset = (int)(Now-d).TotalMilliseconds;
                    Now = d;
                    Thread.Sleep(TimeSpan.FromMicroseconds(0.1));
                }
            });
        }
        const int tps = 128;
        public void AddTickRateUpdate(Action body)
        {
            Task.Run(() =>
            {
                    var lasttick = Now.ToUnixTimeMilliseconds();
                    while (true)
                    {
                        int ticksToProcess = (int)((Now.ToUnixTimeMilliseconds() - lasttick) / 1000f * tps);
                        if (ticksToProcess > 0)
                        {
                            if (ticksToProcess > 1)
                            {
                                Console.WriteLine($"overload {ticksToProcess}");;
                            }
                            while (ticksToProcess-- > 0) body();
                            lasttick = Now.ToUnixTimeMilliseconds();
                        }
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
                        Console.WriteLine($"{item.initiator.name}[{item.initiator.Id}] caused {ex}");
                    }*/
                }
            }));
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
            }));
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
            }));
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
