using MinesServer.GameShit.Entities;
using MinesServer.GameShit.Entities.PlayerStaff;
using MinesServer.GameShit.GUI;
using MinesServer.GameShit.Programmator;
using MinesServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinesServer.GameShit.Buildings
{
    public class Spot : Pack, IDamagable
    {
        public override PackType type => PackType.Spot;

        public DateTime brokentimer { get; set; }
        public int hp { get; set; }
        public int maxhp { get; set; }
        public Program? selected { get; set; }
        public BotSpot? entity;
        private Spot() { }
        public Spot(int x, int y, int ownerid) : base(x, y, ownerid)
        {
            maxhp = 100;
            hp = 100;
            using var db = new DataBase();
            //db.spots.Add(this);
            db.SaveChanges();
        }
        public void Destroy(Player p)
        {
            throw new NotImplementedException();
        }

        public override Window? GUIWin(Player p)
        {
            return null;
        }

        protected override void ClearBuilding()
        {
            
        }
    }
}
