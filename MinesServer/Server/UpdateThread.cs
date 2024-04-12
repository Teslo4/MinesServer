using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Text;
using System.Linq;
using System.Net.Quic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MinesServer.Server
{
    public class UpdateThread<T> : Dictionary<T,Action>
    {
        public UpdateThread() => new Thread(Update).Start();
        private void Update()
        {
            while(true)
            {
                if (Count == 0)
                    Thread.Sleep(1);
                while (Count > 0)
                    lock (qlock)
                    {
                        Dequeue().body();
                    }
            }
        }
        public void Enqueue(T key,Action body)
        {
            lock (qlock)
            {
                if (!ContainsKey(key))
                    Add(key, body);
            }
        }
        private (T key, Action body) Dequeue()
        {
            var key = Keys.First();
            var result = (key, this[key]);
            Remove(key);
            return result;
        }
        readonly object qlock = new();
    }
}
