using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YamiiDarkFantasy.Models;

namespace YamiiDarkFantasy.Services
{
    public class SessionTimeoutService
    {
        private readonly IServiceProvider _services;
        private readonly DiscordSocketClient _client;
        private readonly TimeSpan _timeoutDuration = TimeSpan.FromMinutes(10); // 10分
        private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, CancellationTokenSource> _userTimers = new();

        public SessionTimeoutService(IServiceProvider services, DiscordSocketClient client)
        {
            _services = services;
            _client = client;
        }

        public void Start()
        {
            _ = Task.Run(async () => 
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    
                    // DB上で稼働中の全セッションを取得し、それぞれにタイマーを再設定する
                    var activeSessions = await dbContext.GameSessions.Where(s => s.ChannelId != null).ToListAsync();
                    var now = DateTime.UtcNow;
                    
                    foreach (var s in activeSessions)
                    {
                        var elapsed = now - s.LastActiveTime;
                        var remaining = _timeoutDuration - elapsed;
                        
                        // 既にタイムアウト時刻を過ぎていれば、即座(1ms後)にタイムアウト処理を発火させる
                        if (remaining <= TimeSpan.Zero) 
                            remaining = TimeSpan.FromMilliseconds(1);
                        
                        RegisterOrResetTimer(s.UserId, s.ChannelId.Value, remaining);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TimeoutService Error] {ex.Message}");
                }
            });
        }

        public void RegisterOrResetTimer(ulong userId, ulong channelId, TimeSpan? duration = null)
        {
            // 古いタイマーがあればキャンセル
            if (_userTimers.TryGetValue(userId, out var oldCts))
            {
                try { oldCts.Cancel(); oldCts.Dispose(); } catch { }
            }

            var newCts = new CancellationTokenSource();
            _userTimers[userId] = newCts;

            var runDuration = duration ?? _timeoutDuration;

            // 個別の時限爆弾タスクを開始
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(runDuration, newCts.Token);
                    
                    // タイマー満了時、自身がキャンセルされていなければタイムアウト処理を実行
                    if (!newCts.Token.IsCancellationRequested)
                    {
                        await HandleTimeoutAsync(userId, channelId);
                        _userTimers.TryRemove(userId, out _);
                    }
                }
                catch (TaskCanceledException)
                {
                    // ボタン操作等によりリセットされた場合は正常終了
                }
            }, newCts.Token);
        }

        public void RemoveTimer(ulong userId)
        {
            if (_userTimers.TryRemove(userId, out var cts))
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
            }
        }

        private async Task HandleTimeoutAsync(ulong userId, ulong channelId)
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var session = await dbContext.GameSessions.FirstOrDefaultAsync(s => s.UserId == userId);
            // セッションが存在しないか、すでに別のチャンネルIDに変わっていれば無視
            if (session == null || session.ChannelId != channelId) return;

            Console.WriteLine($"[Timeout] User {session.UserId}'s session timed out (Channel-based).");

            var player = await dbContext.Players.FirstOrDefaultAsync(p => p.UserId == session.UserId);
            if (player != null)
            {
                player.CurrentHP = 0; // 死亡扱いとしてリセット
            }

            session.ChannelId = null; // セッションロック解除
            session.IsInBattle = false;
            session.CurrentEnemyJson = null;
            
            YamiiDarkFantasy.Modules.AdventureModule.ClearUserLocks(session.UserId);

            var channel = await _client.Rest.GetChannelAsync(channelId) as ITextChannel;
            if (channel != null)
            {
                try 
                { 
                    await channel.SendMessageAsync(YamiiDarkFantasy.Constants.MessageConstants.GameOverTimeout);
                    await Task.Delay(5000); // 5秒見せてから消す
                    await channel.DeleteAsync(); 
                } 
                catch { /* 権限がない、すでに消えている等の場合は無視 */ }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
