using MinesServer.GameShit;
using MinesServer.GameShit.GUI;
using System.Diagnostics;

namespace MinesServer.Server
{
    public class ServerTime
    {
        public delegate void GameAction();
        public Queue<(GameAction action,Player initiator)> gameActions;
        public ServerTime()
        {
            gameActions = new Queue<(GameAction,Player)>();
        }
        public void AddAction(GameAction action,Player p)
        {
            gameActions.Enqueue((action,p));
        }
        public static DateTimeOffset Now { get; private set; }
        public void StartTimeUpdate()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Now = DateTimeOffset.UtcNow;
                    Thread.Sleep(TimeSpan.FromMicroseconds(0.1));
                }
            });
        }
        const int tps = 128;
        public void AddTickRateUpdate(Action body)
        {
            Task.Run(() =>
            {
                while(true)
                {
                    var lasttick = Now.ToUnixTimeMilliseconds();
                    while (true)
                    {
                        int ticksToProcess = (int)((Now.ToUnixTimeMilliseconds() - lasttick) / 1000f * tps);
                        if (ticksToProcess > 0)
                        {
                            if (ticksToProcess > 1)
                            {
                                Console.WriteLine("overload");
                            }
                            while (ticksToProcess-- > 0) body();
                            lasttick = Now.ToUnixTimeMilliseconds();
                        }
                    }
                }
            });
        }

        public void Start()
        {
            StartTimeUpdate();
            AddTickRateUpdate(PlayersUpdate);
            AddTickRateUpdate(GameActionsUpdate);
            AddTickRateUpdate(ChunksUpdate);
            AddTickRateUpdate(Update);
        }
        private void ChunksUpdate()
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
        }
        private void GameActionsUpdate()
        {
            for (int i = 0; i < gameActions.Count; i++)
            {
                var item = gameActions.Dequeue();
                try
                {
                if (item.action != null)
                {
                    item.action();
                }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{item.initiator.name}[{item.initiator.Id}] caused {ex}");
                }
            }
        }
        private void PlayersUpdate()
        {
            for (int i = 0; i < DataBase.activeplayers.Count; i++)
            {
                using var dbas = new DataBase();
                if (DataBase.activeplayers.Count > i)
                {
                    var player = DataBase.GetPlayer(DataBase.activeplayers.ElementAt(i).Id);
                    player?.Update();
                }
            }
        }
        public void Update()
        {
            if (!MServer.started)
            {
                return;
            }
            using var db = new DataBase();
            foreach (var order in db.orders)
            {
                order.CheckReady();
            }
            db.SaveChanges();
        }
    }
}
