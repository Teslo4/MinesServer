﻿using Microsoft.EntityFrameworkCore;
using MinesServer.GameShit.Entities.PlayerStaff;
using MinesServer.GameShit.GUI;
using MinesServer.GameShit.GUI.Horb;
using MinesServer.GameShit.GUI.Horb.List;
using MinesServer.GameShit.GUI.Horb.List.Rich;
using MinesServer.GameShit.GUI.UP;
using MinesServer.GameShit.WorldSystem;
using MinesServer.Server;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MinesServer.GameShit.ClanSystem
{
    public class Clan
    {
        #region fields
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public int id { get; set; }
        public virtual List<Request> reqs { get; set; } = new List<Request>();
        public virtual List<Player> members { get; set; } = new List<Player>();
        public virtual List<Rank> ranks { get; set; } = new List<Rank>();
        public int ownerid { get; set; }
        public string name { get; set; }
        public string abr { get; set; }
        #endregion
        public Clan()
        {
        }
        #region clanmain
        public static InventoryItem[] ClanIcons()
        {
            using var db = new DataBase(); List<InventoryItem> l = new();
            for (int i = 0; i < 8; i++)
            {
                var r = Physics.r.Next(1, 219);
                if (db.clans.FirstOrDefault(i => i.id == r) == null && l.FirstOrDefault(i => i.Id == r) == default)
                {
                    l.Add(InventoryItem.Clan((byte)r, 0));
                }
            }
            return l.ToArray();
        }
        public void OpenClanWin(Player p)
        {
            GUI.MButton[] buttons = [new MButton("leave", "leave", (args) => LeaveClan(p))];
            if (p.id == ownerid && members.Count > 1)
            {
                buttons = [];
            }
            Tab[] tabs = [new Tab()
            {
                Action = "view",
                Label = "Обзор",
                InitialPage = new Page()
                {
                    Card = new Card(CardImageType.Clan, id.ToString(), $"<color=white>{name}[{abr}]</color>\nУчастники: <color=white>{members.Count}</color>"),
                    Buttons = []
                }
            },
                new Tab()
                {
                    Action = "list",
                    Label = "Список",
                    InitialPage = new Page()
                    {
                        ClanList = BuildClanlist(p),
                        Buttons = buttons
                    }
                }];

            if (reqs.Count > 0)
            {
                tabs = tabs.Append(new Tab()
                {
                    Action = "reqs",
                    Label = "Заявки",
                    InitialPage = new Page()
                    {
                        Buttons = [],
                        List = Reqs(p)
                    }

                }).ToArray();
            }
            p.win = new Window()
            {
                Title = name,
                ShowTabs = true,
                Tabs = tabs
            };
            p.SendWindow();
        }
        private ClanListEntry[] BuildClanlist(Player p)
        {
            List<ClanListEntry> list = new();
            if (members != null)
            {
                foreach (var player in members)
                {
                    list.Add(new ClanListEntry(new MButton($"<color={player?.clanrank.colorhex}>{player?.name}</color> - {player?.clanrank.name}", $"listrow:{player?.id}", (args) => OpenPlayerPrew(p, player)), 0, "онлайн?"));
                }
            }
            return list.ToArray();
        }
        public void OpenPlayerPrew(Player p, Player target, bool changerank = false)
        {
            GUI.MButton[] buttons = [new MButton("Прокачка", "skills", (args) => OpenPlayerSkills(p, target))];
            RichListEntry[] list = [];
            if (changerank)
            {
                list = list.Append(RichListEntry.DropDown("изменение звания", "changerankd", ranks.Where(i => i.priority < p.clanrank.priority).Select(i => i.name).ToArray(), ranks.IndexOf(target.clanrank))).ToArray();
                buttons = [new MButton("kick", "kick", (args) => KickPlayer(p, target))];
            }
            if (p.clanrank.priority > target.clanrank.priority)
            {
                if (changerank)
                {
                    buttons = buttons.Concat([new MButton("saverank", "saverank", (args) => OpenPlayerPrew(p, target, true))]).ToArray();
                }
                else
                {
                    buttons = buttons.Concat([new MButton("changerank", "changerank", (args) => OpenPlayerPrew(p, target, true))]).ToArray();
                }
            }
            p.win.CurrentTab.Open(new Page()
            {
                RichList = new RichListConfig(list, true),
                Text = $"@@ПРОФИЛЬ СОКЛАНА\n\nИмя: <color={target.clanrank?.colorhex}>{target.name}</color>\nЗвание: {target.clanrank.name}\nID:  <color=white>{target.id}</color>",
                Buttons = buttons
            });
            p.SendWindow();
        }
        public void KickPlayer(Player p, Player target)
        {
            using var db = new DataBase();
            target = DataBase.GetPlayer(target.id);
            db.players.Attach(target);
            target.clanrank = null;
            target.clan = null;
            target.SendClan();
            target.win = null;
            db.SaveChanges();
            OpenClanWin(p);
        }
        public void OpenPlayerSkills(Player p, Player target)
        {
            target = DataBase.GetPlayer(target.id);
            p.win.CurrentTab.Open(new UpPage()
            {
                Skills = target.skillslist.GetSkills(),
                SlotAmount = target.skillslist.slots,
                SkillsToInstall = [],
                Text = $"Просмотр скиллов игрока\n\n<color=white>{target.name}</color>\nID <color=white>{target.id}</color>\nОбщий уровень: <color=white>{target.skillslist.lvlsummary()}</color>"
            });
            p.SendWindow();
        }
        public void LeaveClan(Player p)
        {
            using var db = new DataBase();
            p = DataBase.GetPlayer(p.id);
            p.clan = null;
            p.clanrank = null;
            if (p.id == ownerid)
            {
                db.clans.Remove(this);
            }
            p.SendClan();
            p.SendMyMove();
            p.SendWindow();
            p.win = null;
            db.SaveChanges();
        }
        #endregion
        #region requests
        public void AddReq(int id)
        {
            using var db = new DataBase();
            var p = DataBase.GetPlayer(id);
            db.clans.Attach(this);
            if (reqs.FirstOrDefault(i => i.player?.id == id) == null)
            {
                var req = new Request() { player = p, reqtime = DateTime.Now };
                reqs.Add(req);
            }
            db.SaveChanges();
            p.win?.CurrentTab.Open(new Page()
            {
                Text = "Заявка подана",
                Title = "КЛАНЫ",
                Card = new Card(CardImageType.Clan, this.id.ToString(), $"<color=white>{name}</color>\nУчастники: <color=white>{members.Count}</color>"),
                Buttons = []
            });
            p.SendWindow();
        }
        public ListEntry[] Reqs(Player p)
        {
            List<ListEntry> rq = new();
            var c = 1;
            foreach (var request in reqs)
            {
                rq.Add(new ListEntry($"{c}.<color=white>{request.player?.name}</color>", new MButton("...", $"openreq:{request.player?.id}", (args) => OpenReq(p, request))));
                c++;
            }
            return rq.ToArray();
        }
        public void OpenReq(Player p, Request target)
        {
            p.win = new Window()
            {
                Title = "Заявка в клан",
                Tabs = [new Tab()
                {
                    Action = "req",
                    Label = "req",
                    InitialPage = new Page()
                    {
                        Text = $"@@Заявка на прием в клан:\n\n\nИмя: <color=white>{target.player?.name}</color>\nID <color=white>{target.player?.id}</color>\nИстекает через:" +
                        $" {string.Format("{0:hh}ч.{0:mm} мин.", (TimeSpan.FromDays(1) - (DateTime.Now - target.reqtime)))}",
                        Buttons = [new MButton("Принять", "accept", (args) => { AddMember(target); OpenClanWin(p); }), new MButton("Откланить", "decline", (args) => { DeclineReq(target); OpenClanWin(p); }), new MButton("Прокачка", "openskills", (args) => OpenPlayerSkills(p, target?.player))]
                    }
                }]
            };
            p.SendWindow();
        }
        public void DeclineReq(Request target)
        {
            using var db = new DataBase();
            db.reqs.Remove(target);
            db.SaveChanges();
        }
        public void AddMember(Request q)
        {
            using var db = new DataBase();
            q.player = DataBase.GetPlayer(q.player.id);
            db.clans.Attach(this);
            members.Add(q.player);
            q.player.clanrank = ranks.OrderBy(r => r.priority).First();
            foreach (var req in db.reqs.Where(r => r.player == q.player))
            {
                db.reqs.Remove(req);
            }
            q.player.SendClan();
            db.SaveChanges();
        }
        #endregion
        #region creating
        public static void CreateClan(Player p, int icon, string name, string abr)
        {
            using var db = new DataBase();
            db.Attach(p);
            if (db.clans.FirstOrDefault(i => i.id == icon || i.name == name) == null)
            {
                var c = new Clan() { ownerid = p.id, id = icon, abr = abr, name = name };
                c.ranks = new List<Rank>()
            {
                new Rank() { name = "хуесос",priority = 0,colorhex = "#00FF00",owner = c },
                new Rank() { name = "уже смешарик",priority = 20,colorhex = "#ff0000",owner = c },
                new Rank() { name = "Создатель",priority = 100,colorhex = "#006400",owner = c }
                };
                db.Add(c);
                c.members.Add(p);
                p.clan = c;
                p.clanrank = c.ranks.First(i => i.priority == 100);
                p.SendClan();
            }
            db.SaveChanges();
            p.win?.CurrentTab.Open(new Page()
            {
                Title = "КЛАН СОЗДАН",
                Buttons = []
            });
            p.SendWindow();
        }
        public static void ChooseIcon(Player p)
        {
            var goingtoend = (Player p, int icon, string name, string abr) =>
            {
                p.win?.CurrentTab.Open(new Page()
                {
                    Text = "@@\nВсе готово для создания клана.Остался последний этап.\n\n <color=#ff8888ff>Условия:</color>\n1. При создании спишется залог 1000 кредитов.\n2. При удалении клана 90% залога возвращается.\n3. При неактивности игроков в течение 2 месяцев клан удаляется.\n4. Мультоводство в игре запрещено. Использование нескольких\nаккаунтов одним человеком может повлечь штраф и санкции вплоть\nдо бана аккаунтов и удаления клана.\n",
                    Title = "ЗАВЕРШЕНИЕ СОЗДАНИЯ КЛАНА",
                    Card = new Card(CardImageType.Clan, icon.ToString(), $"<color=white>{name}[{abr}]</color>\n"),
                    Buttons = [new MButton("<color=#ff8888ff>ПРИНИМАЮ УСЛОВИЯ</color>", $"complete", (args) => CreateClan(p, icon, name, abr))]
                });
                p.SendWindow();
            };
            var abrchoose = (Player p, int icon, string name) =>
            {
                p.win?.CurrentTab.Open(new Page()
                {
                    Text = "@@\nВыберите краткое имя клана, заглавными латинскими буквами.\n1-3 буквы. Оно используется в списках, командах консоли и пр.\n\nНапример, Хр@нители - HRA, Герои Меча - GRM\nВыберите сокращение, по которому легко узнать ваш клан.\n",
                    Title = "СОЗДАНИЕ АББРЕВИАТУРЫ",
                    Card = new Card(CardImageType.Clan, icon.ToString(), $"<color=white>{name}</color>\n"),
                    Input = new InputConfig()
                    {
                        IsConsole = false,
                        Placeholder = "XXX",
                        MaxLength = 3
                    },
                    Buttons = [new MButton("Далее", $"next:{ActionMacros.Input}", (args) => goingtoend(p, icon, name, args.Input))]
                });
                p.SendWindow();
            };
            var namechoose = (Player p, int iconid) =>
            {
                p.win?.CurrentTab.Open(new Page()
                {
                    Title = "ВЫБОР НАЗВАНИЯ КЛАНА",
                    Text = "@@\nВыберите название клана.\nВ игре есть модерация, оскорбительные кланы могут быть удалены!\n\nВнимание! Название клана нельзя будет изменить после создания.\n",
                    Input = new InputConfig()
                    {
                        IsConsole = false,
                        Placeholder = "clanname"
                    },
                    Buttons = [new MButton("Продолжить", $"namechoose:{ActionMacros.Input}", (args) => abrchoose(p, iconid, args.Input))]
                });
                p.SendWindow();
            };
            p.win?.CurrentTab.Open(new Page()
            {
                Title = "ВЫБОР ЗНАЧКА КЛАНА",
                Text = "@@Выберите значок клана. Всего значков больше сотни. Для удобства мы\nпоказываем их небольшими порциями. Нажмите ДРУГИЕ, чтобы посмотреть еще.\nДля выбора значка - кликните на него.\n\nВнимание! Значок клана нельзя будет изменить после создания.\n",
                Buttons = [new MButton("Другие", "nexticons", (args) => ChooseIcon(p))],
                Inventory = ClanIcons(),
                OnInventory = (i) => namechoose(p, i - 200)
            });
            p.SendWindow();
        }
        public static void OpenCreateWindow(Player p)
        {
            p.win = new Window()
            {
                Tabs = [new Tab()
                {
                    Label = "",
                    Action = "clancreate:1",
                    InitialPage = new Page()
                    {
                        Text = "@@\nУра! Вы собираетесь создать новый клан. После создания клана вы сможете\nвыполнять клановые квесты, создавать свои фермы, вести войны с другими\nкланами, защищать и отбивать территории, и многое другое.\n\nСоздание клана - ответственное действие, значок и название клана нельзя\nбудет изменить позже. Поэтому внимательно подумайте над тем, как будет\nзвучать и выглядеть ваш клан в игре.\n\nСоздание клана требует залога в 1000 кредитов.\n",
                        Buttons = [new MButton("ВЫБРАТЬ ЗНАЧОК КЛАНА", "chooseicon", (args) => ChooseIcon(p))],

                    }
                }]
            };
            p.SendWindow();
        }
        #endregion
        #region clans
        public static void OpenClanList(Player p)
        {
            List<ClanListEntry> clans = new();
            using var db = new DataBase();
            foreach (var clan in db.clans.Include(i => i.members).Include(i => i.reqs))
            {
                clans.Add(new ClanListEntry(new MButton($"<color=white>{clan.name}</color> [{clan.abr}]", $"clan{clan.id}", (args) => clan.OpenPreview(p)), (byte)clan.id, $"прием аткрыт"));
            }
            p.win = new Window()
            {
                Tabs = [new Tab()
                {
                    Action = "clanlist",
                    Label = "СПИСОК КЛАНОВ",
                    InitialPage = new Page()
                    {
                        Title = "КЛАНЫ",
                        ClanList = clans.ToArray(),
                        Buttons = []
                    }

                }
            ]
            };
            p.SendWindow();
        }
        public void OpenPreview(Player p)
        {
            var text = "";
            MButton[] buttons = [new MButton("Подать заявку", "reqin", (args) => AddReq(p.id))];
            if (p.clan != null)
            {
                buttons = [];
            }
            using var db = new DataBase();
            if (reqs.FirstOrDefault(i => i.player.id == p.id) != null)
            {
                text += "\n Заявка уже подана";
                buttons = [];
            }
            p.win.CurrentTab.Open(new Page()
            {
                Text = text,
                Title = "КЛАНЫ",
                Card = new Card(CardImageType.Clan, id.ToString(), $"<color=white>{name}</color>\nУчастники: <color=white>{members.Count}</color>"),
                Buttons = buttons
            });
            p.SendWindow();
        }
        #endregion
    }
}
