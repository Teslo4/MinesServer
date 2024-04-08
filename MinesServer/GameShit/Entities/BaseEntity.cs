using MinesServer.GameShit.Entities.PlayerStaff;
using MinesServer.GameShit.Enums;
using MinesServer.GameShit.Programmator;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinesServer.GameShit.Entities
{
    public abstract class BaseEntity
    {
        public ProgrammatorData programsData
        {
            get
            {
                _pdata ??= new ProgrammatorData(this);
                return _pdata;
            }
        }
        [NotMapped]
        public virtual Stack<byte> geo { get; set; } = new();
        protected ProgrammatorData? _pdata { get; set; }
        public abstract Basket? crys { get; set; }
        public virtual int Health { get; set; }
        public virtual int MaxHealth { get; set; }
        public virtual int pause { get; set; }
        public virtual double ServerPause { get; }
        public int dir { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public abstract void Build(string type);
        public abstract void Bz();
        public abstract void Geo();
        public abstract bool Heal(int num = -1);
        public abstract void Hurt(int num, DamageType type = DamageType.Pure);
        public abstract void Death();
        public abstract bool Move(int x, int y, int dir = -1,bool prog = false);
        public abstract void Update();
        public (int x, int y) GetDirCord(bool pack = false)
        {
            var x = (this.x + (dir == 3 ? 1 : dir == 1 ? -1 : 0));
            var y = (this.y + (dir == 0 ? 1 : dir == 2 ? -1 : 0));
            if (pack)
            {
                x = (this.x + (dir == 3 ? 2 : dir == 1 ? -2 : 0));
                y = (this.y + (dir == 0 ? 2 : dir == 2 ? -2 : 0));
            }
            return (x, y);
        }
    }
}
