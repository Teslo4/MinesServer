using MinesServer.GameShit.Entities;
using MinesServer.GameShit.Entities.PlayerStaff;
using MoreLinq;
using System.ComponentModel.Design;

namespace MinesServer.GameShit.Programmator
{
    public class ProgrammatorData
    {
        public ProgrammatorData(BaseEntity e)
        {
            ProgRunning = false;
            entity = e;
        }
        BaseEntity entity;
        public int checkX;
        public int checkY;
        public int shiftX;
        public int shiftY;
        public (string,int) startpoint;
        public bool flipstate = false;
        private void Drop()
        {
            startpoint = ("", 0);
            GotoDeath = null;
            cFunction = "";
            checkX = 0;
            checkY = 0;
            shiftX = 0;
            shiftY = 0;
            flipstate = false;
            foreach (var function in currentprog)
                function.Value.Reset();
        }
        public bool ProgRunning { 
            get; 
            set; 
        }
        public Dictionary<string, PFunction> currentprog { get; set; }
        public DateTime delay;
        private string cFunction;
        public Program? selected { get; set; }
        private PFunction current
        {
            get => currentprog[cFunction];
        }
        public void Run(Program p)
        {
            selected = p;
            currentprog = p.programm;
            //func logger
            foreach (var i in currentprog)
            {
                Console.WriteLine($"{i.Key} - {string.Join(' ', i.Value.actions.Select(i => i.type))}");
            }
            foreach (var i in currentprog.Values)
                i.Close();
            delay = DateTime.Now;
            Drop();
            ProgRunning = true;
        }
        public bool RespawnOnProg
        {
            get => entity is Player && (entity as Player).resp.cost == 0 && GotoDeath != null;
        }
        public void OnDeath()
        {
            current.Reset();
            cFunction = GotoDeath;
        }
        private string? GotoDeath;
        public void Run()
        {
            if (ProgRunning || selected == null)
            {
                ProgRunning = false;
                return;
            }
            Run(selected);
        }
        private void Next()
        {
            var i = currentprog.Keys.ToList().IndexOf(cFunction);
            if (currentprog.Count > i + 1)
                cFunction = currentprog.ElementAt(i + 1).Key;
            else
                cFunction = currentprog.First().Key;
        }
        public void IncreaseDelay(int ms) => delay = DateTime.Now + TimeSpan.FromMilliseconds(ms);
        private bool? temp = null;
        public void Step()
        {
            if (current == null || DateTime.Now < delay)
            {
                return;
            }
            PAction action;
            if (current.actions.Count <= 0 || current.actions.Count - 1 < current.current)
            {
                current.Reset();
                Next();
                return;
            }
            action = current.Next;
            object result = action.Execute(entity, ref temp)!;
            switch (result)
            {
                case string label:
                    switch (action.type)
                    {
                        case ActionType.GoTo:
                            if (currentprog.TryGetValue(label, out var _))
                            {
                                current.Reset();
                                currentprog[label].calledfrom = cFunction;
                                cFunction = label;
                            }
                            break;
                        case ActionType.RunSub:
                            if (currentprog.TryGetValue(label, out var _))
                            {
                                currentprog[label].calledfrom = cFunction;
                                cFunction = label;
                            }
                            break;
                        case ActionType.RunFunction:
                            if (currentprog.TryGetValue(label, out var _))
                            {
                                if (shiftX != 0 || shiftY != 0 || checkX != 0 || checkY != 0)
                                    currentprog[label].startoffset = (shiftX + checkX, shiftY + checkY);
                                currentprog[label].calledfrom = cFunction;
                                cFunction = label;
                            }
                            break;
                        case ActionType.RunState:
                            if (currentprog.TryGetValue(label, out var _))
                            {
                                if (shiftX != 0 || shiftY != 0 || checkX != 0 || checkY != 0)
                                    currentprog[label].startoffset = (shiftX + checkX, shiftY + checkY);
                                currentprog[label].state = current.state;
                                currentprog[label].laststateaction = current.laststateaction;
                                currentprog[label].calledfrom = cFunction;
                                cFunction = label;
                            }
                            break;
                        case ActionType.RunIfTrue or ActionType.RunIfFalse:
                            if (currentprog.TryGetValue(label, out var _))
                            {
                                if (label == "")
                                {
                                    break;
                                }
                                current.Reset();
                                cFunction = label;
                            }
                            break;
                        case ActionType.RunOnRespawn:
                            if (currentprog.TryGetValue(label, out var _))
                            {
                                GotoDeath = label;
                            }
                            break;
                    }
                    break;
                case bool state:
                    switch (action.type)
                    {
                        case ActionType.ReturnFunction:
                            current.Reset();
                            current.startoffset = (0, 0);
                            if (current.calledfrom is not null)
                            {
                                cFunction = current.calledfrom;
                            }
                            current.state = state;
                            current.startoffset = (0, 0);
                            break;
                    }
                    break;
                case null:
                    switch (action.type)
                    {
                        case ActionType.CheckDown or ActionType.CheckUp or ActionType.CheckRight or ActionType.CheckLeft
                        or ActionType.CheckDownLeft or ActionType.CheckDownRight or ActionType.CheckUpLeft or ActionType.CheckUpRight
                        or ActionType.ShiftUp or ActionType.ShiftLeft or ActionType.ShiftDown or ActionType.ShiftRight or ActionType.ShiftForward:
                            if (current.startoffset != default)
                            {
                                current.startoffset = (0, 0);
                            }
                                break;
                        case ActionType.Return:
                            current.Reset();
                            if (current.calledfrom is not null)
                            {
                                cFunction = current.calledfrom;
                            }
                            break;
                        case ActionType.ReturnState:
                            current.Reset();
                            if (current.calledfrom is not null)
                            {
                                if (shiftX != 0 || shiftY != 0 || checkX != 0 || checkY != 0)
                                    currentprog[current.calledfrom].startoffset = (shiftX + checkX, shiftY + checkY);
                                currentprog[current.calledfrom].state = current.state;
                                currentprog[current.calledfrom].laststateaction = current.laststateaction;
                                cFunction = current.calledfrom;
                            }
                            break;
                        case ActionType.Stop:
                            Run();
                            break;
                        case ActionType.Start:
                            startpoint = (cFunction, current.current);
                            break;
                        case ActionType.Flip:
                            flipstate = !flipstate;
                            break;
                    }
                    break;
            }
            IncreaseDelay(action.delay);
        }
    }
}
