namespace YamiiDarkFantasy.Constants
{
    public static class MessageConstants
    {
        // --- ログイン・開始 ---
        public static string GameStart(string jobName) => $"新しい冒険が始まります。あなたは `{jobName}` です。";
        public static readonly string ProceedToNextFloor = "新たな階層へと足を踏み入れた...";
        public static readonly string ProceedToNextArea = "次のエリアへと進んだ...";

        // --- 探索・部屋 ---
        public static readonly string SkillSlotFullWarning = "\n\n⚠️ スキル枠がいっぱいです。入れ替える場合はどれかを忘れる必要があります。";
        public static string CurrentEquipmentInfo(string detailString) => $"\n\n🔄 **現在の装備:**\n{detailString}";
        public static string ErrorRoomNotFound = "エラー: その部屋は見つかりません";
        public static string ErrorSessionNotFoundStartOver = "エラー：現在のセッションが見つかりません。 `/start` でやり直してください。";

        // --- アイテム取得・装備・スキル ---
        public static string ErrorPendingEquipNotFound = "エラー：保留中の装備が見つかりません。";
        public static string ErrorEquipParseFailed = "エラー：装備データのパースに失敗しました。";
        public static string ErrorPendingSkillNotFound = "エラー：保留中のスキルが見つかりません。";
        public static string ErrorSkillParseFailed = "エラー：スキルデータのパースに失敗しました。";
        public static string ErrorInvalidSkillSelection = "エラー：無効なスキルが選択されました。";

        public static string EquipEquipped(string equipName) => $"【{equipName}】を新しく装備しました！";
        public static string SkillSwapped(string oldSkill, string newSkill) => $"【{oldSkill}】を忘れ、新たに【{newSkill}】を獲得しました！";
        public static readonly string DiscardedItem = "アイテムを諦めて捨てました。";
        public static string SkillLearned(string skillName) => $"📖 【{skillName}】を習得した！";

        // --- バトルログ関連 ---
        public static string PoisonDamage(string targetName, string effectName, int damage) => $"☠️ {targetName} は【{effectName}】のダメージを {damage} 受けた！\n";
        public static string ParalysisSkip(string targetName, string effectName) => $"⚡ {targetName} は【{effectName}】で体が痺れて動けない！\n";
        public static string BuffAppliedSelf(string targetName, string effectName) => $"✨ {targetName} に【{effectName}】の効果が付与された！\n";
        public static string DebuffAppliedTarget(string targetName, string effectName) => $"⏬ {targetName} に【{effectName}】の効果が付与された！\n";
        public static string AttackBlocked(string targetName) => $"🛡️ {targetName} は攻撃を防御した！ダメージを半減した！\n";
        public static string AttackEvaded(string targetName) => $"💨 {targetName} は攻撃を完全に回避した！\n";

        public static string SkillUsed(string actorName, string skillName) => $"🗡️ **{actorName}** は【{skillName}】を使った！\n";
        public static string DamageDealt(string targetName, int damage) => $"💥 {targetName} に {damage} のダメージ！\n";
        public static string HealHp(string targetName, int healAmount) => $"💚 {targetName} は {healAmount} HP回復した！\n";

        // --- バトルステータス・システム ---
        public static string ErrorBattleInfoNotFound = "戦闘情報が見つかりません。";
        public static string BattleTitle(int floor, string enemyName) => $"⚔ 階層 {floor}： {enemyName} との戦闘！";

        // --- ゲームオーバー・タイムアウト ---
        public static readonly string GameOverTimeout = "⚠️ **10分間操作がなかったため、冒険は自動的に終了（ゲームオーバー）となりました...**\nまもなくチャンネルは消滅します。";
        public static readonly string GameOverDeathTitle = "💀 死亡しました... 💀";
        public static string GameOverDeathDescription(int floor, int rooms) => $"無残にも敗れ去った...\n到達階層: {floor}\n踏破部屋数: {rooms}";
        public static readonly string GiveupConfirmationTitle = "⚠️ 警告：本当に諦めますか？";
        public static readonly string GiveupConfirmationDesc = "ここまでの進行状況を放棄し、完全に最初からやり直します。\n生きて帰れるとは限らない...本当に諦めますか？";
        public static readonly string ChannelDeleteSoon = "⚠️ **冒険を放棄しました...**\nすべての進行情報を破棄しました。まもなく（約10秒後）このチャンネルは消滅します...";
        public static readonly string ResumeAdventure = "冒険を続行します。";
    }
}
