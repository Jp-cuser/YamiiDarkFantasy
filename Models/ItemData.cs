using System;
using System.Collections.Generic;
using System.Linq;

namespace YamiiDarkFantasy.Models
{
    public class Equipment
    {
        public string EquipmentId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        
        // 装備枠: Weapon, Shield, Head, Armor, Cloak, Accessory
        public string EquipSlot { get; set; } = "Weapon"; 
        
        // レアリティ: 1(None), 2(1 Mod), 3(3 Mods), 4(5 Mods), 5(7 Mods)
        public int Rarity { get; set; } = 1;

        // 基礎ボーナス
        public int BonusMaxHP { get; set; }
        public int BonusSTR { get; set; }
        public int BonusDEF { get; set; }
        public int BonusINT { get; set; }
        public int BonusDEX { get; set; }
        public int BonusAGI { get; set; }
        public int BonusLUK { get; set; }

        public string FixedModifierId { get; set; } = ""; // 固有修飾子ID
        public string ProvidedSkillId { get; set; } = ""; // 固有スキルID

        public List<string> Modifiers { get; set; } = new List<string>();

        // 表示用ヘルパー
        private static readonly Dictionary<string, string> SlotNames = new()
        {
            { "Weapon", "武器" }, { "Shield", "盾" }, { "Head", "兜" },
            { "Armor", "鎧" }, { "Cloak", "外套" }, { "Accessory", "アクセサリ" }
        };


        private static readonly string[] RarityStars = { "☆", "★", "★★", "★★★", "★★★★" };

        public string ToDetailString()
        {
            string slotLabel = SlotNames.TryGetValue(EquipSlot, out var sn) ? sn : EquipSlot;
            string rarityLabel = Rarity >= 1 && Rarity <= 5 ? RarityStars[Rarity - 1] : "?";
            
            var lines = new List<string>();
            lines.Add($"【{Name}】 ({slotLabel}) {rarityLabel}");
            
            var coreInfo = new List<string>();
            if (!string.IsNullOrEmpty(FixedModifierId) && EquipmentModifier.FixedModifierNames.TryGetValue(FixedModifierId, out var fmn))
            {
                coreInfo.Add($"固有: **{fmn}**");
            }
            if (!string.IsNullOrEmpty(ProvidedSkillId))
            {
                // Skill.GetSkill メソッドを使って取得するように変更
                var skillTemplate = Skill.GetSkill(ProvidedSkillId);
                string skName = skillTemplate != null ? skillTemplate.Name : ProvidedSkillId;
                coreInfo.Add($"スキル: **『{skName}』**");
            }
            if (coreInfo.Count > 0) lines.Add(string.Join(" / ", coreInfo));

            var bonuses = new List<string>();
            if (BonusMaxHP != 0) bonuses.Add($"HP:{BonusMaxHP}");
            if (BonusSTR != 0) bonuses.Add($"STR:{BonusSTR}");
            if (BonusDEF != 0) bonuses.Add($"DEF:{BonusDEF}");
            if (BonusINT != 0) bonuses.Add($"INT:{BonusINT}");
            if (BonusDEX != 0) bonuses.Add($"DEX:{BonusDEX}");
            if (BonusAGI != 0) bonuses.Add($"AGI:{BonusAGI}");
            if (BonusLUK != 0) bonuses.Add($"LUK:{BonusLUK}");
            if (bonuses.Count > 0) lines.Add($"> 補正: {string.Join(", ", bonuses)}");
            
            if (Modifiers.Count > 0) lines.Add($"> 追加: {string.Join(", ", Modifiers)}");
            
            return string.Join("\n", lines);
        }

        public static Equipment GetBaseEquipment(string id)
        {
            if (Equipment.BaseEquipments.TryGetValue(id, out var template))
            {
                return new Equipment
                {
                    EquipmentId = Guid.NewGuid().ToString("N"),
                    Name = template.Name,
                    Description = template.Description,
                    EquipSlot = template.EquipSlot,
                    Rarity = 1,
                    BonusMaxHP = template.BonusMaxHP,
                    BonusSTR = template.BonusSTR,
                    BonusDEF = template.BonusDEF,
                    BonusINT = template.BonusINT,
                    BonusDEX = template.BonusDEX,
                    BonusAGI = template.BonusAGI,
                    BonusLUK = template.BonusLUK,
                    Modifiers = new List<string>()
                };
            }
            return new Equipment { EquipmentId = Guid.NewGuid().ToString("N"), EquipSlot = "Weapon", Name = "謎の装備" }; // fallback
        }

        // --- マスターデータ (初期装備など用) ---
        public static readonly Dictionary<string, Equipment> BaseEquipments = new()
        {
            // 騎士
            { "old_longsword", new Equipment { EquipSlot = "Weapon", Name = "古びた長剣", BonusSTR = 5 } },
            { "iron_armor", new Equipment { EquipSlot = "Armor", Name = "鉄の胸当て", BonusDEF = 8, BonusMaxHP = 20 } },
            // 聖職者
            { "priest_staff", new Equipment { EquipSlot = "Weapon", Name = "司祭の杖", BonusINT = 6 } },
            { "pilgrim_robe", new Equipment { EquipSlot = "Cloak", Name = "巡礼者のローブ", BonusDEF = 2, BonusINT = 3 } },
            // 侍
            { "uchigatana", new Equipment { EquipSlot = "Weapon", Name = "打刀", BonusSTR = 8, BonusDEX = 4 } },
            { "hachigane", new Equipment { EquipSlot = "Head", Name = "鉢金", BonusDEF = 3 } },
            // 旅人
            { "crude_dagger", new Equipment { EquipSlot = "Weapon", Name = "粗末な短剣", BonusSTR = 3, BonusAGI = 2 } },
            { "lucky_charm", new Equipment { EquipSlot = "Accessory", Name = "幸運のお守り", BonusLUK = 5 } },
            // 格闘家
            { "bandage", new Equipment { EquipSlot = "Weapon", Name = "バンテージ", BonusSTR = 2, BonusAGI = 4 } },
            { "dogi", new Equipment { EquipSlot = "Armor", Name = "道着", BonusDEF = 3, BonusAGI = 3 } },
        };


        // 新・ランダム生成ファクトリー (リスト制)
        public static Equipment GenerateRandomEquipment(int floor, int luckBonus, int minRarity = 1)
        {
            var rand = new Random();
            
            // 1. EquipmentDictionary からベースを抽選 (完全ランダム)
            var templates = EquipmentDictionary.AvailableTemplates;
            var baseT = templates[rand.Next(templates.Count)];

            var equip = new Equipment
            {
                Name = baseT.Name,
                EquipSlot = baseT.EquipSlot,
                FixedModifierId = baseT.FixedModifierId,
                ProvidedSkillId = baseT.ProvidedSkillId,
                BonusMaxHP = baseT.BonusMaxHP,
                BonusSTR = baseT.BonusSTR,
                BonusDEF = baseT.BonusDEF,
                BonusINT = baseT.BonusINT,
                BonusDEX = baseT.BonusDEX,
                BonusAGI = baseT.BonusAGI,
                BonusLUK = baseT.BonusLUK
            };

            // 2. レアリティ判定 (LUK補正あり)
            int roll = rand.Next(100) + (luckBonus / 2);
            int modCount = 0;
            string rarityName = "";

            int finalRarity = 1;
            if (roll >= 98) finalRarity = 5;
            else if (roll >= 90) finalRarity = 4;
            else if (roll >= 75) finalRarity = 3;
            else if (roll >= 45) finalRarity = 2;

            if (finalRarity < minRarity) finalRarity = minRarity;

            equip.Rarity = finalRarity;
            switch (finalRarity)
            {
                case 5: modCount = 7; rarityName = "伝説の"; break;
                case 4: modCount = 5; rarityName = "名工の"; break;
                case 3: modCount = 3; rarityName = "希少な"; break;
                case 2: modCount = 1; rarityName = "良質な"; break;
                default: modCount = 0; rarityName = ""; break;
            }

            // 3. 追加ステータスボーナス (フロア微補正)
            int floorBonus = (floor / 3);
            if (floorBonus > 0)
            {
                equip.BonusMaxHP += floorBonus * 5;
                equip.BonusSTR += floorBonus;
                equip.BonusDEF += floorBonus;
            }

            // 4. ランダム修飾子付与
            for (int i = 0; i < modCount; i++)
            {
                ApplyRandomModifier(equip, rand);
            }

            // 名前の調整
            if (!string.IsNullOrEmpty(rarityName))
            {
                equip.Name = $"{rarityName}{equip.Name}";
            }

            return equip;
        }

        private static void ApplyRandomModifier(Equipment equip, Random rand)
        {
            var modTypes = new[] { "STR", "DEF", "INT", "HP", "DEX", "AGI", "LUK" };
            string type = modTypes[rand.Next(modTypes.Length)];
            int val = rand.Next(1, 4);

            switch (type)
            {
                case "STR": equip.BonusSTR += val; equip.Modifiers.Add("剛力"); break;
                case "DEF": equip.BonusDEF += val; equip.Modifiers.Add("鉄壁"); break;
                case "INT": equip.BonusINT += val; equip.Modifiers.Add("魔術"); break;
                case "HP": equip.BonusMaxHP += val * 5; equip.Modifiers.Add("生命"); break;
                case "DEX": equip.BonusDEX += val; equip.Modifiers.Add("技巧"); break;
                case "AGI": equip.BonusAGI += val; equip.Modifiers.Add("神速"); break;
                case "LUK": equip.BonusLUK += val; equip.Modifiers.Add("幸運"); break;
            }
        }
    }
}
