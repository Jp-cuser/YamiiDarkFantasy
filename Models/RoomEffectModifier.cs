using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using YamiiDarkFantasy.Constants;

namespace YamiiDarkFantasy.Models
{
    public static class RoomEffectModifier
    {
        // 1. 戦闘開始時・初期化フック
        public static void ApplyBattleStartEffects(Player player, Enemy enemy, string? roomEffectId, List<Skill> learnedSkills, ref string actionLog)
        {
            if (string.IsNullOrEmpty(roomEffectId)) return;

            // すいぼつ (敵強化)
            if (roomEffectId == "submerged")
            {
                enemy.MaxHP = (int)(enemy.MaxHP * 1.2);
                enemy.CurrentHP = (int)(enemy.CurrentHP * 1.2);
            }

            // かいじゅう (スキル消失)
            if (roomEffectId == "soul_beast" && learnedSkills.Count > 0)
            {
                int removeIdx = new Random().Next(learnedSkills.Count);
                var lostSkill = learnedSkills[removeIdx];
                learnedSkills.RemoveAt(removeIdx);
                player.SkillsJson = JsonSerializer.Serialize(learnedSkills);
                actionLog += $"👾 **【かいじゅう】の呪い！** あなたは【{lostSkill.Name}】を忘れてしまった...！\n";
            }
        }

        // 2. クールダウン補正フック
        public static int AdjustCooldown(int baseCooldown, string? roomEffectId)
        {
            if (string.IsNullOrEmpty(roomEffectId)) return baseCooldown;

            if (roomEffectId == "tailwind") return 1;
            if (roomEffectId == "swamp") return baseCooldown + 1;
            if (roomEffectId == "muddy") return new Random().Next(2, 5); // 2~4

            return baseCooldown;
        }

        // 3. クールダウン追加（使用後判定など）
        public static int GetCooldownPenalty(int currentCD, string? roomEffectId)
        {
            if (roomEffectId == "frozen" && new Random().Next(100) < 30)
            {
                return 5;
            }
            return 0;
        }

        // 4. ターン開始時フック
        public static void ProcessTurnStartEffects(Player player, string? roomEffectId, ref string actionLog)
        {
            if (string.IsNullOrEmpty(roomEffectId)) return;

            // しゃくねつ (毎ターン5%ダメ)
            if (roomEffectId == "scorching")
            {
                var stats = player.GetTotalStats();
                int sDmg = (int)(stats.MaxHP * 0.05);
                player.CurrentHP = Math.Max(0, player.CurrentHP - sDmg);
                actionLog += $"🔥 **【しゃくねつ】！** ダメージを受けた (-{sDmg}dmg)\n";
            }
        }

        // 5. 命中率補正フック
        public static int AdjustHitChance(int hitChance, string? roomEffectId)
        {
            if (string.IsNullOrEmpty(roomEffectId)) return hitChance;

            if (roomEffectId == "vast") return hitChance - 30;
            if (roomEffectId == "prediction") return hitChance * 3;
            if (roomEffectId == "cramp") return 100; // 回避不能

            return hitChance;
        }

        // 6. アクション終了時（反動など）
        public static void ProcessActionEndEffects(Player player, bool isHit, string? roomEffectId, ref string actionLog)
        {
            if (string.IsNullOrEmpty(roomEffectId)) return;

            if (!isHit && roomEffectId == "gear")
            {
                player.CurrentHP = Math.Max(0, player.CurrentHP - 20);
                actionLog += "⚙️ **【はぐるま】の反動！** 攻撃を外し、自身の機構にダメージを負った (-20dmg)\n";
            }
            if (isHit && roomEffectId == "icicle")
            {
                player.CurrentHP = Math.Max(0, player.CurrentHP - 10);
                actionLog += "❄️ **【つらら】の反動！** 氷の棘が自身にも突き刺さる (-10dmg)\n";
            }
        }

        // 7. ダメージ補正フック
        public static int AdjustDamage(int damage, string? roomEffectId)
        {
            if (roomEffectId == "puppet") return damage / 2; // かいらい (ダメージ半減)
            return damage;
        }

        // 8. ターン終了時フック
        public static void ProcessTurnEndEffects(Player player, List<StatusEffect> playerEffects, List<Skill> learnedSkills, string? roomEffectId, ref string actionLog)
        {
            if (string.IsNullOrEmpty(roomEffectId)) return;

            // 石化進行
            if (roomEffectId == "sculpture" || roomEffectId == "curse_gear")
            {
                var petrify = playerEffects.FirstOrDefault(e => e.Type == EffectType.Petrification);
                int gain = (roomEffectId == "sculpture") ? 10 : 5;
                if (petrify == null)
                {
                    petrify = new StatusEffect { Name = "石化", Type = EffectType.Petrification, Value = gain, Duration = 99 };
                    playerEffects.Add(petrify);
                }
                else
                {
                    petrify.Value += gain;
                }
                actionLog += $"🗿 **石化が進行中...** ({petrify.Value}%)\n";
                if (petrify.Value >= 100)
                {
                    player.CurrentHP = 0;
                    actionLog += "⌛ **完全石化！** あなたの意識は永久に闇へと沈んだ。\n";
                }
            }

            // せんりつ (合計CT判定)
            if (roomEffectId == "dread_beat")
            {
                int totalCD = learnedSkills.Sum(s => s.CurrentCooldown);
                if (totalCD > 50)
                {
                    player.CurrentHP = 0;
                    actionLog += "💓 **【せんりつ】！** 膨れ上がった魔力の反動に心臓が耐えきれなかった！\n";
                }
            }
        }

        // 9. 非戦闘時（部屋移動時等）の特殊効果
        public static void ApplyNonBattleRoomEffects(Player player, string? roomEffectId)
        {
            if (roomEffectId == "traces")
            {
                player.RoomsExplored += 3;
            }
        }
    }
}
