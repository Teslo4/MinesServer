using MinesServer.GameShit.Entities.PlayerStaff;
using MinesServer.GameShit.GUI;
using MinesServer.GameShit.WorldSystem;
using System;
using System.Collections.Generic;
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

        public void Destroy(Player p)
        {
            ClearBuilding();
            World.RemovePack(x, y);
            /*using var db = new DataBase();
            db.storages.Remove(this);
            db.SaveChanges();*/
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
