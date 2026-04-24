using System;

namespace YamiiDarkFantasy.Models
{
    public class Skill
    {
        public string SkillId { get; set; } = Guid.NewGuid().ToString("N");
        public int Level { get; set; } = 1;
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Cooldown { get; set; }
        public int CurrentCooldown { get; set; }
        public int Power { get; set; } = 10;
        public string SkillType { get; set; } = "Attack"; // Attack, Magic, Heal, Buff, Debuff

        // 新メカニズム用フラグ
        public bool IsTurnFree { get; set; } = false; // T: ターン無消費
        public int BarrierCost { get; set; } = 0;    // B: バリア消費量
        public bool IsPreemptive { get; set; } = false; // F: 先制発動
        
        // レベルボーナス内容（文字列で保持し、使用時にパース/判定）
        public string? Bonus3Effect { get; set; }
        public string? Bonus8Effect { get; set; }

        public int GetPowerByLevel()
        {
            // 威力 = 基礎威力 * (1.0 + (Level - 1) * 0.2)
            return (int)(Power * (1.0 + (Level - 1) * 0.2));
        }
        
        // 要求ステータス依存など
        public string ScalingValue { get; set; } = "STR"; // STR, INT, AGI

        // 状態異常・バフ効果付与用
        public StatusEffect? ApplyEffect { get; set; } = null;
        public bool AppliesToSelf { get; set; } = false; // true: 自分に適用, false: 相手に適用

        // 表示用ヘルパー
        public string ToDetailString()
        {
            var parts = new System.Collections.Generic.List<string>();
            parts.Add($"【{Name}】 (Lv.{Level})");
            // T, B, F の表示
            List<string> markers = new();
            if (IsTurnFree) markers.Add("T");
            if (BarrierCost > 0) markers.Add($"{BarrierCost}B");
            if (IsPreemptive) markers.Add("F");
            if (markers.Count > 0) parts.Add($"[{string.Join(" ", markers)}]");

            if (!string.IsNullOrEmpty(Description)) parts.Add(Description);
            
            string typeLabel = SkillType switch
            {
                "Attack" => "⚔️攻撃",
                "Magic" => "✨魔法",
                "Heal" => "💚回復",
                "Buff" => "🛡️バフ",
                _ => SkillType
            };
            int currentPower = GetPowerByLevel();
            string detailLine = $"{typeLabel} | 威力:{currentPower} | 依存:{ScalingValue} | CT:{Cooldown}";
            parts.Add(detailLine);

            if (ApplyEffect != null)
            {
                string effectLabel = ApplyEffect.Type switch
                {
                    EffectType.Poison => $"💀毒({ApplyEffect.Value}dmg/{ApplyEffect.Duration}T)",
                    EffectType.Paralysis => $"⚡麻痺({ApplyEffect.Duration}T)",
                    EffectType.Buff => $"⬆️{ApplyEffect.TargetStat}+{ApplyEffect.Value}({ApplyEffect.Duration}T)",
                    EffectType.Debuff => $"⬇️{ApplyEffect.TargetStat}-{ApplyEffect.Value}({ApplyEffect.Duration}T)",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(effectLabel))
                    parts.Add($"追加効果: {effectLabel}");
            }
            return string.Join("\n", parts);
        }

        public static Skill GetSkill(string id)
        {
            return SkillRegistry.GetSkill(id);
        }

        public static Skill GenerateRandomSkill(int floor)
        {
            var rand = new Random();
            var allKeys = new System.Collections.Generic.List<string>(SkillRegistry.Skills.Keys);
            string selectedId = allKeys[rand.Next(allKeys.Count)];
            
            var generated = GetSkill(selectedId);
            // フロアに応じたボーナス付与
            generated.Power += (floor * 2);

            return generated;
        }
    }
}
