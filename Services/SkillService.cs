using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YamiiDarkFantasy.Models;

namespace YamiiDarkFantasy.Services
{
    public class SkillService
    {
        // --- 計算・判定ヘルパー ---

        public int CalculateSkillValue(Skill skill, (int MaxHP, int STR, int DEF, int INT, int DEX, int AGI, int LUK, int Faith) pStats, Player p, Enemy e, bool isBonus = false)
        {
            double basePower = skill.Power;
            if (isBonus) basePower *= 0.5;

            double scaler = 0;
            switch (skill.ScalingValue)
            {
                case "STR": scaler = pStats.STR * 2; break;
                case "DEX": scaler = pStats.DEX * 2; break;
                case "INT": scaler = pStats.INT * 2; break;
                case "Faith": scaler = pStats.Faith * 2; break;
                case "STR_Faith": scaler = (pStats.STR + pStats.Faith); break;
                case "INT_Faith": scaler = (pStats.INT + pStats.Faith); break;
                case "DEX_INT": scaler = (pStats.DEX + pStats.INT); break;
                case "STR_INT": scaler = (pStats.STR + pStats.INT); break;
                case "STR_DEX": scaler = (pStats.STR + pStats.DEX); break;
                case "Level": scaler = p.Level * 3; break;
                case "MaxHP": scaler = pStats.MaxHP * 0.2; break;
                case "CurrentHP": scaler = p.CurrentHP * 0.2; break;
                case "EnemyMaxHP": scaler = e.MaxHP * 0.1; break;
                case "EnemyHP": scaler = e.CurrentHP * 0.1; break;
                case "Shield": scaler = p.Shield * 0.5; break;
                case "Reflect": scaler = p.Reflect * 1.0; break;
                case "AllStat": scaler = (pStats.STR + pStats.DEX + pStats.INT + pStats.Faith + pStats.AGI) / 2.0; break;
                default: scaler = pStats.STR * 2; break;
            }

            int finalValue = (int)(basePower * (1.0 + (skill.Level - 1) * 0.2) + scaler);
            return Math.Max(1, finalValue);
        }

        public int ApplyDamageWithDefenses(int damage, ref int targetBarrier, ref int targetShield, ref int targetHP, string targetName, ref string log)
        {
            if (targetBarrier > 0)
            {
                targetBarrier--;
                log += $"🛡️ {targetName} のバリアがダメージを完全に無効化した！ (残りバリア: {targetBarrier})\n";
                return 0;
            }

            if (targetShield > 0)
            {
                int absorbed = Math.Min(targetShield, damage);
                targetShield -= absorbed;
                damage -= absorbed;
                log += $"🛡️ {targetName} のシールドが {absorbed} ダメージを肩代わりした！ (残りシールド: {targetShield})\n";
            }

            if (damage > 0)
            {
                targetHP -= damage;
                log += $"{targetName} に {damage} ダメージ！\n";
            }

            return damage;
        }

        public void ApplyLevelBonus(Skill skill, Player p, Enemy e, List<StatusEffect> pEff, List<StatusEffect> eEff, ref string log)
        {
            // ローカル関数は ref を直接触らず、結果の文字列を返すように変更
            string ProcessBonus(string? bonus)
            {
                string resultLog = "";
                if (string.IsNullOrEmpty(bonus)) return resultLog;
                
                if (bonus.Contains("かくとく:"))
                {
                    resultLog += $"✨ スキルボーナス発動！ [{bonus}]\n";
                }
                if (bonus.Contains("スタン"))
                {
                    eEff.Add(new StatusEffect { Name = "スタン", Type = EffectType.Paralysis, Duration = 1, Value = 100 });
                    resultLog += $"✨ スキルボーナス：{e.Name} をスタンさせた！\n";
                }
                return resultLog;
            }

            // 返ってきた文字列を log に足し合わせる
            if (skill.Level >= 3) log += ProcessBonus(skill.Bonus3Effect);
            if (skill.Level >= 8) log += ProcessBonus(skill.Bonus8Effect);
        }

        public async Task<string> TriggerPreemptiveSkillsAsync(Player p, Enemy e, List<Skill> skills, GameSession session)
        {
        string localLog = ""; // メソッド内でログを貯める変数

        if (session.IsPreemptiveTriggered) return localLog;
        session.IsPreemptiveTriggered = true;

        var pStats = p.GetTotalStats();
        var preemptiveSkills = skills.Where(s => s.IsPreemptive).ToList();

        foreach (var sk in preemptiveSkills)
        {
            localLog += $"⚡ **先制！** あなたの【{sk.Name}】が発動！\n";
            int dmg = CalculateSkillValue(sk, pStats, p, e);
            int enemyHP = e.CurrentHP;
            int enemyBarrier = e.Barrier;
            int enemyShield = e.Shield;

            // ApplyDamageWithDefenses は同期メソッドのままなので、localLog を ref で渡して問題ありません
            ApplyDamageWithDefenses(dmg, ref enemyBarrier, ref enemyShield, ref enemyHP, e.Name, ref localLog);

            e.CurrentHP = enemyHP; 
            e.Barrier = enemyBarrier; 
            e.Shield = enemyShield;
        }

        return localLog; // 蓄積したログを返す
        }

        // --- 状態異常・バフのヘルパー ---

        public bool ProcessStatusEffects(List<StatusEffect> effects, ref int currentHp, string targetName, ref string actionLog)
        {
            bool skipTurn = false;
            foreach (var effect in effects.ToList())
            {
                if (effect.Type == EffectType.Poison)
                {
                    currentHp = Math.Max(0, currentHp - effect.Value);
                    actionLog += $"☠️ {targetName} は毒により {effect.Value} ダメージを受けた！\n";
                }
                else if (effect.Type == EffectType.Paralysis)
                {
                    if (new Random().Next(100) < effect.Value)
                    {
                        actionLog += $"⚡ {targetName} は体が痺れて動けない！\n";
                        skipTurn = true;
                    }
                }
            }
            return skipTurn;
        }

        public void ApplySkillEffects(Skill skill, List<StatusEffect> casterEffects, List<StatusEffect> targetEffects, string casterName, string targetName, ref string actionLog)
        {
            if (skill.ApplyEffect == null) return;
            
            var targetList = skill.AppliesToSelf ? casterEffects : targetEffects;
            var displayName = skill.AppliesToSelf ? casterName : targetName;

            var existing = targetList.FirstOrDefault(e => e.Name == skill.ApplyEffect.Name);
            if (existing != null)
            {
                existing.Duration = Math.Max(existing.Duration, skill.ApplyEffect.Duration);
            }
            else
            {
                targetList.Add(skill.ApplyEffect.Clone());
                actionLog += $"✨ {displayName} は【{skill.ApplyEffect.Name}】状態になった！\n";
            }
        }

        public void DecreaseEffectDurations(List<StatusEffect> effects)
        {
            foreach (var eff in effects.ToList())
            {
                eff.Duration--;
                if (eff.Duration <= 0) effects.Remove(eff);
            }
        }
    }
}
