using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinesServer.Server
{
    public class TickAction
    {
        public TickAction(Action a) { body = a; }
        private Action body;
        public void Call()
        {
            if (completed)
            {
                completed = false;
                Task.Run(() =>
                {
                    body();
                    completed = true;
                });
            }
        }
        private bool completed = true;
    }
}
