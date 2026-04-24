using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YamiiDarkFantasy.Models;
using YamiiDarkFantasy.Services;

namespace YamiiDarkFantasy
{
    public class Program
    {
        private DiscordSocketClient _client;
        private InteractionService _commands;
        private IServiceProvider _services;

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            // .envファイルから環境変数を読み込む
            DotNetEnv.Env.Load();
            
            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("エラー: .env に DISCORD_TOKEN が設定されていません。");
                return;
            }
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers,
                AlwaysDownloadUsers = true
            };

            _client = new DiscordSocketClient(config);

            // DIコンテナの設定
            _services = ConfigureServices();

            _commands = new InteractionService(_client.Rest);

            _client.Log += Log;
            _commands.Log += Log;

            // クライアントの準備完了時
            _client.Ready += async () =>
            {
                // DBのセットアップ
                using (var scope = _services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await db.Database.EnsureCreatedAsync();

                    try
                    {
                        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database, "ALTER TABLE GameSessions ADD COLUMN StageId TEXT DEFAULT 'apprentice_cave';");
                        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database, "ALTER TABLE GameSessions ADD COLUMN CurrentRoomEffectId TEXT;");
                        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database, "ALTER TABLE GameSessions ADD COLUMN IsPreemptiveTriggered INTEGER DEFAULT 0;");
                        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database, "ALTER TABLE Players ADD COLUMN Faith INTEGER DEFAULT 5;");
                        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database, "ALTER TABLE Players ADD COLUMN Barrier INTEGER DEFAULT 0;");
                        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database, "ALTER TABLE Players ADD COLUMN Shield INTEGER DEFAULT 0;");
                        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database, "ALTER TABLE Players ADD COLUMN Reflect INTEGER DEFAULT 0;");

                        Console.WriteLine("DB Columns updated (Players & GameSessions).");
                    }
                    catch (System.Exception ex)
                    {
                        // 既にカラムが存在する場合などは無視。詳細ログが必要な場合は ex を使用。
                        Console.WriteLine($"DB Column check/add finished: {ex.Message}");
                    }

                    // 10分タイムアウト監視タイマーの起動
                    scope.ServiceProvider.GetRequiredService<SessionTimeoutService>().Start();
                }

                // 環境変数から開発用サーバー(Guild)のIDを取得
                var guildIdStr = Environment.GetEnvironmentVariable("GUILD_ID");
                if (ulong.TryParse(guildIdStr, out ulong guildId) && guildId > 0)
                {
                    // 開発時はGuildに登録することで、即時にスラッシュコマンドが反映されます
                    await _commands.RegisterCommandsToGuildAsync(guildId, true);
                    Console.WriteLine($"Commands registered to Guild: {guildId}");
                }
                else
                {
                    // 本番環境時はグローバルに登録（ただし反映に最大1時間かかります）
                    await _commands.RegisterCommandsGloballyAsync(true);
                    Console.WriteLine("Commands registered Globally.");
                }

                Console.WriteLine("Bot is connected, DB is ready, and commands are registered.");

                // 起動時にタイトル画面を送信
                var targetChannelStr = Environment.GetEnvironmentVariable("TARGET_CHANNEL_ID");
                if (ulong.TryParse(targetChannelStr, out ulong channelId))
                {
                    if (_client.GetChannel(channelId) is IMessageChannel channel)
                    {
                        using var scope = _services.CreateScope();
                        var gameService = scope.ServiceProvider.GetRequiredService<GameService>();
                        var title = gameService.GetTitleScreenAsync();
                        
                        try
                        {
                            await channel.SendFileAsync(title.imagePath, embed: title.embed, components: title.components);
                            Console.WriteLine("Sent title screen to the target channel.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to send title screen: {ex.Message}");
                        }
                    }
                }
            };

            // インタラクション生成時の処理
            _client.InteractionCreated += async interaction =>
            {
                var ctx = new SocketInteractionContext(_client, interaction);
                await _commands.ExecuteCommandAsync(ctx, _services);
            };

            // モジュールの読み込み
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<InteractionService>()
                .AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=game.db"))
                .AddScoped<GameService>()
                .AddScoped<BattleService>()
                .AddScoped<SkillService>()
                .AddSingleton<SessionTimeoutService>()
                .BuildServiceProvider();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}