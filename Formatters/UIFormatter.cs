using Discord;
using System.Collections.Generic;
using YamiiDarkFantasy.Models;
using YamiiDarkFantasy.Constants;
using System.Linq;

namespace YamiiDarkFantasy.Formatters
{
    public static class UIFormatter
    {
        public static (Embed embed, MessageComponent components) BuildErrorScreen(string errorMessage)
        {
            var embed = new EmbedBuilder()
                .WithTitle("エラー")
                .WithDescription(errorMessage)
                .WithColor(Color.Red)
                .Build();
            return (embed, BuildEmptyComponents());
        }

        // タイトル画面
        public static (Embed embed, MessageComponent components, string imagePath) BuildTitleScreen()
        {
            var embed = new EmbedBuilder()
                .WithTitle("🌘 YAMII DARK FANTASY 🌒")
                .WithDescription("暗く冷たい迷宮の入り口が、あなたを待っている。\n生還する保証はないが、それでも進むか？")
                .WithColor(Color.DarkBlue)
                .WithImageUrl("attachment://title.png") // アップロードされた画像を参照
                .Build();

            var components = new ComponentBuilder()
                .WithButton("冒険を始める", "start_game", ButtonStyle.Success)
                .Build();

            return (embed, components, "Assets/title.png");
        }

        // 部屋選択画面
        public static (Embed embed, MessageComponent components) BuildRoomSelection(List<RoomNode> roomOptions, string? floorThemeId = null)
        {
            bool isInvisible = floorThemeId == "invisible_rooms";
            var embed = new EmbedBuilder()
                .WithTitle(isInvisible ? "🌫️ 濃霧に包まれた道" : "🌲 行き先を選択してください")
                .WithDescription(isInvisible ? "霧が深く、この先の様子が全く分からない..." : "二手に分かれた道が目の前に広がっている。どちらへ進む？")
                .WithColor(isInvisible ? Color.LightGrey : Color.DarkGreen);

            var builder = new ComponentBuilder();
            
            // 部屋ごとに1フィールド（左右インライン）
            foreach (var room in roomOptions)
            {
                string nextPreview = "_暗くて見えない..._";
                if (room.NextRooms != null && room.NextRooms.Count > 0)
                {
                    nextPreview = isInvisible ? "▸ ❓ ???" : string.Join("\n", room.NextRooms.Select(nr => $"▸ {nr.Icon}{nr.Name}"));
                }

                string displayName = isInvisible ? "❓ 不明な部屋" : $"{room.Icon} {room.Name}";
                string fieldContent = isInvisible ? "中に入るまで何の部屋か分からない..." : $"{room.Description}\n\n🔭 **この先に見えるもの**\n{nextPreview}";
                
                embed.AddField(displayName, fieldContent, inline: true);
                builder.WithButton(displayName, $"room_{room.Id}", isInvisible ? ButtonStyle.Secondary : (room.RoomType == "battle" ? ButtonStyle.Danger : ButtonStyle.Secondary), row: 0);
            }
            
            // 下段に諦めるボタンを追加
            builder.WithButton("諦める", "giveup", ButtonStyle.Secondary, row: 1);

            return (embed.Build(), builder.Build());
        }

        // 個別の部屋踏破結果や戦闘勝利などの報酬・結果画面
        public static (Embed embed, MessageComponent components) BuildRewardScreen(string title, string description, string extraMessage, Color color, string pendingItemType, List<Skill> currentSkills, List<Equipment> currentEquips)
        {
            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription($"{description}\n\n**{extraMessage}**")
                .WithColor(color)
                .Build();

            var builder = new ComponentBuilder();

            if (!string.IsNullOrEmpty(pendingItemType))
            {
                if (pendingItemType == "skill") 
                {
                    if (currentSkills != null && currentSkills.Count >= 5)
                    {
                        builder.WithButton("入れ替え画面へ", "swap_skill_todo", ButtonStyle.Primary);
                        builder.WithButton("拾わない", "discard_item", ButtonStyle.Secondary);
                    }
                    else
                    {
                        builder.WithButton("習得する", "pickup_skill", ButtonStyle.Success);
                        builder.WithButton("拾わない", "discard_item", ButtonStyle.Secondary);
                    }
                }
                else if (pendingItemType == "equipment") 
                {
                    if (currentEquips != null && currentEquips.Any())
                    {
                        builder.WithButton("入れ替える", "swap_equipment", ButtonStyle.Primary);
                        builder.WithButton("拾わない", "discard_item", ButtonStyle.Secondary);
                    }
                    else
                    {
                        builder.WithButton("装備する", "pickup_equipment", ButtonStyle.Success);
                        builder.WithButton("拾わない", "discard_item", ButtonStyle.Secondary);
                    }
                }
            }
            else
            {
                // アイテムドロップ等がない場合は、次の部屋へ進むボタン
                builder.WithButton("次へ進む", "room_next", ButtonStyle.Secondary);
            }

            return (embed, builder.Build());
        }

        // バトルステータス画面
        public static (Embed embed, MessageComponent components) BuildBattleStatus(Player player, Enemy enemy, string? additionalLog, string? roomEffectId = null)
        {
            string enemyName = enemy.Name;
            string enemyHP = $"{enemy.CurrentHP}/{enemy.MaxHP}";
            
            if (roomEffectId == "darkness")
            {
                enemyName = "🌑 闇の住人";
                enemyHP = "?? / ??";
            }
            else if (roomEffectId == "mirage")
            {
                var rand = new System.Random();
                enemyName = "🎇 蜃気楼の影";
                enemyHP = $"{rand.Next(1, 999)} / {rand.Next(1, 999)}";
            }

            var embed = new EmbedBuilder()
                .WithTitle(MessageConstants.BattleTitle(player.Floor, enemyName))
                .WithColor(roomEffectId == "darkness" ? Color.DarkerGrey : Color.Red);

            if (!string.IsNullOrEmpty(additionalLog))
            {
                embed.WithDescription($"**【ログ】**\n{additionalLog}");
            }

            // 状態異常のテキスト化
            string GetStatusEffectsText(List<StatusEffect> effects)
            {
                if (roomEffectId == "darkness" || roomEffectId == "mirage") return "???";
                if (effects == null || effects.Count == 0) return "正常";
                return string.Join(", ", effects.Select(e => $"{e.Name}({e.Duration}T)"));
            }

            List<StatusEffect> playerStatus = string.IsNullOrEmpty(player.StatusAilmentsJson) ? new() : System.Text.Json.JsonSerializer.Deserialize<List<StatusEffect>>(player.StatusAilmentsJson) ?? new();
            List<StatusEffect> enemyStatus = enemy.ActiveEffects ?? new();

            // プレイヤーと敵のステータスを2カラムで表示
            string pDefStr = $"🛡️ バリア: {player.Barrier}  |  💎 シールド: {player.Shield}";
            embed.AddField(player.Job, $"💗 HP: {player.CurrentHP}/{player.MaxHP}\n{pDefStr}\n⚔ STR:{player.STR} 🛡 DEF:{player.DEF} 🙏 Faith:{player.Faith}\n状態: {GetStatusEffectsText(playerStatus)}", inline: true);

            string eDefStr = $"🛡️ バリア: {enemy.Barrier}  |  💎 シールド: {enemy.Shield}";
            if (roomEffectId == "darkness" || roomEffectId == "mirage") eDefStr = "🛡️ バリア: ??  |  💎 シールド: ??";
            embed.AddField(enemyName, $"💗 HP: {enemyHP}\n{eDefStr}\n状態: {GetStatusEffectsText(enemyStatus)}", inline: true);

            var builder = new ComponentBuilder();
            
            // プレイヤースキルから行動ボタンを生成 (習得スキル + 武器固有スキルのマージ版)
            List<Skill> availableSkills = player.GetAvailableSkills();
            bool hasReadySkill = availableSkills.Any(sk => sk.CurrentCooldown <= 0);
            
            int btnIndex = 0;
            if (availableSkills.Count == 0 || !hasReadySkill)
            {
                builder.WithButton("素手で殴る", "skill_attack", ButtonStyle.Primary, row: 0);
                btnIndex++;
            }

            foreach(var sk in availableSkills)
            {
                // 武器スキル（GetAvailableSkillsで⚔️が付与されたもの等）は特別なIDを振る
                string customId = sk.Name.StartsWith("⚔️") ? "skill_weapon" : $"skill_{sk.SkillId}";
                
                string btnLabel = sk.Name;
                var markers = new List<string>();
                if (sk.IsTurnFree) markers.Add("T");
                if (sk.BarrierCost > 0) markers.Add($"B{sk.BarrierCost}");
                if (sk.IsPreemptive) markers.Add("F");
                
                if (markers.Any()) btnLabel += $" [{string.Join("/", markers)}]";
                if (sk.CurrentCooldown > 0) btnLabel += $" (CD:{sk.CurrentCooldown})";

                builder.WithButton(btnLabel, customId, sk.CurrentCooldown > 0 ? ButtonStyle.Secondary : ButtonStyle.Primary, row: btnIndex / 4, disabled: sk.CurrentCooldown > 0);
                btnIndex++;
            }

            // 逃げる
            builder.WithButton("逃げる", "skill_flee", ButtonStyle.Secondary, row: 2);


            return (embed.Build(), builder.Build());
        }

        // ボス勝利時画面
        public static (Embed embed, MessageComponent components) BuildBossVictoryScreen(string resultMsgText)
        {
            var embed = new EmbedBuilder()
                .WithTitle("👑 階層の主を撃破した！")
                .WithDescription($"**【ログ】**\n{resultMsgText}")
                .WithColor(Color.Gold)
                .Build();

            var builder = new ComponentBuilder()
                .WithButton("次の階層へ進む", "room_next", ButtonStyle.Success);

            return (embed, builder.Build());
        }

        // 通常勝利時画面
        public static (Embed embed, MessageComponent components) BuildBattleVictoryScreen(string resultMsgText)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🏆 戦闘に勝利した！")
                .WithDescription($"**【ログ】**\n{resultMsgText}")
                .WithColor(Color.Blue)
                .Build();

            var builder = new ComponentBuilder()
                .WithButton("次へ進む", "room_next", ButtonStyle.Secondary);

            return (embed, builder.Build());
        }

        // 敗北画面
        public static (Embed embed, MessageComponent components) BuildDefeatScreen(Player player, Enemy enemy, string resultMsgText)
        {
            var embed = new EmbedBuilder()
                .WithTitle(MessageConstants.GameOverDeathTitle)
                .WithDescription($"**【最後に見た景色】**\n{resultMsgText}\n\n{MessageConstants.GameOverDeathDescription(player.Floor, player.RoomsExplored)}")
                .WithColor(Color.DarkRed)
                .Build();

            // 敗北時はボタンなし
            var builder = new ComponentBuilder();
            return (embed, builder.Build());
        }

        // 逃走成功
        public static (Embed embed, MessageComponent components) BuildFleeSuccessScreen(string resultMsgText)
        {
            var embed = new EmbedBuilder()
                .WithTitle("💨 逃走に成功した...")
                .WithDescription($"**【ログ】**\n{resultMsgText}")
                .WithColor(Color.LightGrey)
                .Build();

            var builder = new ComponentBuilder()
                .WithButton("次へ進む", "room_next", ButtonStyle.Secondary);

            return (embed, builder.Build());
        }

        public static (Embed embed, MessageComponent components) BuildSkillSwapMenu(List<Skill> currentSkills)
        {
            var menu = new SelectMenuBuilder()
                .WithCustomId("select_skill_to_swap")
                .WithPlaceholder("忘れさせる技術を選んでください...");

            int idx = 0;
            foreach (var sk in currentSkills)
            {
                menu.AddOption(sk.Name, idx.ToString(), sk.Description ?? "");
                idx++;
            }

            var builder = new ComponentBuilder().WithSelectMenu(menu);
            var embed = new EmbedBuilder().WithTitle("スキル入れ替え").WithDescription("どれを忘れて新しい技術（スキル）を獲得しますか？").WithColor(Color.Teal).Build();
            return (embed, builder.Build());
        }

        public static (Embed embed, MessageComponent components) BuildGiveupConfirm()
        {
            var embed = new EmbedBuilder()
                .WithTitle(MessageConstants.GiveupConfirmationTitle)
                .WithDescription(MessageConstants.GiveupConfirmationDesc)
                .WithColor(Color.Orange)
                .Build();
                
            var components = new ComponentBuilder()
                .WithButton("はい（死に戻る）", "giveup_confirm", ButtonStyle.Danger)
                .WithButton("いいえ", "giveup_cancel", ButtonStyle.Secondary)
                .Build();
            return (embed, components);
        }

        public static MessageComponent BuildRoomNextButton()
        {
            return new ComponentBuilder().WithButton("さらに奥へ進む", "room_next", ButtonStyle.Primary).Build();
        }

        public static MessageComponent BuildEmptyComponents()
        {
            return new ComponentBuilder().Build();
        }
    }
}
