using MinesServer.Enums;
using MinesServer.GameShit.GUI.UP;
using MinesServer.GameShit.Skills;
using MinesServer.Server;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata.Ecma335;

namespace MinesServer.GameShit
{
    public class PlayerSkills
    {
        [Key]
        public int id { get; set; }
        public string ser { get; set; } = "";
        public void LoadSkills()
        {
            if (skills.Count < 1)
            {
                skills = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, Skill?>>(ser);
            }
        }
        [NotMapped]
        public int selectedslot = -1;
        public void DeleteSkill(Player p)
        {
            if (!skills.ContainsKey(selectedslot))
            {
                return;
            }
            skills[selectedslot] = null;
            p.SendLvl();
            Save();
        }
        public void InstallSkill(string type, int slot, Player p)
        {
            if ((skills.ContainsKey(slot) && skills[slot] != null) || slot > slots || slot < 0 || !skillz.First(i => i.type.GetCode() == type).MeetReqs(p))
            {
                return;
            }
            var s = new Skill();
            skills[slot] = skillz.First(i => i.type.GetCode() == type).Clone();
            p.SendLvl();
            Save();
        }
        public void Save()
        {
            using var db = new DataBase();
            db.skills.Attach(this);
            ser = Newtonsoft.Json.JsonConvert.SerializeObject(skills, Newtonsoft.Json.Formatting.None);
            db.SaveChanges();
        }
        public Dictionary<SkillType, bool> SkillToInstall(Player p)
        {
            Dictionary<SkillType, bool> d = new();
            foreach (var sk in skillz)
            {
                if (skills.FirstOrDefault(skill => skill.Value?.type == sk.type).Value == null && sk.MeetReqs(p))
                {
                    d.Add(sk.type, true);
                }
            }
            return d;
        }
        public int lvlsummary() => skills.Sum(i => i.Value?.lvl ?? 0);
        public UpSkill[] GetSkills()
        {
            List<UpSkill> ski = new();
            LoadSkills();
            foreach (var i in skills)
            {
                if (i.Value != null)
                {
                    ski.Add(new UpSkill(i.Key, i.Value.lvl, i.Value.isUpReady(), i.Value.type));
                }
            }
            return ski.ToArray();
        }
        public int slots { get; set; } = 20;
        [NotMapped]
        public Dictionary<int, Skill?> skills = new();
        [NotMapped]
        public static List<Skill> skillz = new List<Skill>()
        {
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => CoeffSkill.Digcoast((float)x),
effectfunc = (int x) => CoeffSkill.Digeffect((float)x),
expfunc = (int x) => CoeffSkill.Digexp((float)x),
                    type = SkillType.Digging, // dick
                    effecttype = SkillEffectType.OnDig
                },
                new Skill()
                {
                    requirements = new() { {SkillType.Digging,1} },
                    costfunc = (int x) => CoeffSkill.Roadcoast(x),
effectfunc = (int x) => CoeffSkill.Roadeffect(x),
expfunc = (int x) => CoeffSkill.Roadexp(x),
                    type = SkillType.BuildRoad,
                    effecttype = SkillEffectType.OnBld
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => CoeffSkill.Greencoast(x),
effectfunc = (int x) => CoeffSkill.Greeneff(x),
expfunc = (int x) => CoeffSkill.Greenexp(x),
                    dopfunc = (int x) => x,
                    type = SkillType.BuildGreen,
                    effecttype = SkillEffectType.OnBld,
                    description = (lvl,effect,dopeffect,cost,expcurrent,expneed) =>
                    {
                        return $"Стройка зеленых Уровень:{lvl}\nExp - {expcurrent}/{expneed}\nСтоимость блока: {effect}\nПрочность блока: {dopeffect}";
                    }
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => CoeffSkill.Yellowcoast(x),
effectfunc = (int x) => CoeffSkill.Yelloweffect(x),
expfunc = (int x) => CoeffSkill.Yellowexp(x),
                    dopfunc = (int x) => x,
                    type = SkillType.BuildYellow,
                    effecttype = SkillEffectType.OnBld,
                    description = (lvl,effect,dopeffect,cost,expcurrent,expneed) =>
                    {
                        return $"Стройка желтых Уровень:{lvl}\nExp - {expcurrent}/{expneed}\nСтоимость блока: {effect}\nПрочность блока: {dopeffect}";
                    }
                },
                 new Skill()
                {
                     requirements = null,
                    costfunc = (int x) => CoeffSkill.Redcoast(x),
effectfunc = (int x) => CoeffSkill.Redeffect(x),
expfunc = (int x) => CoeffSkill.Redexp(x),
                    dopfunc = (int x) => x,
                    type = SkillType.BuildRed,
                    effecttype = SkillEffectType.OnBld,
                     description = (lvl,effect,dopeffect,cost,expcurrent,expneed) =>
                    {
                        return $"Стройка красных Уровень:{lvl}\nExp - {expcurrent}/{expneed}\nСтоимость блока: {effect}\nПрочность блока: {dopeffect}";
                    }
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => CoeffSkill.Oporacoast(x),
effectfunc = (int x) => CoeffSkill.Oporaeffect(x),
expfunc = (int x) => CoeffSkill.Oporaexp(x),
                    type = SkillType.BuildStructure,
                    effecttype = SkillEffectType.OnBld
                },
                 new Skill()
                {
                     requirements = new() {{SkillType.Digging,100000} },
                    costfunc = (int x) => 1f,
                    effectfunc = (int x) => 1f,
                    dopfunc = (int x) => x,
                    expfunc = (int x) => 1f,
                    type = SkillType.BuildWar,
                    effecttype = SkillEffectType.OnBld,
                    description = (lvl,effect,dopeffect,cost,expcurrent,expneed) =>
                    {
                        return $"Стройка ВБ:{lvl}\nExp - {expcurrent}/{expneed}\nСтоимость блока: {effect}\nПрочность блока: {dopeffect}";
                    }
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => 1f,
                    effectfunc = (int x) => 1f,
                    expfunc = (int x) => 1f,
                    type = SkillType.Fridge, // охлад
                    effecttype = SkillEffectType.OnMove
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => CoeffSkill.Movecoast(x),
effectfunc = (int x) => 70f - x * 0.05f > 30f ? 70f - x * 0.05f : 30f,
expfunc = (int x) => CoeffSkill.Moveexp(x),
                    type = SkillType.Movement, // движение,
                    effecttype = SkillEffectType.OnMove,
                    description = (lvl,effect,dopeffect,cost,expcurrent,expneed) =>
                    {
                        return $"Передвижение Уровень:{lvl}\nExp - {expcurrent}/{expneed}.      Стоимость: { cost}$\nСкорость передвижения {Math.Round((1 / (effect * 1.2f * 0.001f)) * 0.3f * 3.6f,2)} км/ч";
                    }
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => 20000000f,
effectfunc = (int x) => 20f,
expfunc = (int x) => 20000000f,
                    type = SkillType.RoadMovement, // по дорогам
                    effecttype = SkillEffectType.OnMove
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => CoeffSkill.Capacitycoast(x),
effectfunc = (int x) => CoeffSkill.Capacityeffect(x),
expfunc = (int x) => CoeffSkill.Capacityexp(x),
                    type = SkillType.Packing, // упаковка
                    effecttype = SkillEffectType.OnPackCrys
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => CoeffSkill.Hpcoast((float)x),
effectfunc = (int x) => CoeffSkill.Hpeffect((float)x),
expfunc = (int x) => CoeffSkill.Hpexp((float)x),
                    type = SkillType.Health, // хп
                    effecttype = SkillEffectType.OnHealth
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => 1f,
                    effectfunc = (int x) => 1f,
                    expfunc = (int x) => 1f,
                    type = SkillType.PackingBlue, // упаковка синь
                    effecttype = SkillEffectType.OnPackCrys
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => 1f,
                    effectfunc = (int x) => 1f,
                    expfunc = (int x) => 1f,
                    type = SkillType.PackingCyan, // упаковка голь
                    effecttype = SkillEffectType.OnPackCrys
                },
                 new Skill()
                {
                     requirements = null,
                    costfunc = (int x) => 1f,
                    effectfunc = (int x) => 1f,
                    expfunc = (int x) => 1f,
                    type = SkillType.PackingGreen, // упаковка зель
                    effecttype = SkillEffectType.OnPackCrys
                },
                  new Skill()
                {
                      requirements = null,
                    costfunc = (int x) => 1f,
                    effectfunc = (int x) => 1f,
                    expfunc = (int x) => 1f,
                    type = SkillType.PackingRed, // упаковка крась
                    effecttype = SkillEffectType.OnPackCrys
                },
                    new Skill()
                {
                        requirements = null,
                    costfunc = (int x) => 1f,
                    effectfunc = (int x) => 1f,
                    expfunc = (int x) => 1f,
                    type = SkillType.PackingViolet, // упаковка фиоль
                    effecttype = SkillEffectType.OnPackCrys
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => 1f,
                    effectfunc = (int x) => 1f,
                    expfunc = (int x) => 1f,
                    type = SkillType.PackingWhite, // упаковка бель
                    effecttype = SkillEffectType.OnPackCrys
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => CoeffSkill.Minecoast((float)x),
effectfunc = (int x) => CoeffSkill.Mineeffect((float)x),
expfunc = (int x) => CoeffSkill.Mineexp((float)x),
                    type = SkillType.MineGeneral, // доба
                    effecttype = SkillEffectType.OnDigCrys
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) => 1f,
                    effectfunc = (int x) => 100f + x * 0.2f,
                    expfunc = (int x) => 1f,
                    type = SkillType.Induction, // инда
                    effecttype = SkillEffectType.OnHurt
                },
                new Skill()
                {
                    requirements = null,
                    costfunc = (int x) =>CoeffSkill.Zopcoast(x),
effectfunc = (int x) =>  CoeffSkill.Zopeffect(x),
expfunc = (int x) => CoeffSkill.Zopexp(x),
                    type = SkillType.AntiGun, // антипуфка
                    effecttype = SkillEffectType.OnHurt
                }

        };
    }
}
