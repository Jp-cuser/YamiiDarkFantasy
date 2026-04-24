using System.ComponentModel.DataAnnotations;

namespace YamiiDarkFantasy.Models
{
    public class GameSession
    {
        [Key]
        public ulong UserId { get; set; } // PlayerのUserIdと同じ（1:1）
        
        public bool IsInBattle { get; set; } = false;
        
        // 現在戦っている敵の情報（JSONで保存、または外部キー。今回はシンプルにJSON等でシリアライズ可能にする予定）
        public string? CurrentEnemyJson { get; set; }

        public string RoomOptionsJson { get; set; } = "[]"; // 左右の部屋の選択肢情報を保存
        
        // プレイヤーに割り当てられた現在進行中の専用チャンネルID
        public ulong? ChannelId { get; set; }
        
        // 10分無操作タイムアウト判定用
        public System.DateTime LastActiveTime { get; set; } = System.DateTime.UtcNow;

        // ドロップ等の保留中アイテム情報 ('skill' あるいは 'equipment')
        public string? PendingItemJson { get; set; }
        public bool IsPreemptiveTriggered { get; set; } = false; // 先制スキル発動済みフラグ
        public string? PendingItemType { get; set; }

        public string StageId { get; set; } = "apprentice_cave";
        public string? CurrentRoomEffectId { get; set; }
        public string? FloorThemeId { get; set; }
    }
}
