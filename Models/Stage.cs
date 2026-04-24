using System.Collections.Generic;

namespace YamiiDarkFantasy.Models
{
    public class Stage
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImagePath { get; set; } = "Assets/title.png"; // ステージごとの背景・タイトル画像

        // このステージに出現する部屋のテンプレートリスト
        public List<RoomNode> AvailableRooms { get; set; } = new();

        // ボス出現セットのID
        public string BossEncounterSetId { get; set; } = "boss_set";

        // ボス部屋の基本設定
        public string BossRoomName { get; set; } = "最奥の間";
        public string BossRoomIcon { get; set; } = "💀";
        public string BossRoomDescription { get; set; } = "巨大な扉の先に、圧倒的な威圧感を放つ存在が待ち構えている...";
    }
}
