namespace YamiiDarkFantasy.Models
{
    public class RoomEffect
    {
        public int HealHp { get; set; } = 0;
        public int GainExp { get; set; } = 0;
        public int GainAttack { get; set; } = 0;
        
        // ボス敵など、特定の1体を必ず出現させる場合のID
        public string EnemyId { get; set; } = "";
        
        // 通常戦闘など、複数の候補（セット）の中からランダムで1体を出現させる場合のID（こちらが優先される）
        public string EncounterSetId { get; set; } = "";
        
        // 追加でユーザーに表示するテキスト
        public string ResultMessage { get; set; } = "";

        // 特殊効果の識別子 (おいかぜ, かいらい 等)
        public string? EffectId { get; set; }

        // 特殊効果フラグ
        public string DropType { get; set; } = ""; // "skill", "equipment" など
        public bool FullHeal { get; set; } = false;
        public bool ClearStatusAilments { get; set; } = false;
    }
}
