using MinesServer.GameShit.Buildings;
using MinesServer.GameShit.Entities.PlayerStaff;
using MinesServer.GameShit.GUI;
using MinesServer.GameShit.WorldSystem;
using MinesServer.Network.HubEvents;
using MinesServer.Network.World;
using MinesServer.Server;

namespace MinesServer.GameShit.VulkSystem
{
    public class Vulkan : Pack
    {
        public override PackType type => PackType.Vulkan;
        private Vulkan() { }
        public Vulkan(int x,int y) : base(x,y,0)
        {
            using var db = new DataBase();
            db.vulkans.Add(this);
            db.SaveChanges();
        }
        public override void Build()
        {
            base.Build();
        }
        public override Window? GUIWin(Player p) => null;

        protected override void ClearBuilding()
        {

        }
        public void Destroy(Player p)
        {
            ClearBuilding();
            World.RemovePack(x, y);
            using var db = new DataBase();
            db.vulkans.Remove(this);
            db.SaveChanges();
        }
    }
}
