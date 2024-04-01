using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using MinesServer.GameShit.GUI;
using MinesServer.GameShit.GUI.Horb;
using MinesServer.GameShit.GUI.Horb.List;
using MinesServer.GameShit.Programmator.SevenZip.LZMA;
using MinesServer.Network.Programmator;
using MinesServer.Server;
using System.Text;

namespace MinesServer.GameShit.Programmator
{
    public static class StaticGUI
    {

        public static void NewProg(Player p, string name)
        {
            using var db = new DataBase();
            db.players.Attach(p);
            var prog = new Program(p, name, "");
            p.programs.Add(prog);
            db.SaveChanges();
            p.connection?.SendU(new OpenProgrammatorPacket(prog.id, name, prog.data));
            p.connection?.SendU(new ProgrammatorPacket());
            p.win = null;
        }
        public static ListEntry[] LoadProgs(Player p)
        {
            using var db = new DataBase();
            var progs = db.progs.Where(i => i.owner == p).ToList();
            if (progs.Count == 0)
                return [];
            return progs.Select(i => new ListEntry(i.name, new MButton("open", $"openprog:{i.id}", (a) => OpenProg(p, i)))).ToArray();
        }
        public static void OpenProg(Player p, Program prog)
        {
            prog.data = prog.data[(prog.data.IndexOf('�') + 1)..];
            byte[] array = SevenZipHelper.Decompress(Convert.FromBase64String(prog.data));
            int num = BitConverter.ToInt32(array, 0);
            string[] array2 = Encoding.UTF8.GetString(array, num + 4, array.Length - num - 4).Split(':');
            for (int i = 0; i < num; i++)
            {
                Console.WriteLine(GetActionType(Convert.ToInt16(array[i + 4])));
                if (array2[i].Contains("@"))
                {
                    string[] array3 = array2[i].Split('@');
                    Console.WriteLine(array3[0]);
                    Console.WriteLine(array3[1]);
                }
                else
                {
                    Console.WriteLine(array2[i]);
                    Console.WriteLine(0);
                }
            }
            p.win = null;
            p.connection?.SendU(new OpenProgrammatorPacket(prog.id, prog.name,prog.data));
        }
        private static ActionType GetActionType(int id)
        {
            return id switch
            {
                0 => ActionType.None,
                1 => ActionType.NextRow,
                2 => ActionType.Start,
                3 => ActionType.Stop,
                4 => ActionType.MoveUp,
                5 => ActionType.MoveLeft,
                6 => ActionType.MoveDown,
                7 => ActionType.MoveLeft,
                8 => ActionType.Dig,
                9 => ActionType.RotateUp,
                10 => ActionType.RotateLeft,
                11 => ActionType.RotateDown,
                12 => ActionType.RotateRight,
                14 => ActionType.MoveForward,
                15 => ActionType.RotateLeftRelative,
                16 => ActionType.RotateRightRelative,
                17 => ActionType.BuildBlock,
                18 => ActionType.Geology,
                19 => ActionType.BuildRoad,
                20 => ActionType.Heal,
                21 => ActionType.BuildPillar,
                22 => ActionType.RotateRandom,
                23 => ActionType.Beep,
                24 => ActionType.GoTo,
                25 => ActionType.RunSub,
                26 => ActionType.RunFunction,
                27 => ActionType.Return,
                28 => ActionType.ReturnFunction,
                29 => ActionType.CheckUpLeft,
                30 => ActionType.CheckDownRight,
                31 => ActionType.CheckUp,
                32 => ActionType.CheckUpRight,
                33 => ActionType.CheckLeft,
                34 => ActionType.None,
                35 => ActionType.CheckRight,
                36 => ActionType.CheckDownLeft,
                37 => ActionType.CheckDown,
                38 => ActionType.Or,
                39 => ActionType.And,
                40 => ActionType.CreateFunction,
                41 => ActionType.None,
                42 => ActionType.None,
                43 => ActionType.IsNotEmpty,
                44 => ActionType.IsEmpty,
                45 => ActionType.IsFalling,
                46 => ActionType.IsCrystal,
                47 => ActionType.IsLivingCrystal,
                48 => ActionType.IsBoulder,
                49 => ActionType.IsSand,
                50 => ActionType.IsBreakableRock,
                51 => ActionType.IsUnbreakable,
                52 => ActionType.IsRedRock,
                53 => ActionType.IsBlackRock,
                54 => ActionType.IsAcid,
                55 => ActionType.None,
                56 => ActionType.None,
                57 => ActionType.IsQuadBlock,
                58 => ActionType.IsRoad,
                59 => ActionType.IsRedBlock,
                60 => ActionType.IsYellowBlock,
                61 => ActionType.None, //хуй знает чет с хп
                62 => ActionType.None, //хуй знает чет с хп
                74 => ActionType.IsBox,
                76 => ActionType.IsPillar,
                77 => ActionType.IsGreenBlock,
                119 => ActionType.WritableStateMore,
                120 => ActionType.WritableStateLower,
                123 => ActionType.WritableState,
                131 => ActionType.ShiftUp,
                132 => ActionType.ShiftLeft,
                133 => ActionType.ShiftDown,
                134 => ActionType.ShiftRight,
                135 => ActionType.CheckForward,
                136 => ActionType.ShiftForward,
                137 => ActionType.RunState,
                138 => ActionType.ReturnState,
                139 => ActionType.RunIfFalse,
                140 => ActionType.RunIfTrue,
                141 => ActionType.MacrosDig,
                142 => ActionType.MacrosBuild,
                143 => ActionType.MacrosHeal,
                144 => ActionType.Flip,
                145 => ActionType.MacrosMine,
                146 => ActionType.CheckGun,
                147 => ActionType.FillGun,
                148 => ActionType.IsHpLower100,
                149 => ActionType.IsHpLower50,
                156 => ActionType.CheckForwardLeft,
                157 => ActionType.CheckForwardRight,
                158 => ActionType.EnableAutoDig,
                159 => ActionType.DisableAutoDig,
                160 => ActionType.EnableAgression,
                161 => ActionType.DisableAgression,
                166 => ActionType.RunOnRespawn,
                _ => ActionType.None
            } ;
        }
        public static void Rename(Player p,int id)
        {
            var rename =  (ActionArgs args) =>
            {
                if (Default.def.IsMatch(args.Input))
                {
                    using var db = new DataBase();
                    var prog = db.progs.FirstOrDefault(p => p.id == id);
                    prog.name = args.Input;
                    db.SaveChanges();
                    p.connection?.SendU(new UpdateProgrammatorPacket(prog.id, prog.name, prog.data));
                }
                p.win = null;
            };
            p.win = new Window()
            {
                Title = "RenameProg",
                Tabs = [new Tab()
                {
                    Action = "pren1",
                    InitialPage = new Page(){
                        Text = "Rename\n",
                Input = new InputConfig()
                {
                    Placeholder = "Название программы..."
                },
                Style = new Style()
                {
                    FixScrollTag = "prg"
                },
                Buttons = [new MButton("Ok", $"rename{ActionMacros.Input}", rename)]
                    },
                    Label = "pren2"
                }]
            };
            p.SendWindow();
        }
        public static void StartedProg(Player p, (int id, string source) data)
        {
            using var db = new DataBase();
            var programm = db.progs.FirstOrDefault(i => i.id == data.id);
            if (programm != default)
            {
                programm.data = data.source;
                db.SaveChanges();
                p.RunProgramm(programm);
                p.connection?.SendU(new UpdateProgrammatorPacket(programm.id,programm.name,programm.data));
            }
        }
        public static void OpenCreateProg(Player p)
        {
            p.win.CurrentTab.Open(new Page()
            {
                Text = "Введите название вашей программы\n",
                Input = new InputConfig()
                {
                    Placeholder = "Название программы..."
                },
                Style = new Style()
                {
                    FixScrollTag = "prg"
                },
                Buttons = [new MButton("Создать", $"create2{ActionMacros.Input}", (args) => NewProg(p, args.Input))]
            });
            p.SendWindow();
        }
        public static void DeleteProg(Player p,int id)
        {
            using var db = new DataBase();
            db.progs.Remove(db.progs.FirstOrDefault(i => i.id == id)!);
            db.SaveChanges();
        }
        public static void OpenGui(Player p)
        {
            var l = LoadProgs(p);
            if (l.Length > 0)
            {
                p.win = new Window()
                {
                    Tabs = [new Tab()
                {
                    Action = "prog",
                    Label = "",
                    Title = "ПРОГРАММАТОР",
                    InitialPage = new Page()
                    {
                        List = l,
                        Buttons = [new MButton("Создать","createrog",(e) => OpenCreateProg(p))]
                    }

                }]
                };
                p.SendWindow();
                return;
            }
            p.win = new Window()
            {
                Tabs = [new Tab()
                {
                    Action = "prog",
                    Label = "",
                    Title = "ПРОГРАММАТОР",
                    InitialPage = new Page()
                    {
                        Buttons = [new MButton("СОЗДАТЬ ПРОГРАММУ", "createprog", (args) => OpenCreateProg(p))]
                    }

                }]
            };
            p.SendWindow();
        }
    }
}
