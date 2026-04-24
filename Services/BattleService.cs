using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using YamiiDarkFantasy.Models;
using YamiiDarkFantasy.Constants;
using YamiiDarkFantasy.Formatters;

namespace YamiiDarkFantasy.Services
{
    public class BattleService
    {
        private readonly AppDbContext _dbContext;
        private readonly SkillService _skillService;

        public BattleService(AppDbContext dbContext, SkillService skillService)
        {
            _dbContext = dbContext;
            _skillService = skillService;
        }

        private async Task SaveCorpseAsync(Player player, GameSession session)
        {
            try
            {
                var stats = player.GetTotalStats();
                var corpse = new Corpse
                {
                    Name = $"彷徨う者 (元{player.Job})",
                    Job = player.Job,
                    Level = player.Level,
                    Floor = player.Floor,
                    StageId = session.StageId,
                    SkillsJson = player.SkillsJson,
                    EquipmentsJson = player.EquipmentsJson,
                    DiedAt = DateTime.UtcNow,
                    MaxHP = stats.MaxHP,
                    STR = stats.STR,
                    DEF = stats.DEF,
                    INT = stats.INT,
                    AGI = stats.AGI
                };
                _dbContext.Corpses.Add(corpse);
            }
            catch { /* ロギング */ }
        }

        public async Task<(Embed embed, MessageComponent components)> GetBattleStatusMessageAsync(ulong userId, string? additionalMessage = null)
        {
            var player = await _dbContext.Players.FirstOrDefaultAsync(p => p.UserId == userId);
            var session = await _dbContext.GameSessions.FirstOrDefaultAsync(p => p.UserId == userId);

            if (player == null || session == null || !session.IsInBattle || string.IsNullOrEmpty(session.CurrentEnemyJson))
            {
                return UIFormatter.BuildErrorScreen(MessageConstants.ErrorBattleInfoNotFound);
            }

            var enemy = JsonSerializer.Deserialize<Enemy>(session.CurrentEnemyJson);
            if (enemy == null) return UIFormatter.BuildErrorScreen(MessageConstants.ErrorBattleInfoNotFound);
            return UIFormatter.BuildBattleStatus(player, enemy, additionalMessage, session.CurrentRoomEffectId);
        }

        public async Task<(string resultMsg, Embed embed, MessageComponent components)> ProcessSkillActionAsync(ulong userId, string actionType)
        {
            var player = await _dbContext.Players.FirstOrDefaultAsync(p => p.UserId == userId);
            var session = await _dbContext.GameSessions.FirstOrDefaultAsync(p => p.UserId == userId);

            if (player == null || session == null || !session.IsInBattle || string.IsNullOrEmpty(session.CurrentEnemyJson))
                return ("エラー発生", null!, null!);

            var enemy = JsonSerializer.Deserialize<Enemy>(session.CurrentEnemyJson);
            if (enemy == null) return ("エラー発生", null!, null!);

            string actionLog = "";
            var learnedSkills = JsonSerializer.Deserialize<List<Skill>>(player.SkillsJson) ?? new();

            // --- 部屋効果: 戦闘開始時フック ---
            RoomEffectModifier.ApplyBattleStartEffects(player, enemy, session.CurrentRoomEffectId, learnedSkills, ref actionLog);

            var playerStats = player.GetTotalStats();
            var playerEquips = JsonSerializer.Deserialize<List<Equipment>>(player.EquipmentsJson) ?? new();
            var weapon = playerEquips.FirstOrDefault(e => e.EquipSlot == "Weapon");

            string? roomEffectId = session.CurrentRoomEffectId;
            Skill? usedSkill = null;

            if (actionType == "attack")
            {
                usedSkill = new Skill { Name = "基本攻撃", Power = 10, ScalingValue = "STR", SkillType = "Attack" };
            }
            else if (actionType == "weapon" && weapon != null && !string.IsNullOrEmpty(weapon.ProvidedSkillId))
            {
                usedSkill = Skill.GetSkill(weapon.ProvidedSkillId);
                if (usedSkill != null) usedSkill.Name = $"⚔️{usedSkill.Name}";
            }
            else
            {
                usedSkill = learnedSkills.FirstOrDefault(sk => sk.SkillId == actionType);
            }

            if (usedSkill == null) usedSkill = new Skill { Name = "基本攻撃", Power = 10, ScalingValue = "STR", SkillType = "Attack" };

            // クールダウン減少 (ターン消費スキルの場合のみ)
            if (!usedSkill.IsTurnFree)
            {
                if (roomEffectId != "silence")
                {
                    foreach (var sk in learnedSkills) if (sk.CurrentCooldown > 0) sk.CurrentCooldown--;
                }
                else if (learnedSkills.All(s => s.CurrentCooldown > 0 && s.Cooldown > 0))
                {
                    foreach (var sk in learnedSkills) sk.CurrentCooldown = 0;
                    actionLog += "🤫 **【せいじゃく】の効果！** クールダウンが零に還った。\n";
                }
            }

            // 使用スキルのクールダウン設定 (基本攻撃以外)
            if (actionType != "attack" && actionType != "weapon")
            {
                var skillInList = learnedSkills.FirstOrDefault(sk => sk.SkillId == actionType);
                if (skillInList != null)
                {
                    int finalCD = RoomEffectModifier.AdjustCooldown(skillInList.Cooldown, roomEffectId);
                    finalCD += RoomEffectModifier.GetCooldownPenalty(finalCD, roomEffectId);
                    skillInList.CurrentCooldown = finalCD;
                }
            }

            player.SkillsJson = JsonSerializer.Serialize(learnedSkills);

            var playerEffects = JsonSerializer.Deserialize<List<StatusEffect>>(player.StatusAilmentsJson) ?? new();
            var enemyEffects = enemy.ActiveEffects ?? new();
            bool playerSkipTurn = false;

            if (!usedSkill.IsTurnFree)
            {
                // --- 部屋効果: ターン開始時フック ---
                RoomEffectModifier.ProcessTurnStartEffects(player, roomEffectId, ref actionLog);

                int currentPlayerHp = player.CurrentHP;
                playerSkipTurn = _skillService.ProcessStatusEffects(playerEffects, ref currentPlayerHp, "あなた", ref actionLog);
                player.CurrentHP = currentPlayerHp;

                // --- 先制スキルの発動 ---
                actionLog += await _skillService.TriggerPreemptiveSkillsAsync(player, enemy, learnedSkills, session);
            }

            if (!playerSkipTurn && player.CurrentHP > 0 && enemy.CurrentHP > 0)
            {
                if (player.Barrier < usedSkill.BarrierCost)
                {
                    actionLog += $"⚠️ バリアが足りないため【{usedSkill.Name}】を発動できなかった！\n";
                }
                else
                {
                    player.Barrier -= usedSkill.BarrierCost;
                    if (usedSkill.BarrierCost > 0) actionLog += $"💠 バリアを {usedSkill.BarrierCost} 消費した！\n";

                    if (usedSkill.SkillType == "Heal")
                    {
                        int healValue = _skillService.CalculateSkillValue(usedSkill, playerStats, player, enemy);
                        player.CurrentHP = Math.Min(player.CurrentHP + healValue, playerStats.MaxHP);
                        actionLog += $"あなたは【{usedSkill.Name}】を使った！ HPが {healValue} 回復した。\n";
                        _skillService.ApplyLevelBonus(usedSkill, player, enemy, playerEffects, enemyEffects, ref actionLog);
                    }
                    else if (usedSkill.SkillType == "Buff")
                    {
                        actionLog += $"あなたは【{usedSkill.Name}】を使った！\n";
                        if (usedSkill.ScalingValue == "Shield")
                        {
                            int shieldGain = _skillService.CalculateSkillValue(usedSkill, playerStats, player, enemy);
                            player.Shield += shieldGain;
                            actionLog += $"🛡️ シールドを {shieldGain} 獲得した！\n";
                        }
                        _skillService.ApplySkillEffects(usedSkill, playerEffects, enemyEffects, "あなた", enemy.Name, ref actionLog);
                        _skillService.ApplyLevelBonus(usedSkill, player, enemy, playerEffects, enemyEffects, ref actionLog);
                    }
                    else
                    {
                        int damage = _skillService.CalculateSkillValue(usedSkill, playerStats, player, enemy);
                        int hitChance = 80 + (playerStats.AGI - enemy.AGI);
                        hitChance = RoomEffectModifier.AdjustHitChance(hitChance, roomEffectId);

                        if (new Random().Next(100) >= hitChance)
                        {
                            actionLog += $"あなたの【{usedSkill.Name}】が外れた！\n";
                            RoomEffectModifier.ProcessActionEndEffects(player, false, roomEffectId, ref actionLog);
                        }
                        else
                        {
                            damage = Math.Max(1, damage - enemy.GetTotalStats().DEF);
                            damage = RoomEffectModifier.AdjustDamage(damage, roomEffectId);

                            int eHp = enemy.CurrentHP, eBar = enemy.Barrier, eShi = enemy.Shield;
                            _skillService.ApplyDamageWithDefenses(damage, ref eBar, ref eShi, ref eHp, enemy.Name, ref actionLog);
                            enemy.CurrentHP = eHp; enemy.Barrier = eBar; enemy.Shield = eShi;

                            RoomEffectModifier.ProcessActionEndEffects(player, true, roomEffectId, ref actionLog);
                            _skillService.ApplySkillEffects(usedSkill, playerEffects, enemyEffects, "あなた", enemy.Name, ref actionLog);
                            _skillService.ApplyLevelBonus(usedSkill, player, enemy, playerEffects, enemyEffects, ref actionLog);
                        }
                    }
                }
            }

            if (enemy.CurrentHP <= 0)
            {
                player.Exp += enemy.ExpReward;
                actionLog += $"\n🎉 勝利！ 経験値 {enemy.ExpReward} 獲得。";
                actionLog += player.CheckAndApplyLevelUp();

                if (enemy.IsBoss)
                {
                    player.Floor++; player.RoomsExplored = 0; player.CurrentHP = player.MaxHP;
                    session.RoomOptionsJson = "[]";
                    var nextTheme = RoomEffectRegistry.FloorThemes.Keys.ElementAt(new Random().Next(RoomEffectRegistry.FloorThemes.Count));
                    session.FloorThemeId = nextTheme;
                    actionLog += $"\n👑 撃破！ 次は『{RoomEffectRegistry.FloorThemes[nextTheme].Name}』の階層だ。";
                }

                if (enemy.IsBoss || new Random().Next(100) < 40)
                {
                    var eq = Equipment.GenerateRandomEquipment(player.Floor, player.LUK, enemy.IsBoss ? 5 : 1);
                    session.PendingItemType = "equipment";
                    session.PendingItemJson = JsonSerializer.Serialize(eq);
                    actionLog += $"\n📦 アイテムドロップ: {eq.Name}";
                }
                session.IsInBattle = false; session.CurrentEnemyJson = null;
                player.StatusAilmentsJson = JsonSerializer.Serialize(playerEffects);
                await _dbContext.SaveChangesAsync();

                var (winEmbed, winCmp) = UIFormatter.BuildBattleVictoryScreen(actionLog);
                return ("", winEmbed, winCmp);
            }

            if (player.CurrentHP > 0 && enemy.CurrentHP > 0)
            {
                if (usedSkill.IsTurnFree) actionLog += $"\n⚡ **【{usedSkill.Name}】の追加行動！** (ターンを消費しなかった)\n";
                else
                {
                    int currentEnemyHp = enemy.CurrentHP;
                    bool enemySkipTurn = _skillService.ProcessStatusEffects(enemyEffects, ref currentEnemyHp, enemy.Name, ref actionLog);
                    enemy.CurrentHP = currentEnemyHp;

                    if (!enemySkipTurn && enemy.CurrentHP > 0)
                    {
                        var enemyStats = enemy.GetTotalStats();
                        var enemyAction = enemy.EnemySkills.Count > 0 ? enemy.EnemySkills[new Random().Next(enemy.EnemySkills.Count)] : new Skill { Name = "攻撃", Power = 10, ScalingValue = "STR", SkillType = "Attack" };
                        
                        if (new Random().Next(100) < (80 + enemyStats.AGI - playerStats.AGI))
                        {
                            // 敵のステータスを CalculateSkillValue が求める Tuple の形に合わせる (足りない MaxHP と LUK を補完)
                            var eStatsTuple = (MaxHP: enemy.MaxHP, STR: enemyStats.STR, DEF: enemyStats.DEF, INT: enemyStats.INT, DEX: enemyStats.DEX, AGI: enemyStats.AGI, LUK: 0, Faith: enemyStats.Faith);
                            
                            int eDmg = _skillService.CalculateSkillValue(enemyAction, eStatsTuple, new Player { Level = player.Level }, enemy);
                            eDmg = Math.Max(1, eDmg - playerStats.DEF);

                            int pHp = player.CurrentHP, pBar = player.Barrier, pShi = player.Shield;
                            _skillService.ApplyDamageWithDefenses(eDmg, ref pBar, ref pShi, ref pHp, "あなた", ref actionLog);
                            player.CurrentHP = pHp; player.Barrier = pBar; player.Shield = pShi;

                            _skillService.ApplySkillEffects(enemyAction, enemyEffects, playerEffects, enemy.Name, "あなた", ref actionLog);
                        }
                        else actionLog += $"{enemy.Name} の攻撃を回避した！\n";
                    }
                    RoomEffectModifier.ProcessTurnEndEffects(player, playerEffects, learnedSkills, roomEffectId, ref actionLog);
                    _skillService.DecreaseEffectDurations(playerEffects);
                    _skillService.DecreaseEffectDurations(enemyEffects);
                }
            }

            player.StatusAilmentsJson = JsonSerializer.Serialize(playerEffects);

            if (player.CurrentHP <= 0)
            {
                actionLog += "\n\n💀 YOU DIED...";
                await SaveCorpseAsync(player, session);
                session.IsInBattle = false; session.CurrentEnemyJson = null;
                player.Floor = 1; player.RoomsExplored = 0; player.CurrentHP = 0;
                await _dbContext.SaveChangesAsync();
                var (loseE, loseC) = UIFormatter.BuildDefeatScreen(player, enemy, actionLog);
                return ("", loseE, loseC);
            }

            session.CurrentEnemyJson = JsonSerializer.Serialize(enemy);
            await _dbContext.SaveChangesAsync();
            var status = await GetBattleStatusMessageAsync(userId, actionLog);
            return ("", status.embed, status.components);
        }
    }
}
