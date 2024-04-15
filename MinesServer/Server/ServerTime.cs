using MinesServer.GameShit.Entities.PlayerStaff;
using MinesServer.GameShit.GUI;
using MinesServer.GameShit.WorldSystem;
using System.ComponentModel.Design;
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
            Now = DateTime.Now;
            gameActions = new Queue<(GameAction,Player)>();
            StartTimeUpdate();
            StupidUpdate(() =>
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
            },10);
            StupidUpdate(() =>
            {
                var players = DataBase.activeplayers;
                for (int i = 0; i < players.Count; i++)
                {
                    players[i]?.Update();
                }
            },10);
            StupidUpdate(() =>
            {
                using var db = new DataBase();
                foreach (var order in db.orders)
                {
                    order.CheckReady();
                }
                db.SaveChanges();
            },1000);
            ChunksUpdateSlised();
            programmatorUpdate();
        }
        private void StupidUpdate(Action a,double delay)
        {
            Stopwatch m = Stopwatch.StartNew();
            Task.Run(() =>
            {
                while (true)
                {
                    m.Restart();
                    a();
                    m.Stop();
                    Thread.Sleep(TimeSpan.FromMilliseconds(delay));
                }
            });
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
        public void programmatorUpdate()
        {
            Task.Run(() =>
            {
                while(true)
                { 
                    var players = DataBase.activeplayers.Where(i => i.programsData.ProgRunning);
                    for (int i = 0; i < players.Count(); i++)
                    {
                        players.ElementAt(i)?.ProgrammatorUpdate();
                    }
                    Thread.Sleep(1);
                }
            });
        }
        public void ChunksUpdateSlised()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var first = Task.Run(() =>
                    {
                        for (int y = 0; y < World.ChunksH; y++)
                        {
                            for (int x = 0; x < World.ChunksW; x++)
                            {
                               if (x % 2 == 0) World.W.chunks[x, y].Update(); 
                            }
                        }
                    });
                    var second = Task.Run(() =>
                    {
                        for (int y = 0; y < World.ChunksH; y++)
                        {
                            for (int x = 0; x < World.ChunksW; x++)
                            {
                                if (x % 2 == 1) World.W.chunks[x, y].Update();
                            }
                        }
                    });

                    Task.WaitAll(first, second);
                    World.Update();
                    World.W.cells.Commit();
                    World.W.road.Commit();
                    World.W.durability.Commit();
                    Thread.Sleep(1);
                }
            });
        }
        public void Update()
        {
            if (!MServer.started)
                return;
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
            var players = DataBase.activeplayers;
            for (int i = 0; i < players.Count; i++)
            {
                players[i]?.Update();
            }
            using var db = new DataBase();
            foreach (var order in db.orders)
            {
                order.CheckReady();
            }
            db.SaveChanges();
            Thread.Sleep(1);
        }
    }
}
