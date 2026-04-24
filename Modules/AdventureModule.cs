using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using YamiiDarkFantasy.Services;
using YamiiDarkFantasy.Constants;
using YamiiDarkFantasy.Formatters;
using YamiiDarkFantasy.Models;

namespace YamiiDarkFantasy.Modules
{
    public class AdventureModule : InteractionModuleBase<SocketInteractionContext>
    {
        // ユーザーレベルでの処理の競合（連打や複数ボタンの同時押し）を防止するための静的ロック
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, bool> _userLocks = new();

        // 諦めるの確認画面が出ている間、他の操作をブロックするためのフラグ
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, bool> _givingUpUsers = new();

        private readonly GameService _gameService;
        private readonly BattleService _battleService;
        private readonly SessionTimeoutService _timeoutService;

        public AdventureModule(GameService gameService, BattleService battleService, SessionTimeoutService timeoutService)
        {
            _gameService = gameService;
            _battleService = battleService;
            _timeoutService = timeoutService;
        }

        // タイムアウトなどの外部処理からユーザーの静的ロックを強制解除するためのメソッド
        public static void ClearUserLocks(ulong userId)
        {
            _userLocks.TryRemove(userId, out _);
            _givingUpUsers.TryRemove(userId, out _);
        }

        // ユーザーの一時的なステージ選択を保持
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, string> _tempStageSelections = new();

        [SlashCommand("start", "冒険を開始します")]
        public async Task StartGameCommand()
        {
            await ShowStageSelectionAsync();
        }

        [ComponentInteraction("start_game")]
        public async Task HandleStartGameButton()
        {
            await ShowStageSelectionAsync();
        }

        private async Task ShowStageSelectionAsync()
        {
            // 連打防止ロック
            if (!_userLocks.TryAdd(Context.User.Id, true)) return;

            try
            {
                var existingChannelId = await _gameService.GetActiveSessionChannelIdAsync(Context.User.Id);
                // すでにアクティブな場合は弾く
                if (existingChannelId.HasValue)
                {
                    var existingChannel = Context.Guild?.GetTextChannel(existingChannelId.Value);
                    if (existingChannel != null)
                    {
                        await RespondAsync($"⚠️ **すでに進行中の冒険があります！**\n別のテキストチャンネル (<#{existingChannelId.Value}>) で冒険が進行中です。そちらで進めるか、「諦める」ボタンを押してから再度やり直してください。", ephemeral: true);
                        return;
                    }
                    else
                    {
                        await _gameService.ClearChannelIdAsync(Context.User.Id);
                    }
                }

                var stageMenu = new SelectMenuBuilder()
                    .WithCustomId("select_stage")
                    .WithPlaceholder("舞台を選択してください...");
                
                foreach (var stg in StageDictionary.AvailableStages)
                {
                    stageMenu.AddOption(stg.Name, stg.Id, stg.Description);
                }

                var builder = new ComponentBuilder().WithSelectMenu(stageMenu);
                await RespondAsync("どの舞台で冒険を始めますか？", components: builder.Build(), ephemeral: true);
            }
            finally
            {
                _userLocks.TryRemove(Context.User.Id, out _);
            }
        }

        [ComponentInteraction("select_stage")]
        public async Task HandleStageSelection(string[] selectedStages)
        {
            string stageId = selectedStages.Length > 0 ? selectedStages[0] : "apprentice_cave";
            _tempStageSelections[Context.User.Id] = stageId;

            // 続けて職業選択へ
            var menu = new SelectMenuBuilder()
                .WithCustomId("select_job")
                .WithPlaceholder("職業を選択してください...")
                .AddOption("騎士", "knight", "高いHPと防御力を持ち、前線で耐え抜く堅牢な戦士。")
                .AddOption("聖職者", "cleric", "神聖な祈りで傷を癒やし、魔術で敵を討つ後衛職。")
                .AddOption("侍", "samurai", "洗練された技巧と高い攻撃力で一撃必殺を狙う剣客。")
                .AddOption("旅人", "traveler", "目立った強さはないが、すべてにおいて平均的で運が良い。")
                .AddOption("格闘家", "fighter", "己の肉体のみを頼りに戦う。高いHPと敏捷性を持つ。");

            var builder = new ComponentBuilder().WithSelectMenu(menu);
            var component = Context.Interaction as SocketMessageComponent;
            if (component != null)
            {
                await component.UpdateAsync(msg => 
                {
                    msg.Content = "どの職業で冒険に出ますか？";
                    msg.Components = builder.Build();
                });
            }
        }

        [ComponentInteraction("select_job")]
        public async Task HandleJobSelection(string[] selectedJobs)
        {
            string jobId = selectedJobs.Length > 0 ? selectedJobs[0] : "traveler";
            string stageId = _tempStageSelections.TryGetValue(Context.User.Id, out var stage) ? stage : "apprentice_cave";
            
            await StartGameLogicAsync(jobId, stageId);
        }

        private async Task StartGameLogicAsync(string jobId, string stageId)
        {
            // 新規開始時は「諦める」フラグを必ずクリアする
            _givingUpUsers.TryRemove(Context.User.Id, out _);

            // まず連打防止ロックを獲得 (すでに処理中なら何もせずreturn)
            if (!_userLocks.TryAdd(Context.User.Id, true))
            {
                return;
            }

            try
            {
                await DeferAsync(); // 処理に時間がかかる可能性を考慮してDefer
                
                // 元のUI（職業選択セレクト）はもう要らないので消去(または更新して消す)
                // このコンテキストは select_job の Interaction なので消去対象
                try { await DeleteOriginalResponseAsync(); } catch { }

                var resultMessage = await _gameService.StartNewGameAsync(Context.User.Id, jobId, stageId);
                
                if (resultMessage == null)
                {
                    await FollowupAsync("ゲームの開始に失敗しました。すでにゲーム中かもしれません。", ephemeral: true);
                    return;
                }

                var guild = Context.Guild;
                var currentChannel = Context.Channel as ITextChannel;
                if (guild != null)
                {
                    var newChannel = await guild.CreateTextChannelAsync($"冒険-{Context.User.Username}", prop => 
                    {
                        prop.CategoryId = currentChannel?.CategoryId;
                    });
                    
                    // 本人とBotだけが見れるように権限を設定
                    await newChannel.AddPermissionOverwriteAsync(Context.User, new OverwritePermissions(viewChannel: PermValue.Allow));
                    await newChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(viewChannel: PermValue.Deny));

                    // セッションにChannelIdを保存
                    await _gameService.SaveChannelIdAsync(Context.User.Id, newChannel.Id);

                    // 最初の選択肢を新しいチャンネルに表示
                    var (embed, components) = await _gameService.GetRoomSelectionMessageAsync(Context.User.Id);
                    await newChannel.SendMessageAsync(text: resultMessage, embed: embed, components: components);
                    
                    // URL形式とタグ形式の両方でリンクを案内
                    string channelUrl = $"https://discord.com/channels/{guild.Id}/{newChannel.Id}";
                    await FollowupAsync($"冒険の準備が整いました。\n👉 **テキストチャンネル:** <#{newChannel.Id}>\n👉 **直接リンク:** {channelUrl}", ephemeral: true);
                    
                    // パブリックにも通知を出す（他人に邪魔にならないようメンションはしない）
                    await Context.Channel.SendMessageAsync($"⚔️ {Context.User.Username} が 新たな冒険に出発しました！ (<#{newChannel.Id}>)", allowedMentions: AllowedMentions.None);
                }
                else
                {
                    // ダイレクトメッセージ等での実行の場合は従来通り
                    var (embed, components) = await _gameService.GetRoomSelectionMessageAsync(Context.User.Id);
                    await FollowupAsync(text: resultMessage, embed: embed, components: components);
                }
            }
            finally
            {
                // 処理が終わったらロックを確実に解除
                _userLocks.TryRemove(Context.User.Id, out _);
            }
        }

        [ComponentInteraction("room_*")]
        public async Task HandleRoomSelection(string roomId)
        {
            if (_givingUpUsers.ContainsKey(Context.User.Id))
            {
                await RespondAsync("⚠️ 先に「諦める」の確認（はい/いいえ）を完了させてください。", ephemeral: true);
                return;
            }

            // 排他制御
            if (!_userLocks.TryAdd(Context.User.Id, true)) return;

            try
            {
                // roomIdは一意のIDまたは"next"
                // まずボタンを即座に消去してACKを返す (これでタイムアウトも防げる)
                if (Context.Interaction is SocketMessageComponent component)
                {
                    await component.UpdateAsync(properties => 
                    {
                        properties.Components = new ComponentBuilder().Build();
                    });
                }
                await _gameService.UpdateLastActiveTimeAsync(Context.User.Id);

                var result = await _gameService.ProcessRoomSelectionAsync(Context.User.Id, roomId);
                
                if (result.embed == null)
                {
                    await FollowupAsync("エラー：現在のセッションが見つかりません。 `/start` でやり直してください。");
                    return;
                }

                // 新しい結果をチャンネルの下部へ新設メッセージとして送信する
                await Context.Channel.SendMessageAsync(text: result.text, embed: result.embed, components: result.components);
            }
            finally
            {
                _userLocks.TryRemove(Context.User.Id, out _);
            }
        }

        [ComponentInteraction("skill_*")]
        public async Task HandleSkillAction(string actionType)
        {
            if (_givingUpUsers.ContainsKey(Context.User.Id))
            {
                await RespondAsync("⚠️ 先に「諦める」の確認（はい/いいえ）を完了させてください。", ephemeral: true);
                return;
            }

            // 排他制御
            if (!_userLocks.TryAdd(Context.User.Id, true)) return;

            try
            {
                // まずボタンを即座に消去してACKを返す (これでタイムアウトも防げる)
                if (Context.Interaction is SocketMessageComponent component)
                {
                    await component.UpdateAsync(properties => 
                    {
                        properties.Components = new ComponentBuilder().Build();
                    });
                }
                await _gameService.UpdateLastActiveTimeAsync(Context.User.Id);

                var result = await _battleService.ProcessSkillActionAsync(Context.User.Id, actionType);

                if (result.embed == null)
                {
                    await FollowupAsync("エラー：スキル処理に失敗しました。");
                    return;
                }

                // 死亡などのゲームオーバー確認の場合、チャンネル削除があるかもしれないので一旦通常の処理
                await Context.Channel.SendMessageAsync(text: result.resultMsg, embed: result.embed, components: result.components);
                
                // 死亡判定（タイトルで判別）
                if (result.embed != null && result.embed.Title.Contains("死亡しました"))
                {
                    var userId = Context.User.Id;
                    var channelId = Context.Channel.Id;
                    var restClient = Context.Client.Rest;
                    
                    // 生きているうちにスコープのサービスを使ってクリア
                    await _gameService.ClearChannelIdAsync(userId);
                    
                    _ = Task.Run(async () => 
                    {
                        await Task.Delay(10000); // 10秒待機
                        
                        try 
                        {
                            var channelToDelete = await restClient.GetChannelAsync(channelId) as ITextChannel;
                            if (channelToDelete != null)
                            {
                                await channelToDelete.DeleteAsync(); 
                            }
                        } 
                        catch (System.Exception ex) 
                        { 
                            System.Console.WriteLine($"[Channel Delete Error] 死亡時のチャンネル削除に失敗しました: {ex.Message}"); 
                        }
                    });
                }
            }
            finally
            {
                _userLocks.TryRemove(Context.User.Id, out _);
            }
        }


        [ComponentInteraction("giveup")]
        public async Task HandleGiveup()
        {
            if (_givingUpUsers.ContainsKey(Context.User.Id)) return; // すでに出ているなら何もしない

            // 排他制御
            if (!_userLocks.TryAdd(Context.User.Id, true)) return;

            try
            {
                _givingUpUsers.TryAdd(Context.User.Id, true); // 確認画面表示中フラグをオン

                var (embed, components) = UIFormatter.BuildGiveupConfirm();
                    
                // 元のメッセージは消さずに「エフェメラル（自分だけに見えるメッセージ）」で確認を出す
                await RespondAsync(embed: embed, components: components, ephemeral: true);
            }
            finally
            {
                _userLocks.TryRemove(Context.User.Id, out _);
            }
        }

        [ComponentInteraction("discard_item")]
        public async Task HandleDiscardItem()
        {
            if (!_userLocks.TryAdd(Context.User.Id, true)) return;
            try
            {
                await DeferAsync();
                var session = await _gameService.GetSessionAsync(Context.User.Id);
                if (session != null)
                {
                    session.PendingItemJson = null;
                    session.PendingItemType = null;
                    await _gameService.SaveChangesAsync();
                }

                await ModifyOriginalResponseAsync(msg => 
                {
                    msg.Components = UIFormatter.BuildRoomNextButton();
                });
                await FollowupAsync(MessageConstants.DiscardedItem);
            }
            finally
            {
                _userLocks.TryRemove(Context.User.Id, out _);
            }
        }

        [ComponentInteraction("pickup_skill")]
        public async Task HandlePickupSkill()
        {
            if (!_userLocks.TryAdd(Context.User.Id, true)) return;
            try
            {
                await DeferAsync();
                var session = await _gameService.GetSessionAsync(Context.User.Id);
                var player = await _gameService.GetPlayerAsync(Context.User.Id);

                if (session == null || player == null || session.PendingItemType != "skill" || string.IsNullOrEmpty(session.PendingItemJson))
                {
                    await FollowupAsync(MessageConstants.ErrorPendingSkillNotFound, ephemeral: true);
                    return;
                }

                var newSkill = System.Text.Json.JsonSerializer.Deserialize<YamiiDarkFantasy.Models.Skill>(session.PendingItemJson);
                if (newSkill == null) { await FollowupAsync(MessageConstants.ErrorSkillParseFailed, ephemeral: true); return; }

                var skills = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<YamiiDarkFantasy.Models.Skill>>(player.SkillsJson) ?? new();
                
                var existing = skills.FirstOrDefault(s => s.Name == newSkill.Name);
                if (existing != null)
                {
                    existing.Level++;
                }
                else
                {
                    skills.Add(newSkill);
                }
                player.SkillsJson = System.Text.Json.JsonSerializer.Serialize(skills);

                session.PendingItemJson = null;
                session.PendingItemType = null;
                await _gameService.SaveChangesAsync();

                await ModifyOriginalResponseAsync(msg =>
                {
                    msg.Components = UIFormatter.BuildRoomNextButton();
                });
                await FollowupAsync(MessageConstants.SkillLearned(newSkill.Name));
            }
            finally
            {
                _userLocks.TryRemove(Context.User.Id, out _);
            }
        }

        [ComponentInteraction("pickup_equipment")]
        public async Task HandlePickupEquipment()
        {
            if (!_userLocks.TryAdd(Context.User.Id, true)) return;
            try
            {
                await DeferAsync();
                var session = await _gameService.GetSessionAsync(Context.User.Id);
                var player = await _gameService.GetPlayerAsync(Context.User.Id);

                if (session == null || player == null || session.PendingItemType != "equipment" || string.IsNullOrEmpty(session.PendingItemJson))
                {
                    await FollowupAsync(MessageConstants.ErrorPendingEquipNotFound, ephemeral: true);
                    return;
                }

                var newEquip = System.Text.Json.JsonSerializer.Deserialize<YamiiDarkFantasy.Models.Equipment>(session.PendingItemJson);
                if (newEquip == null) { await FollowupAsync(MessageConstants.ErrorEquipParseFailed, ephemeral: true); return; }

                var equips = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<YamiiDarkFantasy.Models.Equipment>>(player.EquipmentsJson) ?? new();
                equips.Add(newEquip);
                player.EquipmentsJson = System.Text.Json.JsonSerializer.Serialize(equips);

                session.PendingItemJson = null;
                session.PendingItemType = null;
                await _gameService.SaveChangesAsync();

                await ModifyOriginalResponseAsync(msg =>
                {
                    msg.Components = UIFormatter.BuildRoomNextButton();
                });
                await FollowupAsync(MessageConstants.EquipEquipped(newEquip.Name));
            }
            finally
            {
                _userLocks.TryRemove(Context.User.Id, out _);
            }
        }

        [ComponentInteraction("giveup_confirm")]
        public async Task HandleGiveupConfirm()
        {
            // 削除・ロック処理が絡むためここも明示的にロック
            // なお、10秒の待機はTask.Run内で行われるため、このロックは瞬時に解除される
            if (!_userLocks.TryAdd(Context.User.Id, true)) return;

            try
            {
                await DeferAsync();
                await DeleteOriginalResponseAsync();
                
                await Context.Channel.SendMessageAsync(MessageConstants.ChannelDeleteSoon);
                
                var userId = Context.User.Id;
                var channelId = Context.Channel.Id;
                var restClient = Context.Client.Rest;
                
                // 生きているうちにスコープのサービスを使ってクリア
                await _gameService.ClearChannelIdAsync(userId); 
                
                _ = Task.Run(async () => 
                {
                    await Task.Delay(10000); // 10秒待機
                    
                    try 
                    {
                        var channelToDelete = await restClient.GetChannelAsync(channelId) as ITextChannel;
                        if (channelToDelete != null)
                        {
                            await channelToDelete.DeleteAsync(); 
                        }
                    } 
                    catch (System.Exception ex) 
                    { 
                        System.Console.WriteLine($"[Channel Delete Error] 諦める時のチャンネル削除に失敗しました: {ex.Message}"); 
                    }
                });
            }
            finally
            {
                _userLocks.TryRemove(Context.User.Id, out _);
            }
        }

        [ComponentInteraction("giveup_cancel")]
        public async Task HandleGiveupCancel()
        {
            _givingUpUsers.TryRemove(Context.User.Id, out _); // 確認画面表示中フラグをオフ

            // 元のメッセージ（はい/いいえの確認エフェメラル）を削除する
            // エフェメラルは通常 DeleteOriginalResponseAsync() で消去可能
            try 
            {
                await DeferAsync();
                await DeleteOriginalResponseAsync();
                await FollowupAsync(MessageConstants.ResumeAdventure, ephemeral: true);
            }
            catch 
            {
                // 消去に失敗した場合（すでに消えている等）はそのまま何もしない
            }
        }




        [ComponentInteraction("swap_equipment")]
        public async Task HandleSwapEquipment()
        {
            if (!_userLocks.TryAdd(Context.User.Id, true)) return;
            try
            {
                await DeferAsync();
                var (success, msgText) = await _gameService.SwapPendingEquipmentAsync(Context.User.Id);

                await ModifyOriginalResponseAsync(msg => 
                {
                    msg.Components = UIFormatter.BuildRoomNextButton();
                });
                await FollowupAsync(msgText);
            }
            finally
            {
                _userLocks.TryRemove(Context.User.Id, out _);
            }
        }

        [ComponentInteraction("swap_skill_todo")]
        public async Task HandleSwapSkillTodo()
        {
            if (!_userLocks.TryAdd(Context.User.Id, true)) return;
            try
            {
                var session = await _gameService.GetSessionAsync(Context.User.Id);
                var player = await _gameService.GetPlayerAsync(Context.User.Id);

                if (session == null || player == null || session.PendingItemType != "skill" || string.IsNullOrEmpty(session.PendingItemJson))
                {
                    await RespondAsync(MessageConstants.ErrorPendingSkillNotFound, ephemeral: true);
                    return;
                }

                var skills = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<YamiiDarkFantasy.Models.Skill>>(player.SkillsJson) ?? new();
                if (skills.Count == 0)
                {
                    await RespondAsync(MessageConstants.ErrorInvalidSkillSelection, ephemeral: true);
                    return;
                }

                var (embed, components) = UIFormatter.BuildSkillSwapMenu(skills);
                await RespondAsync(embed: embed, components: components, ephemeral: true);
            }
            finally
            {
                _userLocks.TryRemove(Context.User.Id, out _);
            }
        }

        [ComponentInteraction("select_skill_to_swap")]
        public async Task HandleSelectSkillToSwap(string[] selectedIndices)
        {
            string skillIndexStr = selectedIndices.Length > 0 ? selectedIndices[0] : "";
            if (string.IsNullOrEmpty(skillIndexStr) || !int.TryParse(skillIndexStr, out int skillIndex)) return;

            if (!_userLocks.TryAdd(Context.User.Id, true)) return;
            try
            {
                await DeferAsync();
                var (success, msgText) = await _gameService.SwapPendingSkillAsync(Context.User.Id, skillIndex);

                try { await DeleteOriginalResponseAsync(); } catch { }

                var session = await _gameService.GetSessionAsync(Context.User.Id);
                
                // 元のメッセージ（ボタン付き）は追跡していないので放置か、ユーザーに押させないために何もしない
                // Followupで「さらに奥へ進む」を含んだ再送信を行うのが確実
                var components = UIFormatter.BuildRoomNextButton();
                await Context.Channel.SendMessageAsync($"🔄 {msgText}", components: components);
            }
            finally
            {
                _userLocks.TryRemove(Context.User.Id, out _);
            }
        }
    }
}
