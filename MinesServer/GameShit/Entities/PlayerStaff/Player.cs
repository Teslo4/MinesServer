using Microsoft.Identity.Client;
using MinesServer.Enums;
using MinesServer.GameShit.Buildings;
using MinesServer.GameShit.ClanSystem;
using MinesServer.GameShit.Enums;
using MinesServer.GameShit.GChat;
using MinesServer.GameShit.GUI;
using MinesServer.GameShit.Programmator;
using MinesServer.GameShit.Skills;
using MinesServer.GameShit.WorldSystem;
using MinesServer.Network;
using MinesServer.Network.BotInfo;
using MinesServer.Network.Chat;
using MinesServer.Network.ConnectionStatus;
using MinesServer.Network.Constraints;
using MinesServer.Network.GUI;
using MinesServer.Network.HubEvents;
using MinesServer.Network.HubEvents.Bots;
using MinesServer.Network.HubEvents.FX;
using MinesServer.Network.HubEvents.Packs;
using MinesServer.Network.Movement;
using MinesServer.Network.Programmator;
using MinesServer.Network.World;
using MinesServer.Server;
using MinesServer.Server.Network;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MinesServer.GameShit.Entities.PlayerStaff
{
    public class Player : BaseEntity
    {
        #region forprogs
        public void RunProgramm(Program p = null)
        {
            if (p == null)
            {
                programsData.Run();
                return;
            }

            programsData.Run(p);
        }
        #endregion
        #region fields
        [NotMapped]
        public Chat? currentchat { get; set; }
        [NotMapped]
        public Session? connection { get; set; }
        [NotMapped]
        public bool online
        {
            get => connection != null;
        }
        public Player() => Delay = ServerTime.Now;
        public DateTimeOffset lastPlayersend = ServerTime.Now;
        public DateTimeOffset lastPacks = ServerTime.Now;
        public DateTimeOffset afkstarttime = ServerTime.Now;
        private DateTimeOffset lastping;
        private DateTimeOffset lastSync = ServerTime.Now;
        public Queue<Action> playerActions = new();
        public int id { get; set; }
        public string name { get; set; }
        public Clan? clan { get; set; }
        public Rank? clanrank { get; set; }
        public override int pause //= 3500;
        {
            get
            {
                var retval = 10000;
                foreach (var c in skillslist.skills.Values)
                {
                    if (c != null && c.UseSkill(SkillEffectType.OnMove, this))
                    {
                        if (c.type == SkillType.Movement)
                        {
                            retval = (int)(c.Effect * 100);
                        }
                    }
                }
                return retval;
            }
        }
        public List<Program> programs { get; set; }
        [NotMapped]
        public int cid { get => clan == null ? 0 : clan.id; }
        public Resp resp { get; set; }
        public long money { get; set; }
        public long creds { get; set; }
        public string hash { get; set; }
        public string passwd { get; set; }
        [NotMapped]
        public int tail { get => programsData.ProgRunning ? 1 : 0; }
        public int skin
        {
            get
            {
                if (online)
                    return _skin;
                return 1;
            }
            set => _skin = value;
        }
        private int _skin;
        public bool autoDig { get; set; }
        public bool agression { get; set; }
        public int c190stacks = 1;
        public DateTimeOffset lastc190hit = ServerTime.Now;
        public Basket crys { get; set; }
        public Inventory inventory { get; set; }
        public Settings settings { get; set; }
        public PlayerSkills skillslist { get; set; }
        public Stack<byte> geo = new Stack<byte>();
        public Queue<Line> console = new Queue<Line>();
        [NotMapped]
        public Window? win;
        [NotMapped]
        private float cb;
        public DateTimeOffset Delay = ServerTime.Now;
        public bool CanAct { get => !(Delay.AddMilliseconds(ServerTime.offset) > ServerTime.Now); }
        public bool OnRoad { get => World.isRoad(World.GetCell(x, y)); }
        public int ChunkX
        {
            get => (int)Math.Floor((float)x / 32);
        }
        public int ChunkY
        {
            get => (int)Math.Floor((float)y / 32);
        }
        public double Pause
        {
            get => World.GetCell(x, y) == 35 ? 2 : 2;
        }
        private void Sync()
        {
            using (var db = new DataBase())
            {
                db.players.Attach(this);
                db.SaveChanges();
            }
        }
        #endregion
        #region actions
        public override void Update()
        {
            var now = ServerTime.Now;
            if (lastping != default)
            {
                if (now - lastping >= TimeSpan.FromSeconds(30))
                {
                    connection?.Disconnect();
                }
                else if (now - lastping >= TimeSpan.FromSeconds(10))
                {
                    SendPing(default);
                }
            }
            if (now - lastSync >= TimeSpan.FromSeconds(30))
            {
                Sync();
                lastSync = now;
            }
            if (now - lastc190hit >= TimeSpan.FromMinutes(1))
            {
                c190stacks = 1;
                lastc190hit = now;
            }
            if (!online)
            {
                if (now - afkstarttime > TimeSpan.FromMinutes(5))
                {
                    DataBase.activeplayers.Remove(this);
                    Death();
                }
                return;
            }
            if (now - lastPlayersend > TimeSpan.FromSeconds(4))
            {
                ReSendBots();
            }
            var cell = World.GetCell(x, y);
            var cellprop = World.GetProp(cell);
            if (!cellprop.isEmpty)
            {
                Hurt(cellprop.fall_damage);
                if (cell == 90)
                {
                    GetBox(x, y);
                    World.DamageCell(x, y, 1);
                }
                else if (cellprop.is_destructible)
                {
                    World.Destroy(x, y);
                }
            }
            if (programsData.ProgRunning)
            {
                programsData.Step();
                return;
            }
        }
        public void SetResp(Resp r) => resp = r;
        public void TryAct(Action a, double delay)
        {
            if (Delay < ServerTime.Now)
            {
                a();
                Delay = ServerTime.Now + TimeSpan.FromMicroseconds(delay * 1.4);
            }
        }
        private int ParseCryType(CellType cell)
        {
            return cell switch
            {
                CellType.XGreen or CellType.Green => 0,
                CellType.XBlue or CellType.Blue => 1,
                CellType.XRed or CellType.Red => 2,
                CellType.XViolet or CellType.Violet => 3,
                CellType.White => 4,
                CellType.XCyan or CellType.Cyan => 5,
                _ => 0
            };
        }
        public override void Geo()
        {
            int x = (int)GetDirCord().x, y = (int)GetDirCord().y;
            if (!World.W.ValidCoord(x, y) || World.GunRadius(x, y, this))
            {
                return;
            }
            var cell = World.GetCell(x, y);
            if (World.GetProp(cell).isPickable && !World.GetProp(cell).isEmpty)
            {
                geo.Push(cell);
                World.Destroy(x, y);
            }
            else if (World.GetProp(cell).isEmpty && World.GetProp(cell).can_place_over && geo.Count > 0 && !World.PackPart(x, y))
            {
                var cplaceable = geo.Pop();
                World.SetCell(x, y, cplaceable);
                World.SetDurability(x, y, World.isCry(cplaceable) ? 0 : Physics.r.Next(1, 101) > 99 ? 0 : World.GetProp(cplaceable).durability);
            }
            this.SendGeo();
        }
        public void BBox(long[]? c)
        {
            var boxc = GetDirCord();
            if (!World.W.ValidCoord(boxc.x, boxc.y) || c == null)
            {
                return;
            }
            Box.BuildBox(boxc.x, boxc.y, c, this);
            connection?.CloseWindow();

        }
        private void Mine(byte cell, int x, int y)
        {
            float dob = 1 + (float)Math.Truncate(cb);
            foreach (var c in skillslist.skills.Values)
            {
                if (c != null && c.UseSkill(SkillEffectType.OnDigCrys, this))
                {
                    if (c.type == SkillType.MineGeneral)
                    {
                        dob += c.Effect;
                        c.AddExp(this, (float)Math.Truncate(dob));
                    }
                }
            }
            dob *= (CellType)cell switch
            {
                CellType.XGreen => 4,
                CellType.XBlue => 3,
                CellType.XRed => 2,
                CellType.XViolet => 2,
                CellType.XCyan => 2,
                _ => 1
            };
            cb -= (float)Math.Truncate(cb);
            long odob = (long)Math.Truncate(dob);
            var type = ParseCryType((CellType)cell);
            cb += dob - odob;
            crys.AddCrys(type, odob);
            World.AddDob(type, odob);
            SendDFToBots(2, x, y, id, (int)(odob < 255 ? odob : 255), type == 1 ? 3 : type == 2 ? 1 : type == 3 ? 2 : type);
        }
        public void GetBox(int x, int y)
        {
            var b = Box.GetBox(x, y);
            if (b == null)
            {
                return;
            }
            crys.Boxcrys(b.bxcrys);
            crys.SendBasket();
            using var db = new DataBase();
            db.Remove(b);
            db.SaveChanges();
            connection?.SendB(new HBPacket([new HBChatPacket(0, x, y, "+ " + b.AllCrys)]));
        }
        private void OnDestroy(byte type)
        {
            foreach (var c in skillslist.skills.Values)
            {
                if (c != null && c.UseSkill(SkillEffectType.OnDig, this))
                {
                    c.AddExp(this);
                }
            }
        }
        public override void Bz()
        {
            var cord = GetDirCord();
            int x = cord.x, y = cord.y;
            if (!World.W.ValidCoord(x, y))
            {
                return;
            }
            SendDFToBots(0, this.x, this.y, id, dir);
            var cell = World.GetCell(x, y);
            if (World.GetProp(cell).damage > 0)
            {
                Hurt(World.GetProp(cell).damage);
            }
            if (!World.GetProp(cell).is_diggable)
            {
                return;
            }
            if (cell == 90)
            {
                GetBox(x, y);
                World.DamageCell(x, y, 1);
                return;
            }
            if (cell == (byte)CellType.MilitaryBlock)
            {
                World.DamageCell(x, y, 1);
                return;
            }
            float hitdmg = 0.2f;
            if (World.isCry(cell))
            {
                hitdmg = 1f;
                Mine(cell, x, y);
            }
            else
            {
                foreach (var c in skillslist.skills.Values)
                {
                    if (c != null && c.UseSkill(SkillEffectType.OnDig, this))
                    {
                        hitdmg = c.type switch
                        {
                            SkillType.Digging => hitdmg * (c.Effect / 100f),
                            _ => 1f
                        };
                    }
                }
            }
            if (World.DamageCell(x, y, hitdmg)) OnDestroy(cell);
            if (World.GetProp(cell).isBoulder)
            {
                var plusy = dir == 2 ? -1 : dir == 0 ? 1 : 0;
                var plusx = dir == 3 ? 1 : dir == 1 ? -1 : 0;
                if (World.GetProp(World.GetCell(x + plusx, y + plusy)).isEmpty)
                {
                    World.MoveCell(x, y, plusx, plusy);
                    foreach (var c in skillslist.skills.Values)
                    {
                        if (c != null && c.UseSkill(SkillEffectType.OnDig, this))
                        {
                            c.AddExp(this);
                        }
                    }
                }
            }
        }
        public override bool Move(int x, int y, int dir = -1)
        {
            if (!World.W.ValidCoord(x, y) || win != null)
            {
                tp(this.x, this.y);
                return false;
            }
            if (dir == -1 || this.x != x || this.y != y)
                this.dir = this.x > x ? 1 : this.x < x ? 3 : this.y > y ? 2 : 0;
            else
                this.dir = dir;
            var cell = World.GetCell(x, y);
            if (!World.GetProp(cell).isEmpty)
            {
                if (dir == -1)
                {
                    tp(this.x, this.y);
                    if (autoDig)
                    {
                        Bz();
                    }
                    return true;
                }
                tp(this.x, this.y);
                return false;
            }
            if (Vector2.Distance(new Vector2(this.x,this.y), new Vector2(x, y)) < 1.2f)
            {
                foreach (var c in skillslist.skills.Values)
                {
                    if (c != null && c.UseSkill(SkillEffectType.OnMove, this))
                    {
                        if (c.type == SkillType.Movement)
                        {
                            c.AddExp(this);
                        }
                    }
                }
                this.x = x;this.y = y;
            }
            else
            {
                tp(this.x, this.y);
                return false;
            }
            SendMyMove();
            SendMap();
            if (World.ContainsPack(x, y, out var pack) && (pack.cid == cid || pack.cid == 0) && !programsData.ProgRunning)
            {
                win = pack.GUIWin(this)!;
                SendWindow();
            }
            return false;
        }
        public void Build(string type)
        {
            int x = (int)GetDirCord().x, y = (int)GetDirCord().y;
            if (!World.W.ValidCoord(x, y) || World.GunRadius(x, y, this) || World.PackPart(x, y))
            {
                return;
            }
            var buildskills = skillslist.skills.Values.Where(c => c.EffectType() == SkillEffectType.OnBld);
            switch (type)
            {
                case "G":
                    foreach (var c in buildskills)
                    {
                        if (c.type == SkillType.BuildGreen && World.GetProp(x, y).isEmpty)
                        {
                            c.AddExp(this);
                            if (crys.RemoveCrys(0, (long)c.Effect))
                            {
                                World.SetCell(x, y, CellType.GreenBlock);
                                World.SetDurability(x, y, c.AdditionalEffect);
                            }
                            return;
                        }
                        else if (c.type == SkillType.BuildYellow && World.GetCell(x, y) == (byte)CellType.GreenBlock)
                        {
                            c.AddExp(this);
                            if (crys.RemoveCrys(4, (long)c.Effect))
                            {
                                World.SetCell(x, y, CellType.YellowBlock);
                                World.SetDurability(x, y, World.GetDurability(x, y) + c.AdditionalEffect);
                            }
                            return;
                        }
                        else if (c.type == SkillType.BuildRed && World.GetCell(x, y) == (byte)CellType.YellowBlock)
                        {
                            c.AddExp(this);
                            if (crys.RemoveCrys(2, (long)c.Effect))
                            {
                                World.SetCell(x, y, CellType.RedBlock);
                                World.SetDurability(x, y, World.GetDurability(x, y) + c.AdditionalEffect);
                            }
                            return;
                        }
                    }
                    break;
                case "V":
                    foreach (var c in buildskills)
                    {
                        if (c.type == SkillType.BuildWar)
                        {
                            c.AddExp(this);
                            if (crys.RemoveCrys(5, (long)c.Effect) && World.GetProp(x, y).isEmpty)
                            {
                                World.SetCell(x, y, CellType.MilitaryBlockFrame);
                                World.W.AsyncAction(50, () =>
                                {
                                    if (World.GetCell(x, y) == (byte)CellType.MilitaryBlockFrame)
                                    {
                                        World.SetCell(x, y, CellType.MilitaryBlock);
                                        World.SetDurability(x, y, c.AdditionalEffect);
                                    }
                                });
                            }
                            return;
                        }
                    }
                    break;
                case "R":
                    foreach (var c in buildskills)
                    {
                        if (c.type == SkillType.BuildRoad)
                        {
                            c.AddExp(this);
                            if (crys.RemoveCrys(0, (long)c.Effect) && World.GetProp(x, y).isEmpty)
                            {
                                World.SetCell(x, y, CellType.Road);
                            }
                            return;
                        }
                    }
                    break;
                case "O":
                    foreach (var c in buildskills)
                    {
                        if (c.type == SkillType.BuildStructure)
                        {
                            c.AddExp(this);
                            if (crys.RemoveCrys(0, (long)c.Effect) && World.GetProp(x, y).isEmpty)
                            {
                                World.SetCell(x, y, CellType.Support);
                            }
                            return;
                        }
                    }
                    break;
            }
        }
        #endregion
        #region creating
        public void CreatePlayer()
        {
            name = "";
            money = 1000;
            creds = 0;
            hash = GenerateHash();
            passwd = "";
            Health = 100;
            MaxHealth = 100;
            inventory = new Inventory();
            settings = new Settings(true);
            crys = new Basket(this);
            skillslist = new PlayerSkills();
            AddBasicSkills();
            x = 0;y = 0;
            dir = 0;
            clan = null;
            skin = 0;
            RandomResp();
        }
        public void RandomResp()
        {
            using var db = new DataBase();
            var re = db.resps.Where(i => i.ownerid == 0);
            var rp = Physics.r.Next(0, re.Count());
            var resp = re.ElementAt(rp);
            var pos = resp.GetRandompoint();
            x = pos.x; y = pos.y;
            SetResp(resp);
            db.SaveChanges();
        }
        public Resp? GetCurrentResp()
        {
            using var db = new DataBase();
            World.ContainsPack(resp.x, resp.y, out var p);
            return p as Resp;
        }
        private void AddBasicSkills()
        {
            //базовые скиллы
            skillslist.InstallSkill(SkillType.MineGeneral.GetCode(), 0, this);
            skillslist.InstallSkill(SkillType.Digging.GetCode(), 1, this);
            skillslist.InstallSkill(SkillType.Movement.GetCode(), 2, this);
            skillslist.InstallSkill(SkillType.Health.GetCode(), 3, this);
        }
        public void Init()
        {
            if (DataBase.activeplayers.FirstOrDefault(p => p.id == id) == default)
            {
                DataBase.activeplayers.Add(this);
            }
            MaxHealth = 100;
            foreach (var c in skillslist.skills.Values)
            {
                if (c != null && c.UseSkill(SkillEffectType.OnHealth, this))
                {
                    if (c.type == SkillType.Health)
                    {
                        MaxHealth += (int)c.Effect;
                    }
                }
            }
            Health = Health <= 0 ? MaxHealth : Health;
            connection.auth = null;
            crys.player = this;
            skillslist.LoadSkills();
            connection?.SendWorldInfo();
            this.SendAutoDigg();
            this.SendGeo();
            this.SendHealth();
            this.SendBotInfo();
            this.SendSpeed();
            this.SendCrys();
            this.SendMoney();
            this.SendLvl();
            this.SendInventory();
            tp(x, y);
            SendMap();
            console.Enqueue(new Line { text = "@@> Добро пожаловать в консоль!" });
            for (var i = 0; i < 4; i++)
            {
                MConsole.AddConsoleLine(this);
            }

            MConsole.AddConsoleLine(this, "Если вы не понимаете, что происходит,");
            MConsole.AddConsoleLine(this, "или вас попросили выполнить команду,");
            MConsole.AddConsoleLine(this, "сосите хуй глотайте сперму");
            for (var i = 0; i < 8; i++)
            {
                MConsole.AddConsoleLine(this);
            }
            settings.SendSettings(this);
            SendClan();
            SendChat();
            SendMap(true);
            connection.starttime = ServerTime.Now;
            SendPing(default);
            connection?.SendU(new ConfigPacket("oldprogramformat+"));
        }

        #endregion
        #region senders

        public void SendChat()
        {
            using var db = new DataBase();
            currentchat ??= db.chats.FirstOrDefault(i => i.tag == "FED");
            connection?.SendU(new CurrentChatPacket(currentchat.tag, currentchat.Name));
            var msg = currentchat.GetMessages();
            if (msg.Length > 0)
            {
                connection?.SendU(new ChatMessagesPacket("FED", currentchat.GetMessages()));
            }
        }
        public void SendPing(PongPacket p)
        {
            if (connection is null)
                return;
            var now = ServerTime.Now;
            var localserver = (int)(now - connection.starttime).TotalMilliseconds;
            Task.Run(() =>
            {
                Thread.Sleep(100);
                connection?.SendU(new PingPacket(52, localserver, $"{localserver - p.CurrentTime - (int)(now - lastping).TotalMilliseconds} "));
                lastping = now;
            });
        }
        public void SendWindow()
        {
            if (win != null)
            {
                connection?.SendU(new GUIPacket(win.ToString()));
                return;
            }
            connection?.SendU(new GuPacket());
        }
        public void OpenClan()
        {
            if (clan != null)
            {
                using var db = new DataBase();
                db.clans.Where(i => i.id == clan.id).Include(p => p.members).Include(p => p.reqs).FirstOrDefault()?.OpenClanWin(this);
            }
        }
        public void tp(int x, int y)
        {
            connection?.SendU(new TPPacket(x, y));
            SendMyMove();
        }
        public void SendClan()
        {
            if (cid == 0)
                connection?.SendU(new ClanHidePacket());
            else
                connection?.SendU(new ClanShowPacket(cid));

        }
        #endregion
        #region renders
        public void ReSendBots()
        {
            List<IHubPacket> packets = new();
            var valid = bool (int x, int y) => x >= 0 && y >= 0 && x < World.ChunksW && y < World.ChunksH;
            for (var xxx = -2; xxx <= 2; xxx++)
            {
                for (var yyy = -2; yyy <= 2; yyy++)
                {
                    var x = ChunkX + xxx;
                    var y = ChunkY + yyy;
                    if (valid(x, y))
                    {
                        var ch = World.W.chunks[x, y];
                        foreach (var id in ch.bots)
                        {
                            var player = DataBase.GetPlayer(id.Key);
                            if (player != null)
                            {
                                packets.Add(new HBBotPacket(player.id, player.x, player.y, player.dir, player.skin, player.cid, player.tail));
                            }
                        }
                    }
                }
            }
            connection?.SendB(new HBPacket(packets.ToArray()));
            lastPlayersend = DateTime.Now;
        }
        public void ReSendPacks()
        {
            var valid = bool (int x, int y) => x >= 0 && y >= 0 && x < World.ChunksW && y < World.ChunksH;
            for (var xxx = -2; xxx <= 2; xxx++)
            {
                for (var yyy = -2; yyy <= 2; yyy++)
                {
                    var x = ChunkX + xxx;
                    var y = ChunkY + yyy;
                    if (valid(x, y))
                    {
                        var ch = World.W.chunks[x, y];
                        foreach (var p in ch.packs.Values)
                        {
                            connection?.SendB(new HBPacket([new HBPacksPacket(p.x / 32 + p.y / 32 * World.ChunksH, [new HBPack((char)p.type, p.x, p.y, (byte)p.cid, (byte)p.off)])]));
                        }
                    }
                }
            }
            lastPlayersend = DateTime.Now;
        }
        public void SendMyMove()
        {
            if (connection == null)
            {
                return;
            }
            var valid = bool (int x, int y) => x >= 0 && y >= 0 && x < World.ChunksW && y < World.ChunksH;
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    var cx = ChunkX + x;
                    var cy = ChunkY + y;
                    if (valid(cx, cy))
                    {
                        var ch = World.W.chunks[cx, cy];
                        ch.active = true;
                        if (ch != null)
                        {

                            cx *= 32; cy *= 32;
                            foreach (var id in ch.bots)
                            {
                                DataBase.GetPlayer(id.Key)?.connection?.SendB(new HBPacket([new HBBotPacket(this.id, this.x, this.y, dir, skin, cid, tail)]));
                            }
                        }
                    }
                }
            }
        }
        public void MoveToChunk(int x, int y)
        {
            if (lastchunk != null && World.W.chunks[lastchunk.Value.Item1, lastchunk.Value.Item2] != null)
            {
                var chtoremove = World.W.chunks[lastchunk.Value.Item1, lastchunk.Value.Item2];
                if (chtoremove.bots.ContainsKey(id))
                {
                    chtoremove.bots.Remove(id, out var p);
                }
            }
            var chtoadd = World.W.chunks[x, y];
            lastchunk = (x, y);
            if (World.W.chunks[lastchunk.Value.Item1, lastchunk.Value.Item2] != null)
            {
                if (!chtoadd.bots.ContainsKey(id))
                {
                    chtoadd.AddBot(this);
                }
            }
        }
        [NotMapped]
        public (int, int)? lastchunk { get; private set; }
        public void SendMap(bool force = false)
        {
            var valid = bool (int x, int y) => x >= 0 && y >= 0 && x < World.ChunksW && y < World.ChunksH;
            if (!valid(ChunkX, ChunkY))
            {
                return;
            }
            if (lastchunk != (ChunkX, ChunkY) || force)
            {
                MoveToChunk(ChunkX, ChunkY);
                List<IHubPacket> packetsmap = new();
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        var cx = ChunkX + x;
                        var cy = ChunkY + y;
                        if (valid(cx, cy))
                        {
                            var ch = World.W.chunks[cx, cy];
                            ch.active = true;
                            List<HBPack> packs = new();
                            if (ch != null)
                            {
                                cx *= 32; cy *= 32;
                                packetsmap.Add(new HBMapPacket(cx, cy, 32, 32, ch.cells));
                                foreach (var p in ch.packs.Values)
                                {
                                    packs.Add(new HBPack((char)p.type, p.x, p.y, (byte)p.cid, (byte)p.off));
                                }
                                connection?.SendB(new HBPacket([new HBPacksPacket(ch.pos.Item1 + ch.pos.Item2 * World.ChunksH, packs.ToArray())]));
                                foreach (var id in ch.bots)
                                {
                                    var player = DataBase.GetPlayer(id.Key);
                                    if (player != null)
                                    {
                                        packetsmap.Add(new HBBotPacket(player.id, player.x, player.y, player.dir, player.skin, player.cid, player.tail));
                                    }
                                }
                            }
                        }
                    }
                }
                connection?.SendB(new HBPacket(packetsmap.ToArray()));
                lastPlayersend = DateTime.Now;
            }
        }
        public void SendDFToBots(int fx, int fxx, int fxy, int bid, int dir, int col = 0)
        {
            var valid = bool (int x, int y) => x >= 0 && y >= 0 && x < World.ChunksW && y < World.ChunksH;
            for (var xxx = -2; xxx <= 2; xxx++)
            {
                for (var yyy = -2; yyy <= 2; yyy++)
                {
                    if (valid(ChunkX + xxx, ChunkY + yyy))
                    {
                        var x = ChunkX + xxx;
                        var y = ChunkY + yyy;
                        var ch = World.W.chunks[x, y];

                        foreach (var player in ch.bots.Select(id => DataBase.GetPlayer(id.Key)))
                        {
                            player?.connection?.SendB(new HBPacket([new HBDirectedFXPacket(id, fxx, fxy, fx, dir, col)]));
                        }
                    }
                }
            }
        }
        public void SendFXoBots(int fx, int fxx, int fxy)
        {
            var valid = bool (int x, int y) => x >= 0 && y >= 0 && x < World.ChunksW && y < World.ChunksH;
            for (var xxx = -2; xxx <= 2; xxx++)
            {
                for (var yyy = -2; yyy <= 2; yyy++)
                {
                    if (valid(ChunkX + xxx, ChunkY + yyy))
                    {
                        var x = ChunkX + xxx;
                        var y = ChunkY + yyy;
                        var ch = World.W.chunks[x, y];

                        foreach (var player in ch.bots.Select(id => DataBase.GetPlayer(id.Key)))
                        {
                            player?.connection?.SendB(new HBPacket([new HBFXPacket(fxx, fxy, fx)]));
                        }
                    }
                }
            }
        }
        public void SendLocalMsg(string msg)
        {
            var valid = bool (int x, int y) => x >= 0 && y >= 0 && x < World.ChunksW && y < World.ChunksH;
            for (var xxx = -2; xxx <= 2; xxx++)
            {
                for (var yyy = -2; yyy <= 2; yyy++)
                {
                    var x = ChunkX + xxx;
                    var y = ChunkY + yyy;
                    if (valid(x, y))
                    {
                        var ch = World.W.chunks[x, y];
                        foreach (var id in ch.bots)
                        {
                            var player = DataBase.GetPlayer(id.Key);
                            if (player != null)
                            {
                                player?.connection?.SendB(new HBPacket([new HBChatPacket(this.id, x, y, msg)]));
                            }
                        }
                    }
                }
            }
        }
        #endregion
        public string GenerateHash()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public void CallWinAction(string text)
        {
            if (win == null)
            {
                connection?.SendU(new GuPacket());
                return;
            }
            win.ProcessButton(text);
        }
        #region health
        public override void Heal(int num = -1)
        {
            var heal = skillslist.skills.Values.FirstOrDefault(i => i.type == SkillType.Repair);
            if (Health == MaxHealth || heal == default)
                return;
            num = (int)heal.Effect;
            if (num == -1)
                return;
            if (crys.RemoveCrys(2, 1))
            {
                heal.AddExp(this);
                Health += num;
                if (Health > MaxHealth)
                    Health = MaxHealth;
                SendDFToBots(5, 0, 0, id, 0);
                this.SendHealth();
            }
        }

        public override void Hurt(int num, DamageType t = DamageType.Pure)
        {
            foreach (var c in skillslist.skills.Values)
            {
                if (c != null && c.UseSkill(SkillEffectType.OnHealth, this))
                {
                    if (c.type == SkillType.Health)
                    {
                        c.AddExp(this);
                    }
                }
                if (c != null && c.UseSkill(SkillEffectType.OnHurt, this) && t == DamageType.Gun)
                {
                    if (c.type == SkillType.Induction)
                    {
                        c.AddExp(this);
                    }
                    if (c.type == SkillType.AntiGun)
                    {
                        c.AddExp(this);
                        var eff = (int)(num * (c.Effect / 100));
                        if (num - eff >= 0)
                        {
                            num -= eff;
                        }
                        else
                        {
                            num = 0;
                        }
                    }
                }
            }
            if (Health - num > 0)
            {
                Health -= num;
                SendDFToBots(6, 0, 0, id, 0);
            }
            else
            {
                Death();
            }
            this.SendHealth();
        }
        private async Task<(int x, int y)> FindEmptyForBox(int x, int y)
        {
            var dirs = new (int, int)[] { (0, 1), (1, 0), (-1, 0), (0, -1) };
            var q = new Queue<(int, int)>();
            var valid = bool (int x, int y) => World.GetProp(x, y).isEmpty && !World.PackPart(x, y) && World.W.ValidCoord(x, y);
            var a = World.PackPart(x, y);
            if (!valid(x, y))
            {
                q.Enqueue((x, y));
            }
            while (q.Count > 0)
            {
                var b = q.Dequeue();
                foreach (var dir in dirs)
                {
                    if (!valid(b.Item1 + dir.Item1, b.Item2 + dir.Item2))
                    {
                        q.Enqueue((b.Item1 + dir.Item1, b.Item2 + dir.Item2));
                        continue;
                    }
                    return (b.Item1 + dir.Item1, b.Item2 + dir.Item2);
                }
            }
            return (x, y);
        }

        public override void Death()
        {
            if (crys.AllCry > 0)
            {
                var c = FindEmptyForBox(x, y);
                c.ContinueWith((f) =>
                {
                    Box.BuildBox(f.Result.x, f.Result.y, crys.cry, this, true);
                    crys.ClearCrys();
                });
            }
            win = null;
            SendWindow();
            SendFXoBots(2, x, y);
            Health = MaxHealth;
            var r = GetCurrentResp()!;
            r.OnRespawn(this);
            r = GetCurrentResp()!;
            var newpos = r.GetRandompoint();
            x = newpos.Item1; y = newpos.Item2;
            tp(x, y);
            ReSendBots();
            SendMap();
            this.SendHealth();
            if (programsData.ProgRunning)
            {
                if (programsData.RespawnOnProg)
                {
                    programsData.OnDeath();
                    return;
                }
                RunProgramm();
                connection?.SendU(new ProgrammatorPacket(false));
            }
        }
        #endregion
    }
}
