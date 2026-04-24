using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YamiiDarkFantasy.Models
{
    public class Player
    {
        [Key]
        public ulong UserId { get; set; } // DiscordのユーザーID
        public string Job { get; set; } = "戦士";
        public int Level { get; set; } = 1;
        public int Exp { get; set; } = 0;
        public int MaxHP { get; set; } = 100;
        public int CurrentHP { get; set; } = 100;
        public int STR { get; set; } = 10;
        public int DEF { get; set; } = 5;
        public int INT { get; set; } = 5;
        public int DEX { get; set; } = 5;
        public int AGI { get; set; } = 5;
        public int LUK { get; set; } = 5;
        public int Faith { get; set; } = 5; // 信仰

        // 戦闘中の一時的なステータス
        public int Barrier { get; set; } = 0;
        public int Shield { get; set; } = 0;
        public int Reflect { get; set; } = 0;
        
        public int Floor { get; set; } = 1;
        public int RoomsExplored { get; set; } = 0;

        // シリアライズして保存するためのプロパティ
        [Column(TypeName = "TEXT")]
        public string SkillsJson { get; set; } = "[]"; 
        [Column(TypeName = "TEXT")]
        public string EquipmentsJson { get; set; } = "[]"; 
        [Column(TypeName = "TEXT")]
        public string StatusAilmentsJson { get; set; } = "[]"; 

        public (int MaxHP, int STR, int DEF, int INT, int DEX, int AGI, int LUK, int Faith) GetTotalStats()
        {
            int tMaxHP = MaxHP, tSTR = STR, tDEF = DEF, tINT = INT, tDEX = DEX, tAGI = AGI, tLUK = LUK, tFaith = Faith;

            try
            {
                var equips = System.Text.Json.JsonSerializer.Deserialize<List<YamiiDarkFantasy.Models.Equipment>>(EquipmentsJson) ?? new();
                foreach (var eq in equips)
                {
                    tMaxHP += eq.BonusMaxHP;
                    tSTR += eq.BonusSTR;
                    tDEF += eq.BonusDEF;
                    tINT += eq.BonusINT;
                    tDEX += eq.BonusDEX;
                    tAGI += eq.BonusAGI;
                    tLUK += eq.BonusLUK;
                    // Equipment に Faith 補正が追加されることを想定して仮実装
                }

                // 固有修飾子による割合補正（一括適用）
                EquipmentModifier.ApplyStatPercentages(ref tMaxHP, ref tINT, ref tDEF, equips);

                // バフ・デバフの反映
                var effects = System.Text.Json.JsonSerializer.Deserialize<List<YamiiDarkFantasy.Models.StatusEffect>>(StatusAilmentsJson) ?? new();
                foreach (var effect in effects)
                {
                    int val = effect.Type == YamiiDarkFantasy.Models.EffectType.Buff ? effect.Value : (effect.Type == YamiiDarkFantasy.Models.EffectType.Debuff ? -effect.Value : 0);
                    if (val != 0)
                    {
                        switch (effect.TargetStat)
                        {
                            case "STR": tSTR += val; break;
                            case "DEF": tDEF += val; break;
                            case "INT": tINT += val; break;
                            case "DEX": tDEX += val; break;
                            case "AGI": tAGI += val; break;
                            case "LUK": tLUK += val; break;
                            case "Faith": tFaith += val; break;
                        }
                    }
                }
            }
            catch { }

            return (
                tMaxHP, 
                System.Math.Max(1, tSTR), 
                System.Math.Max(1, tDEF), 
                System.Math.Max(1, tINT), 
                System.Math.Max(1, tDEX), 
                System.Math.Max(1, tAGI), 
                System.Math.Max(1, tLUK),
                System.Math.Max(1, tFaith)
            );
        }

        // 習得済みスキル + 武器固有スキルの合計を返す
        public List<Skill> GetAvailableSkills()
        {
            var skills = System.Text.Json.JsonSerializer.Deserialize<List<Skill>>(SkillsJson) ?? new();
            try
            {
                var equips = System.Text.Json.JsonSerializer.Deserialize<List<Equipment>>(EquipmentsJson) ?? new();
                var weapon = equips.FirstOrDefault(e => e.EquipSlot == "Weapon");
                if (weapon != null && !string.IsNullOrEmpty(weapon.ProvidedSkillId))
                {
                    var weaponSkill = Skill.GetSkill(weapon.ProvidedSkillId);
                    // 武器スキルの名前を少し強調
                    weaponSkill.Name = $"⚔️{weaponSkill.Name}";
                    skills.Add(weaponSkill);
                }
            }
            catch { }
            return skills;
        }

        public int GetRequiredExpForNextLevel()
        {
            // 必要経験値テーブル（例：Lv1➔Lv2は 20、Lv2➔Lv3は 40）
            return Level * 20;
        }

        public string CheckAndApplyLevelUp()
        {
            string levelUpLogs = "";
            bool leveledUp = false;

            while (Exp >= GetRequiredExpForNextLevel())
            {
                Exp -= GetRequiredExpForNextLevel();
                Level++;
                
                // ステータス上昇処理
                MaxHP += 5;
                STR += 1;
                DEF += 1;
                INT += 1;
                DEX += 1;
                AGI += 1;
                LUK += 1;
                Faith += 1;
                leveledUp = true;
            }

            if (leveledUp)
            {
                // レベルアップでHPを全回復
                CurrentHP = MaxHP;
                levelUpLogs = $"\n🌟 **レベルアップ！** (現在のレベル: **Lv.{Level}**)\n基礎能力が全体的に少しずつ上昇し、体力が完全に回復した！";
            }

            return levelUpLogs;
        }
    }
}