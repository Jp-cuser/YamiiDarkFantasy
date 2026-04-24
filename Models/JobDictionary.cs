using System.Collections.Generic;

namespace YamiiDarkFantasy.Models
{
    public static class JobDictionary
    {
        public class JobData
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public int MaxHP { get; set; }
            public int STR { get; set; }
            public int DEF { get; set; }
            public int INT { get; set; }
            public int DEX { get; set; }
            public int AGI { get; set; }
            public int LUK { get; set; }
            public int Faith { get; set; }
            public List<Equipment> InitialEquipments { get; set; } = new();
            public List<Skill> InitialSkills { get; set; } = new();
        }

        public static readonly Dictionary<string, JobData> Jobs = new Dictionary<string, JobData>
        {
            {
                "knight", new JobData
                {
                    Name = "騎士",
                    Description = "高いHPと防御力を持ち、前線で耐え抜く堅牢な戦士。",
                    MaxHP = 120, STR = 8, DEF = 12, INT = 3, DEX = 5, AGI = 3, LUK = 5, Faith = 5,
                    InitialEquipments = new List<Equipment> 
                    {
                        Equipment.GetBaseEquipment("old_longsword"),
                        Equipment.GetBaseEquipment("iron_armor")
                    },
                    InitialSkills = new List<Skill>
                    {
                        Skill.GetSkill("bash"),
                        Skill.GetSkill("guard")
                    }
                }
            },
            {
                "cleric", new JobData
                {
                    Name = "聖職者",
                    Description = "神聖な祈りで傷を癒やし、魔術で敵を討つ後衛職。",
                    MaxHP = 80, STR = 3, DEF = 5, INT = 8, DEX = 4, AGI = 4, LUK = 8, Faith = 15,
                    InitialEquipments = new List<Equipment>
                    {
                        Equipment.GetBaseEquipment("priest_staff"),
                        Equipment.GetBaseEquipment("pilgrim_robe")
                    },
                    InitialSkills = new List<Skill>
                    {
                        Skill.GetSkill("holy_bit"),
                        Skill.GetSkill("first_aid")
                    }
                }
            },
            {
                "samurai", new JobData
                {
                    Name = "侍",
                    Description = "洗練された技巧と高い攻撃力で一撃必殺を狙う剣客。",
                    MaxHP = 90, STR = 12, DEF = 4, INT = 2, DEX = 10, AGI = 8, LUK = 4, Faith = 2,
                    InitialEquipments = new List<Equipment>
                    {
                        Equipment.GetBaseEquipment("uchigatana"),
                        Equipment.GetBaseEquipment("hachigane")
                    },
                    InitialSkills = new List<Skill>
                    {
                        Skill.GetSkill("けさぎり"),
                        Skill.GetSkill("back_step")
                    }
                }
            },
            {
                "traveler", new JobData
                {
                    Name = "旅人",
                    Description = "目立った強さはないが、すべてにおいて平均的で運が良い。",
                    MaxHP = 100, STR = 6, DEF = 6, INT = 6, DEX = 6, AGI = 6, LUK = 15, Faith = 6,
                    InitialEquipments = new List<Equipment>
                    {
                        Equipment.GetBaseEquipment("crude_dagger"),
                        Equipment.GetBaseEquipment("lucky_charm")
                    },
                    InitialSkills = new List<Skill>
                    {
                        Skill.GetSkill("いしつぶて"),
                        Skill.GetSkill("feint")
                    }
                }
            },
            {
                "fighter", new JobData
                {
                    Name = "格闘家",
                    Description = "己の肉体のみを頼りに戦う。高いHPと敏捷性を持つ。",
                    MaxHP = 110, STR = 10, DEF = 4, INT = 2, DEX = 8, AGI = 12, LUK = 4, Faith = 2,
                    InitialEquipments = new List<Equipment>
                    {
                        Equipment.GetBaseEquipment("bandage"),
                        Equipment.GetBaseEquipment("dogi")
                    },
                    InitialSkills = new List<Skill>
                    {
                        Skill.GetSkill("jab"),
                        Skill.GetSkill("feint")
                    }
                }
            }
        };

        public static Player CreatePlayerWithJob(ulong userId, string jobId)
        {
            if (!Jobs.TryGetValue(jobId, out var jobData))
            {
                jobData = Jobs["traveler"]; // デフォルト
            }

            var player = new Player
            {
                UserId = userId,
                Job = jobData.Name,
                MaxHP = jobData.MaxHP,
                CurrentHP = jobData.MaxHP,
                STR = jobData.STR,
                DEF = jobData.DEF,
                INT = jobData.INT,
                DEX = jobData.DEX,
                AGI = jobData.AGI,
                LUK = jobData.LUK,
                Faith = jobData.Faith,
                Level = 1,
                Exp = 0,
                Floor = 1,
                RoomsExplored = 0
            };

            // 初期装備と初期スキルをJSON化して保存
            player.EquipmentsJson = System.Text.Json.JsonSerializer.Serialize(jobData.InitialEquipments);
            player.SkillsJson = System.Text.Json.JsonSerializer.Serialize(jobData.InitialSkills);
            player.StatusAilmentsJson = "[]";

            return player;
        }
    }
}
