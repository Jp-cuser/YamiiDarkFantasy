using System;
using System.Collections.Generic;

namespace YamiiDarkFantasy.Models
{
    public class RoomNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string RoomType { get; set; } = ""; // "event", "battle", "rest" など
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Description { get; set; } = "";

        // この部屋の効果パラメータ
        public RoomEffect Effect { get; set; } = new();

        // 次の部屋の候補
        public List<RoomNode> NextRooms { get; set; } = new();
    }

    public static class RoomDictionary
    {
        public static RoomNode GetRandomRoom(string stageId = "apprentice_cave")
        {
            var rand = new Random();
            var stage = StageDictionary.GetStage(stageId);
            var roomList = stage.AvailableRooms;

            int limit = roomList.Count;
            var baseRoom = roomList[rand.Next(limit)];
            
            return new RoomNode
            {
                RoomType = baseRoom.RoomType,
                Name = baseRoom.Name,
                Icon = baseRoom.Icon,
                Description = baseRoom.Description,
                Effect = baseRoom.Effect 
            };
        }

        // ボス部屋専用のテンプレート
        public static RoomNode GetBossRoom(string stageId = "apprentice_cave")
        {
            var stage = StageDictionary.GetStage(stageId);

            return new RoomNode
            {
                RoomType = "battle",
                Name = stage.BossRoomName,
                Icon = stage.BossRoomIcon,
                Description = stage.BossRoomDescription,
                Effect = new RoomEffect { EncounterSetId = stage.BossEncounterSetId }
            };
        }
    }
}
