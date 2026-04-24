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
    public class GameService
    {
        private readonly AppDbContext _dbContext;
        private readonly BattleService _battleService;
        private readonly SessionTimeoutService _timeoutService;

        public GameService(AppDbContext dbContext, BattleService battleService, SessionTimeoutService timeoutService)
        {
            _dbContext = dbContext;
            _battleService = battleService;
            _timeoutService = timeoutService;
        }

        public async Task<string?> StartNewGameAsync(ulong userId, string jobId = "traveler", string stageId = "apprentice_cave")
        {
            var player = await _dbContext.Players.FirstOrDefaultAsync(p => p.UserId == userId);
            if (player == null)
            {
                player = JobDictionary.CreatePlayerWithJob(userId, jobId);
                _dbContext.Players.Add(player);
            }
            else
            {
                // キャラクターデータの初期化 (転生)
                var tp = JobDictionary.CreatePlayerWithJob(userId, jobId);
                player.Job = tp.Job;
                player.Level = 1;
                player.Exp = 0;
                player.MaxHP = tp.MaxHP;
                player.CurrentHP = tp.CurrentHP;
                player.STR = tp.STR;
                player.DEF = tp.DEF;
                player.INT = tp.INT;
                player.DEX = tp.DEX;
                player.AGI = tp.AGI;
                player.LUK = tp.LUK;
                player.Floor = 1;
                player.RoomsExplored = 0;
                player.SkillsJson = tp.SkillsJson;
                player.EquipmentsJson = tp.EquipmentsJson;
                player.StatusAilmentsJson = tp.StatusAilmentsJson;

                _dbContext.Players.Update(player);
            }

            var session = await _dbContext.GameSessions.FirstOrDefaultAsync(s => s.UserId == userId);
            if (session == null)
            {
                session = new GameSession { UserId = userId, StageId = stageId };
                _dbContext.GameSessions.Add(session);
            }
            else
            {
                session.IsInBattle = false;
                session.CurrentEnemyJson = null;
                session.ChannelId = null; // 開始時は一旦クリア
                session.StageId = stageId;
                _dbContext.GameSessions.Update(session);
                _timeoutService.RemoveTimer(userId);
            }

            // 初回開始時なので探索数は0
            player.RoomsExplored = 0;
            session.FloorThemeId = RoomEffectRegistry.FloorThemes.Keys.ElementAt(new Random().Next(RoomEffectRegistry.FloorThemes.Count));
            
            var initialRooms = GenerateRoomNodes(2, player.RoomsExplored, player.Floor, session.StageId);
            session.RoomOptionsJson = JsonSerializer.Serialize(initialRooms);

            await _dbContext.SaveChangesAsync();
            return MessageConstants.GameStart(player.Job);
        }

        public async Task SaveChannelIdAsync(ulong userId, ulong channelId)
        {
            var session = await _dbContext.GameSessions.FirstOrDefaultAsync(s => s.UserId == userId);
            if (session != null)
            {
                session.ChannelId = channelId;
                _timeoutService.RegisterOrResetTimer(userId, channelId);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<ulong?> GetActiveSessionChannelIdAsync(ulong userId)
        {
            var session = await _dbContext.GameSessions.FirstOrDefaultAsync(s => s.UserId == userId);
            if (session != null && session.ChannelId.HasValue)
            {
                return session.ChannelId.Value;
            }
            return null;
        }

        public async Task ClearChannelIdAsync(ulong userId)
        {
            var session = await _dbContext.GameSessions.FirstOrDefaultAsync(s => s.UserId == userId);
            if (session != null)
            {
                session.ChannelId = null;
                _timeoutService.RemoveTimer(userId);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task UpdateLastActiveTimeAsync(ulong userId)
        {
            var session = await _dbContext.GameSessions.FirstOrDefaultAsync(s => s.UserId == userId);
            if (session != null)
            {
                session.LastActiveTime = DateTime.UtcNow;
                if (session.ChannelId.HasValue)
                {
                    _timeoutService.RegisterOrResetTimer(userId, session.ChannelId.Value);
                }
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<GameSession?> GetSessionAsync(ulong userId)
        {
            return await _dbContext.GameSessions.FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<Player?> GetPlayerAsync(ulong userId)
        {
            return await _dbContext.Players.FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }

        public async Task<(bool success, string message)> SwapPendingEquipmentAsync(ulong userId)
        {
            var player = await _dbContext.Players.FirstOrDefaultAsync(p => p.UserId == userId);
            var session = await _dbContext.GameSessions.FirstOrDefaultAsync(s => s.UserId == userId);

            if (player == null || session == null || session.PendingItemType != "equipment" || string.IsNullOrEmpty(session.PendingItemJson))
                return (false, MessageConstants.ErrorPendingEquipNotFound);

            var newEquip = JsonSerializer.Deserialize<Equipment>(session.PendingItemJson);
            if (newEquip == null) return (false, MessageConstants.ErrorEquipParseFailed);

            List<Equipment> equips = JsonSerializer.Deserialize<List<Equipment>>(player.EquipmentsJson) ?? new List<Equipment>();
            var existing = equips.FirstOrDefault(e => e.EquipSlot == newEquip.EquipSlot);
            
            if (existing != null)
            {
                equips.Remove(existing);
            }
            equips.Add(newEquip);

            player.EquipmentsJson = JsonSerializer.Serialize(equips);
            
            // 保留中データをリセット
            session.PendingItemJson = null;
            session.PendingItemType = null;
            
            await _dbContext.SaveChangesAsync();

            return (true, MessageConstants.EquipEquipped(newEquip.Name));
        }

        public async Task<(bool success, string message)> SwapPendingSkillAsync(ulong userId, int skillIndexToForget)
        {
            var player = await _dbContext.Players.FirstOrDefaultAsync(p => p.UserId == userId);
            var session = await _dbContext.GameSessions.FirstOrDefaultAsync(s => s.UserId == userId);

            if (player == null || session == null || session.PendingItemType != "skill" || string.IsNullOrEmpty(session.PendingItemJson))
                return (false, MessageConstants.ErrorPendingSkillNotFound);

            var newSkill = JsonSerializer.Deserialize<Skill>(session.PendingItemJson);
            if (newSkill == null) return (false, MessageConstants.ErrorSkillParseFailed);

            List<Skill> skills = JsonSerializer.Deserialize<List<Skill>>(player.SkillsJson) ?? new List<Skill>();
            if (skillIndexToForget < 0 || skillIndexToForget >= skills.Count)
                return (false, MessageConstants.ErrorInvalidSkillSelection);

            var oldSkill = skills[skillIndexToForget];
            skills.RemoveAt(skillIndexToForget);
            skills.Add(newSkill);

            player.SkillsJson = JsonSerializer.Serialize(skills);
            
            // 保留中データをリセット
            session.PendingItemJson = null;
            session.PendingItemType = null;
            
            await _dbContext.SaveChangesAsync();

            return (true, MessageConstants.SkillSwapped(oldSkill.Name, newSkill.Name));
        }

        // タイトル画面用のEmbed, Components, そして画像のパスを返す
        public (Embed embed, MessageComponent components, string imagePath) GetTitleScreenAsync()
        {
            return UIFormatter.BuildTitleScreen();
        }

        // ツリー状に部屋を生成 (depth=深さ。2なら「今の選択肢＋次の選択肢」まで生成)
        private List<RoomNode> GenerateRoomNodes(int depth, int roomsExplored, int floor, string stageId)
        {
            if (depth <= 0) return new List<RoomNode>();

            var rand = new Random();
            var nodes = new List<RoomNode>();

            // ボス出現判定
            bool isBossSpawned = false;
            double bossChance = CalculateBossEncounterRate(roomsExplored); // 0.0 ~ 100.0
            
            if (rand.NextDouble() * 100.0 < bossChance)
            {
                isBossSpawned = true;
            }

            for (int i = 0; i < 2; i++)
            {
                RoomNode room;
                if (isBossSpawned && (roomsExplored >= 20 || i == 0))
                {
                    room = RoomDictionary.GetBossRoom(stageId);
                }
                else
                {
                    room = RoomDictionary.GetRandomRoom(stageId);
                    
                    // 特殊効果の抽選
                    if (rand.Next(100) < 40) // 40%の確率で特殊効果付きの部屋になる
                    {
                        var effectList = RoomEffectRegistry.Effects.Values.ToList();
                        
                        // 階層テーマによる補正
                        var session = _dbContext.GameSessions.Local.FirstOrDefault() ?? _dbContext.GameSessions.FirstOrDefault();
                        if (session?.FloorThemeId == "high_treasure_sculpture")
                        {
                            // 宝物庫や石像を高確率に（雑な実装だがFilterで調整）
                            var filtered = effectList.Where(e => e.Id == "treasure_vault" || e.Id == "sculpture").ToList();
                            if (filtered.Count > 0 && rand.Next(100) < 70) effectList = filtered;
                        }

                        var effectTemplate = effectList[rand.Next(effectList.Count)];
                        room.Effect.EffectId = effectTemplate.Id;
                        room.Name = effectTemplate.Name;
                        room.Description = effectTemplate.Description;
                        room.Icon = effectTemplate.Icon;

                        // 効果がある部屋は原則として戦闘にする
                        room.RoomType = "battle";

                        // 効果に応じた個別初期化
                        if (effectTemplate.Id == "rest") { room.Effect.HealHp = 30; }
                    }
                }
                
                room.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
                room.NextRooms = GenerateRoomNodes(depth - 1, roomsExplored + 1, floor, stageId);
                nodes.Add(room);
            }

            return nodes;
        }

        private double CalculateBossEncounterRate(int roomsExplored)
        {
            if (roomsExplored < 10) return 0.0;
            if (roomsExplored < 20)
            {
                // 10部屋で10%、20部屋で100% に線形に上げる
                double progress = (double)(roomsExplored - 10) / (20 - 10);
                return 10.0 + progress * (100.0 - 10.0);
            }
            return 100.0;
        }

        private RoomNode GenerateRandomRoom(string stageId = "apprentice_cave")
        {
            return RoomDictionary.GetRandomRoom(stageId);
        }

        public async Task<(Embed embed, MessageComponent components)> GetRoomSelectionMessageAsync(ulong userId)
        {
            var session = await _dbContext.GameSessions.FirstOrDefaultAsync(s => s.UserId == userId);
            var rooms = string.IsNullOrEmpty(session?.RoomOptionsJson) ? new List<RoomNode>() : JsonSerializer.Serialize(session.RoomOptionsJson) == "\"[]\"" ? new List<RoomNode>() : JsonSerializer.Deserialize<List<RoomNode>>(session.RoomOptionsJson);

            if (rooms == null || rooms.Count == 0)
            {
                // セーフティネット
                var player = await _dbContext.Players.FirstOrDefaultAsync(p => p.UserId == userId);
                rooms = GenerateRoomNodes(2, player?.RoomsExplored ?? 0, player?.Floor ?? 1, session?.StageId ?? "apprentice_cave");
                if (session != null)
                {
                    session.RoomOptionsJson = JsonSerializer.Serialize(rooms);
                    await _dbContext.SaveChangesAsync();
                }
            }
            
            return UIFormatter.BuildRoomSelection(rooms, session?.FloorThemeId);
        }

        public async Task<(string text, Embed? embed, MessageComponent? components)> ProcessRoomSelectionAsync(ulong userId, string roomId)
        {
            var player = await _dbContext.Players.FirstOrDefaultAsync(p => p.UserId == userId);
            var session = await _dbContext.GameSessions.FirstOrDefaultAsync(s => s.UserId == userId);

            if (player == null || session == null) return ("エラーが発生しました", null, null);

            var rooms = JsonSerializer.Deserialize<List<RoomNode>>(session.RoomOptionsJson);
            
            // "room_next" 等の直接コマンドの場合や、部屋ID検索
            RoomNode selectedRoom = null;
            if (roomId == "next")
            {
                // 次の部屋へ進む
                if (!string.IsNullOrEmpty(session.RoomOptionsJson) && session.RoomOptionsJson != "[]")
                {
                    player.RoomsExplored += 1;
                }
                
                await _dbContext.SaveChangesAsync();

                var nextMsg = await GetRoomSelectionMessageAsync(userId);
                string message = (player.RoomsExplored == 0 || string.IsNullOrEmpty(session.RoomOptionsJson)) 
                    ? MessageConstants.ProceedToNextFloor 
                    : MessageConstants.ProceedToNextArea;
                
                return (message, nextMsg.embed, nextMsg.components);
            }
            else
            {
                selectedRoom = rooms?.FirstOrDefault(r => r.Id == roomId);
                if (selectedRoom == null)
                    return (MessageConstants.ErrorRoomNotFound, null, null);

                // 選択肢の更新
                var nextOptions = selectedRoom.NextRooms;
                foreach (var nr in nextOptions)
                {
                    if (nr.NextRooms == null || nr.NextRooms.Count == 0)
                        nr.NextRooms = GenerateRoomNodes(1, player.RoomsExplored + 1, player.Floor, session.StageId);
                }
                session.RoomOptionsJson = JsonSerializer.Serialize(nextOptions);
            }

            // --- 扉・ランダム抽選ギミック ---
            string? effectId = selectedRoom.Effect.EffectId;
            string doorRevealNotice = "";

            if (effectId == "door")
            {
                // 扉を開けたとき、ランダムな効果（扉以外）を決定する
                var allEffects = RoomEffectRegistry.Effects.Values.Where(e => e.Id != "door").ToList();
                var revealed = allEffects[new Random().Next(allEffects.Count)];
                effectId = revealed.Id;
                doorRevealNotice = $"🚪 **扉を開けた！** その先には【{revealed.Name}】の空間が広がっていた...\n";
            }

            // 部屋効果をセッションに保存
            session.CurrentRoomEffectId = effectId;

            // 即時メリットの適用 (休息、経験値、ステータス、距離短縮など)
            RoomEffectModifier.ApplyNonBattleRoomEffects(player, session.CurrentRoomEffectId);

            if (selectedRoom.Effect.ClearStatusAilments) player.StatusAilmentsJson = "[]";
            if (selectedRoom.Effect.FullHeal) player.CurrentHP = player.MaxHP;
            else if (selectedRoom.Effect.HealHp > 0) player.CurrentHP = Math.Min(player.CurrentHP + selectedRoom.Effect.HealHp, player.MaxHP);
            if (selectedRoom.Effect.GainExp > 0) player.Exp += selectedRoom.Effect.GainExp;
            if (selectedRoom.Effect.GainAttack > 0) player.STR += selectedRoom.Effect.GainAttack;

            // イベントドロップが存在する場合 (スキル or 装備)
            bool hasEventDrop = !string.IsNullOrEmpty(selectedRoom.Effect.DropType);
            string extraRewardMsg = "";

            if (hasEventDrop && (selectedRoom.RoomType == "event" || selectedRoom.RoomType == "rest"))
            {
                // 非戦闘部屋としての報酬処理 (戦闘後のドロップとは別枠)
                if (selectedRoom.Effect.DropType == "skill")
                {
                    var newSkill = Skill.GenerateRandomSkill(player.Floor);
                    extraRewardMsg = $"{selectedRoom.Effect.ResultMessage}\n\n📖 **発見したスキル:**\n{newSkill.ToDetailString()}";
                    session.PendingItemType = "skill";
                    session.PendingItemJson = JsonSerializer.Serialize(newSkill);
                }
                else if (selectedRoom.Effect.DropType == "equipment")
                {
                    var newEquip = Equipment.GenerateRandomEquipment(player.Floor, player.LUK);
                    extraRewardMsg = $"{selectedRoom.Effect.ResultMessage}\n\n📦 **発見した装備:**\n{newEquip.ToDetailString()}";
                    session.PendingItemType = "equipment";
                    session.PendingItemJson = JsonSerializer.Serialize(newEquip);
                }
            }

            if (selectedRoom.RoomType == "battle")
            {
                Enemy enemy = null;
                string effectNotice = doorRevealNotice;

                if (session.CurrentRoomEffectId == "red_remains")
                {
                    var candidates = await _dbContext.Corpses.Where(c => c.StageId == session.StageId && c.Floor <= player.Floor).ToListAsync();
                    if (candidates.Any())
                    {
                        var corpse = candidates[new Random().Next(candidates.Count)];
                        var skills = JsonSerializer.Deserialize<List<Skill>>(corpse.SkillsJson) ?? new();
                        enemy = new Enemy
                        {
                            EnemyId = $"corpse_{corpse.Id}",
                            Name = $"{corpse.Name} (遺志)",
                            Description = $"かつて第{corpse.Floor}階層で果てた{corpse.Job}の亡骸。生前の執念が襲いかかる。",
                            MaxHP = (int)(corpse.MaxHP * 0.8),
                            CurrentHP = (int)(corpse.MaxHP * 0.8),
                            STR = corpse.STR,
                            DEF = corpse.DEF,
                            INT = corpse.INT,
                            AGI = corpse.AGI,
                            ExpReward = corpse.Level * 15,
                            IsBoss = false,
                            EnemySkills = skills.Take(3).ToList()
                        };
                        effectNotice += "☠️ **【亡骸】の影響:** 過去に倒れたプレイヤーの亡骸が襲いくる！\n";
                    }
                }
                else if (session.CurrentRoomEffectId == "treasure_vault")
                {
                    enemy = EnemyDictionary.GetEnemy("dragon_whelp", player.Floor + 2);
                    effectNotice += "💎 **【宝物庫】の影響:** 秘宝を守る強大な番人が現れた！\n";
                }

                if (enemy == null)
                {
                    if (!string.IsNullOrEmpty(selectedRoom.Effect.EncounterSetId))
                        enemy = EnemyDictionary.GetRandomEnemyFromSet(selectedRoom.Effect.EncounterSetId, player.Floor);
                    else
                        enemy = EnemyDictionary.GetEnemy(string.IsNullOrEmpty(selectedRoom.Effect.EnemyId) ? "wild_dog" : selectedRoom.Effect.EnemyId, player.Floor);
                }

                if (!string.IsNullOrEmpty(session.CurrentRoomEffectId))
                {
                    var template = RoomEffectRegistry.Effects[session.CurrentRoomEffectId];
                    if (!effectNotice.Contains(template.Name))
                        effectNotice += $"{template.Icon} **【{template.Name}】の影響下:** {template.Description}\n";
                }

                session.IsInBattle = true;
                session.IsPreemptiveTriggered = false; // 先制スキル発動フラグをリセット
                session.CurrentEnemyJson = JsonSerializer.Serialize(enemy);
                await _dbContext.SaveChangesAsync();

                var status = await _battleService.GetBattleStatusMessageAsync(userId, $"{effectNotice}\n{selectedRoom.Icon} **{enemy.Name}** が現れた！\n*{enemy.Description}*");
                return ("", status.embed, status.components);
            }
            else
            {
                // 非戦闘部屋 (event, rest)
                await _dbContext.SaveChangesAsync();
                List<Skill> cSkills = JsonSerializer.Deserialize<List<Skill>>(player.SkillsJson) ?? new();
                List<Equipment> cEqs = JsonSerializer.Deserialize<List<Equipment>>(player.EquipmentsJson) ?? new();
                
                var (resEmbed, resCmp) = UIFormatter.BuildRewardScreen(
                    $"{selectedRoom.Icon} {selectedRoom.Name}", 
                    selectedRoom.Description, 
                    string.IsNullOrEmpty(extraRewardMsg) ? selectedRoom.Effect.ResultMessage : extraRewardMsg, 
                    selectedRoom.RoomType == "rest" ? Color.Blue : Color.LightGrey, 
                    session.PendingItemType, 
                    cSkills, 
                    cEqs
                );
                return ("", resEmbed, resCmp);
            }
        }
    }
}
