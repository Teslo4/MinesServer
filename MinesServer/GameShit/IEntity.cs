using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinesServer.GameShit
{
    public interface IEntity
    {
        protected abstract int Health { get; set; }
        protected abstract int MaxHealth { get; set; }
        public abstract void Heal(int num = -1);
        public abstract void Hurt(int num, DamageType type = DamageType.Pure);
        public abstract void Death();
    }
}
