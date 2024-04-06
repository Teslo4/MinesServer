using MinesServer.GameShit.Entities.PlayerStaff;
using MinesServer.GameShit.GUI;
using MinesServer.GameShit.GUI.Horb;
using MinesServer.GameShit.GUI.Horb.Canvas;
using MinesServer.GameShit.WorldSystem;
using MinesServer.Network.HubEvents;
using MinesServer.Network.World;
using MinesServer.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinesServer.GameShit.Buildings
{
    public class Teleport : Pack, IDamagable
    {
        public DateTime brokentimer { get; set; }
        public float charge { get; set; }
        public float maxcharge { get; set; }
        public int hp { get; set; }
        public int maxhp { get; set; }
        public int cost { get; set; }
        [NotMapped]
        public override int off => charge > 0 ? 1 : 0;
        private Teleport() {}
        public Teleport(int x, int y, int ownerid) : base(x, y, ownerid, PackType.Teleport)
        {
            cost = 10;
            charge = 1000;
            maxcharge = 10000;
            hp = 1000;
            maxhp = 1000;
            using var db = new DataBase();
            db.teleports.Add(this);
            db.SaveChanges();
        }
        #region affectworld
        protected override void ClearBuilding()
        {
            World.SetCell(x, y, 37, true);
            World.SetCell(x, y + 1, 37, true);
            World.SetCell(x + 1, y, 106, true);
            World.SetCell(x + 1, y - 1, 106, true);
            World.SetCell(x + 1, y + 1, 106, true);
            World.SetCell(x - 1, y - 1, 106, true);
            World.SetCell(x - 1, y + 1, 106, true);
            World.SetCell(x, y - 1, 106, true);
            World.SetCell(x - 1, y, 106, true);
        }
        public override void Build()
        {
            World.SetCell(x, y, 37, true);
            World.SetCell(x, y + 1, 37, true);
            World.SetCell(x + 1, y, 106, true);
            World.SetCell(x + 1, y - 1, 106, true);
            World.SetCell(x + 1, y + 1, 106, true);
            World.SetCell(x - 1, y - 1, 106, true);
            World.SetCell(x - 1, y + 1, 106, true);
            World.SetCell(x, y - 1, 106, true);
            World.SetCell(x - 1, y, 106, true);
            base.Build();
        }
        public void Destroy(Player p)
        {
            ClearBuilding();
            World.RemovePack(x, y);
            if (charge > 0)
            {
                var temp =new long[] { 0, 0, 0, 0, (long)charge, 0};
                Box.BuildBox(x, y, temp,null);
            }
            using var db = new DataBase();
            db.teleports.Remove(this);
            db.SaveChanges();
            if (Physics.r.Next(1, 101) < 40)
            {
                p.connection?.SendB(new HBPacket([new HBChatPacket(0, x, y, "ШПАААК ВЫПАЛ")]));
                p.inventory[0]++;
            }
        }
        #endregion
        private CanvasElement[] Buttonsg()
        {
            var chunk = World.W.GetChunkPosByCoords(x, y);
            var st = (chunk.Item1 - 7, chunk.Item2 - 7);
            List<CanvasElement> n = new();
            using var db = new DataBase();
            foreach (var i in db.teleports.Where(tp => Math.Abs(tp.x - x) < 1000 && Math.Abs(tp.y - y) < 1000))
            {
                n.Add(CanvasElement.TPButton(new MButton($"tp{i.x}:{i.y}", "tp"),0,0,0,0,CanvasElementPivot.Left));
            }
            return n.ToArray();
        }

        public override Window? GUIWin(Player p)
        {
            if (true)
            {
                return null;
            }
            var chunk = World.W.GetChunkPosByCoords(x, y);
            var start = (chunk.Item1 - 7, chunk.Item2 - 7);
            var end = (chunk.Item1 + 7, chunk.Item2 + 7);
            CanvasElement[] canvas = [CanvasElement.Image(ImgSpace.LocateChunks(p.id, start, end), 381, 381, CanvasElementPivot.Default, -98, 0, 0, 8)];
            canvas = canvas.Concat(Buttonsg()).ToArray(); ;
            return new Window() {
                Tabs = [new Tab() {
                    InitialPage = new Page()
                    {
                        Style = new Style(){Canvas = new GridStyle(){Height = 390,Width = 600 } },
                        Canvas = canvas,
                        Buttons = []
                    },
                    Action = "123",
                    Label = "3321",
                    Title = "ТП"}],
                Title = "Тп" };
        }
    }
}
