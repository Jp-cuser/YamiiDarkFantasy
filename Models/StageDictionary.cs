using System.Collections.Generic;
using System.Linq;

namespace YamiiDarkFantasy.Models
{
    public static class StageDictionary
    {
        public static List<Stage> AvailableStages = new List<Stage>
        {
            new Stage
            {
                Id = "apprentice_cave",
                Name = "見習いの洞窟",
                Description = "初心者向けのダンジョン。比較的安全な道が続くが、奥には強力な主が潜んでいる。",
                ImagePath = "Assets/title.png",
                BossEncounterSetId = "boss_set",
                AvailableRooms = new List<RoomNode>
                {
                    // イベント系
                    new RoomNode { 
                        RoomType = "event", Name = "死体のある道", Icon = "💀", Description = "ボロボロの死体が転がっている...",
                        Effect = new RoomEffect { DropType = "skill", ResultMessage = "遺体から技術書を見つけた！" }
                    },
                    new RoomNode { 
                        RoomType = "event", Name = "古びた宝箱", Icon = "📦", Description = "誰かが置いていったと思われる宝箱がある。",
                        Effect = new RoomEffect { DropType = "equipment", ResultMessage = "宝箱を開けた！" }
                    },
                    
                    // バトル系
                    new RoomNode { 
                        RoomType = "battle", Name = "獣の唸り声", Icon = "🐺", Description = "暗がりから何らかの野獣の鳴き声が聞こえる。",
                        Effect = new RoomEffect { EncounterSetId = "beast_set" }
                    },
                    new RoomNode { 
                        RoomType = "battle", Name = "不気味な影", Icon = "👻", Description = "恨みを持った怨霊やアンデッドが彷徨っている。",
                        Effect = new RoomEffect { EncounterSetId = "undead_set" }
                    },
                    
                    // 休憩系
                    new RoomNode { 
                        RoomType = "rest", Name = "静かな泉", Icon = "⛲", Description = "清らかな水がコンコンと湧き出ている。",
                        Effect = new RoomEffect { ClearStatusAilments = true, ResultMessage = "冷たい水で清められた...！ 全ての状態異常が回復した！" }
                    },
                    new RoomNode { 
                        RoomType = "rest", Name = "焚き火の跡", Icon = "🔥", Description = "まだ温かい焚き火の跡だ。少しばかり休めるだろう。",
                        Effect = new RoomEffect { FullHeal = true, ResultMessage = "ほのかな暖かさに安心した... HPが完全に回復した！" }
                    }
                }
            }
        };

        public static Stage GetStage(string stageId)
        {
            return AvailableStages.FirstOrDefault(s => s.Id == stageId) ?? AvailableStages.First();
        }
    }
}
