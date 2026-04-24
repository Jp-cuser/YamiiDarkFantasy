using System.Collections.Generic;

namespace YamiiDarkFantasy.Models
{
    public class EquipmentTemplate
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string EquipSlot { get; set; } = "Weapon";
        
        // 基礎ステータス
        public int BonusMaxHP { get; set; }
        public int BonusSTR { get; set; }
        public int BonusDEF { get; set; }
        public int BonusINT { get; set; }
        public int BonusDEX { get; set; }
        public int BonusAGI { get; set; }
        public int BonusLUK { get; set; }

        // 固有効果と武器スキル
        public string FixedModifierId { get; set; } = ""; // 固有修飾子ID
        public string ProvidedSkillId { get; set; } = ""; // 固有スキルID
    }

    public static class EquipmentDictionary
    {
        public static readonly List<EquipmentTemplate> AvailableTemplates = new()
        {
            // --- 武器 (Weapon) ---
            new EquipmentTemplate { 
                Id = "holy_excalibur", Name = "聖剣エクスカリバー", EquipSlot = "Weapon", 
                BonusSTR = 15, BonusINT = 10, FixedModifierId = EquipmentModifier.HolyAura, ProvidedSkillId = "smite" 
            },
            new EquipmentTemplate { 
                Id = "blood_dagger", Name = "吸血の短剣", EquipSlot = "Weapon", 
                BonusSTR = 5, BonusAGI = 10, FixedModifierId = EquipmentModifier.Vampiric, ProvidedSkillId = "iai" 
            },
            new EquipmentTemplate { 
                Id = "heavy_mace", Name = "ヘヴィメイス", EquipSlot = "Weapon", 
                BonusSTR = 12, BonusDEF = 5, FixedModifierId = EquipmentModifier.Crushing, ProvidedSkillId = "shield_bash" 
            },
            new EquipmentTemplate { 
                Id = "wizard_staff", Name = "賢者の杖", EquipSlot = "Weapon", 
                BonusINT = 20, FixedModifierId = EquipmentModifier.ManaWell, ProvidedSkillId = "fireball" 
            },

            // --- 盾 (Shield) ---
            new EquipmentTemplate { 
                Id = "tower_shield", Name = "タワーシールド", EquipSlot = "Shield", 
                BonusDEF = 20, BonusMaxHP = 50, FixedModifierId = EquipmentModifier.Sturdy
            },
            new EquipmentTemplate { 
                Id = "buckler", Name = "バックラー", EquipSlot = "Shield", 
                BonusDEF = 8, BonusAGI = 5, FixedModifierId = EquipmentModifier.Parry
            },

            // --- 防具 (Armor) ---
            new EquipmentTemplate { 
                Id = "plate_mail", Name = "プレートメイル", EquipSlot = "Armor", 
                BonusDEF = 25, BonusMaxHP = 40, FixedModifierId = EquipmentModifier.HeavyWeight
            },
            new EquipmentTemplate { 
                Id = "thief_leather", Name = "盗賊の革鎧", EquipSlot = "Armor", 
                BonusDEF = 10, BonusAGI = 15, BonusDEX = 10, FixedModifierId = EquipmentModifier.Concealment
            },

            // --- その他 (Head, Cloak, Accessory) ---
            new EquipmentTemplate { 
                Id = "iron_helmet", Name = "鉄の兜", EquipSlot = "Head", 
                BonusDEF = 5, BonusMaxHP = 20, FixedModifierId = EquipmentModifier.HeadGuard
            },
            new EquipmentTemplate { 
                Id = "traveler_cloak", Name = "旅人の外套", EquipSlot = "Cloak", 
                BonusDEF = 2, BonusAGI = 8, FixedModifierId = EquipmentModifier.WindStep
            },
            new EquipmentTemplate { 
                Id = "cursed_ring", Name = "呪いの指輪", EquipSlot = "Accessory", 
                BonusSTR = 20, BonusLUK = -10, FixedModifierId = EquipmentModifier.BloodCurse
            }
        };
    }
}
