using System.Collections.Generic;

namespace YamiiDarkFantasy.Models
{
    public static class EquipmentModifier
    {
        // 定義済みID定数
        public const string Vampiric = "vampiric";
        public const string HolyAura = "holy_aura";
        public const string Crushing = "crushing";
        public const string ManaWell = "mana_well";
        public const string Sturdy = "sturdy";
        public const string Parry = "parry";
        public const string HeavyWeight = "heavy_weight";
        public const string Concealment = "concealment";
        public const string HeadGuard = "head_guard";
        public const string WindStep = "wind_step";
        public const string BloodCurse = "blood_curse";

        // 表示名と説明用
        public static readonly Dictionary<string, string> FixedModifierNames = new()
        {
            { Vampiric, "💉吸血(与ダメ5%回復)" },
            { HolyAura, "✨聖気(アンデッド特効/浄化回復)" },
            { Crushing, "🔨粉砕(防御貫通上昇)" },
            { ManaWell, "🌀魔力泉(INT+15%)" },
            { Sturdy, "🛡️堅牢(被弾ダメ-5)" },
            { Parry, "✋受け流し(回避率+5%)" },
            { HeavyWeight, "🧱重量級(DEF+20%)" },
            { Concealment, "👤隠密(狙われ率低下/回避+10%)" },
            { HeadGuard, "💂保護(最大HP+10%)" },
            { WindStep, "🍃風歩(AGI+10)" },
            { BloodCurse, "🩸血の呪い(STR+20/HP徐々に減少)" }
        };

        public static string GetModifierName(string id)
        {
            return FixedModifierNames.TryGetValue(id, out var name) ? name : "不明な効果";
        }

        // --- ステータス計算時のロジック ---
        public static void ApplyStatPercentages(ref int hp, ref int intVal, ref int defVal, List<Equipment> equips)
        {
            foreach (var eq in equips)
            {
                if (eq.FixedModifierId == ManaWell) intVal = (int)(intVal * 1.15);
                if (eq.FixedModifierId == HeavyWeight) defVal = (int)(defVal * 1.20);
                if (eq.FixedModifierId == HeadGuard) hp = (int)(hp * 1.10);
            }
        }

        // --- 戦闘中のロジック (攻撃時) ---
        public static int AdjustEnemyDefense(int enemyDef, List<Equipment> playerEquips)
        {
            if (playerEquips.Exists(e => e.FixedModifierId == Crushing))
            {
                return (int)(enemyDef * 0.7);
            }
            return enemyDef;
        }

        public static void ProcessAttackEffects(int damage, Player player, List<Equipment> playerEquips, ref string actionLog)
        {
            if (playerEquips.Exists(e => e.FixedModifierId == Vampiric))
            {
                int vHeal = System.Math.Max(1, damage / 10);
                player.CurrentHP = System.Math.Min(player.CurrentHP + vHeal, player.MaxHP);
                actionLog += $"💉 吸血により体力が **{vHeal}** 回復した！\n";
            }
        }

        // --- 戦闘中のロジック (防御時・回避時) ---
        public static int GetEvasionBonus(List<Equipment> playerEquips)
        {
            int bonus = 0;
            if (playerEquips.Exists(eq => eq.FixedModifierId == Parry)) bonus += 5;
            if (playerEquips.Exists(eq => eq.FixedModifierId == Concealment)) bonus += 10;
            return bonus;
        }

        public static int AdjustReceivedDamage(int damage, List<Equipment> playerEquips, ref string actionLog)
        {
            if (playerEquips.Exists(eq => eq.FixedModifierId == Sturdy))
            {
                int reduced = damage >= 5 ? 5 : damage;
                actionLog += $"🛡️ 堅牢な盾が衝撃を和らげた！ (-{reduced}dmg)\n";
                return damage - reduced;
            }
            return damage;
        }

        // --- ターン終了時のロジック ---
        public static void ProcessTurnEndEffects(Player player, List<Equipment> playerEquips, ref string actionLog)
        {
            if (playerEquips.Exists(eq => eq.FixedModifierId == BloodCurse))
            {
                int selfDmg = 5;
                player.CurrentHP = System.Math.Max(0, player.CurrentHP - selfDmg);
                actionLog += $"🩸 【血の呪い】があなたの命を蝕む... (-{selfDmg}dmg)\n";
            }
        }
    }
}
