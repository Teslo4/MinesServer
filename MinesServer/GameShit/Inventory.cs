﻿using MinesServer.GameShit.Buildings;
using MinesServer.Network;
using MinesServer.Network.GUI;
using MinesServer.Server;
using System.ComponentModel.DataAnnotations.Schema;
namespace MinesServer.GameShit
{
    public class Inventory
    {
        public int Id { get; set; }
        public Inventory()
        {
            typeditems = new Dictionary<int, ItemUsage>
            {
                {
                    0,(p) =>
                    {
                        return true;
                    }
                },
                {
                    1,(p) =>
                    {
                        new Resp((int)p.GetDirCord(true).X,(int)p.GetDirCord(true).Y,p.Id).Build();
                        return true;
                    }
                },
                {
                    2,(p) =>
                    {
                        return true;
                    }
                },
                {
                    3,(p) =>
                    {
                        return true;
                    }
                },
                {
                    4,(p) =>
                    {
                        return true;
                    }
                },
                {
                    5,(p) =>{
                    World.Boom((int)p.GetDirCord().X,(int)p.GetDirCord().Y);
                        return true;
                        }
                }
            };
        }
        public bool canplace(int x,int y)
        {
            return true;
        }
        public void SetItem(int id, int col)
        {
            var x = items;
            x[id] = col;
            items = x;
            using var db = new DataBase();
            db.SaveChanges();
        }
        private Dictionary<int, int> getinv()
        {
            var dick = new Dictionary<int, int>();
            var t = "";
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] > 0)
                {
                    dick[i] = items[i];
                }
            }
            return dick;
        }
        public InventoryPacket InvToSend()
        {
            return new InventoryPacket(new InventoryShowPacket(getinv(), selected, Lenght));
        }
        public void Use(Player p)
        {
            if (typeditems.ContainsKey(selected) && World.GetProp(World.GetCell((int)p.GetDirCord().X, (int)p.GetDirCord().Y)).can_place_over)
            {
                typeditems[selected](p);
            }
        }
        public Dictionary<int, ItemUsage> typeditems;
        public delegate bool ItemUsage(Player p);
        public void Choose(int id, Player p)
        {
            IDataPartBase packet = InventoryPacket.Choose("ты хуесос", new bool[0, 0], 123, 123, 12);
            selected = id;
            if (id == -1)
            {
                packet = new InventoryClosePacket();
            }
            p.connection.SendU(packet);
        }
        public int selected = -1;
        [NotMapped]
        public int Lenght
        {
            get
            {
                var l = 0;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] > 0)
                    {
                        l++;
                    }
                }
                return l;
            }
        }
        public string itemstobd { get; set; }
        [NotMapped]
        private int[] iret = null;
        [NotMapped]
        public int[] items
        {
            get
            {
                if (iret == null)
                {
                    var splited = itemstobd.Split(";");
                    var i = new int[49];
                    if (splited.Length > 0)
                    {
                        for (var it = 0; it < splited.Length; it++)
                        {
                            i[it] = int.Parse(splited[it]);
                        }
                    }
                    iret = i;
                }
                return iret;
            }
            set
            {
                itemstobd = string.Join(';', value);
            }
        }
    }
}
