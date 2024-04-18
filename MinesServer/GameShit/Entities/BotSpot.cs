using MinesServer.GameShit.Entities.PlayerStaff;
using MinesServer.GameShit.Enums;
using MinesServer.GameShit.Programmator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinesServer.GameShit.Entities
{
    public class BotSpot : BaseEntity
    {
        public ProgrammatorData pd;
        public int id { get; set; }
        private BotSpot() => pd = new(this);
        public BotSpot(int x,int y,Player owner) : this() {
            this.x = x;this.y = y;this.owner = owner;
            /*
             * this should be in db but ...But i should think bout how to make it more simple
           *using var db = new DataBase();
           *db.add to spots list
           *savechanges()
           */
        }
        public int tail => 1;
        public int skin => 3;
        public int cid => owner.cid;
        public Player? owner { get; set; }
        public override Basket? crys { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Build(string type)
        {
        }

        public override void Bz()
        {
            
        }

        public override void Death()
        {
           
        }

        public override void Geo()
        {
            
        }

        public override bool Heal(int num = -1)
        {
            return false;
        }

        public override void Hurt(int num, DamageType type = DamageType.Pure)
        {
            
        }

        public override bool Move(int x, int y, int dir = -1, bool prog = false)
        {
            return false;
        }

        public override void Update()
        {
            
        }
    }
}
