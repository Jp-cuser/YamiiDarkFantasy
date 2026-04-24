using System;

namespace YamiiDarkFantasy.Models
{
    public enum EffectType
    {
        Poison,      // 毎ターンダメージ
        Paralysis,   // 一定確率で行動失敗
        Buff,        // 対象ステータスの加算
        Debuff,      // 対象ステータスの減算
        Petrification, // 石化進行 (100で死亡)
        Weakness,    // 与ダメージが減る
        Blindness,   // 命中率が下がる
        Bleeding,    // 回復不可
        Contamination, // 状態異常になりやすくなる
        Shielded,    // 受けるダメージを軽減する
        Evasion,     // 回避率が上がる
        Resistance,  // 状態異常を無効化しやすくなる
        Regeneration // 毎ターンHP回復
    }

    public class StatusEffect
    {
        public string EffectId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "";
        public EffectType Type { get; set; }
        public int Duration { get; set; } = 3;       // 残りターン数
        public int Value { get; set; } = 0;          // 効果量（毒のダメージ量やステータス増減値）
        public string TargetStat { get; set; } = ""; // STR, DEF, AGI など (Buff/Debuff のみ使用)
        
        public StatusEffect Clone()
        {
            return new StatusEffect
            {
                Name = this.Name,
                Type = this.Type,
                Duration = this.Duration,
                Value = this.Value,
                TargetStat = this.TargetStat
            };
        }
    }
}
