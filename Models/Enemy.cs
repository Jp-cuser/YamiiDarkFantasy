namespace YamiiDarkFantasy.Models
{
    public class Enemy
    {
        public string EnemyId { get; set; } = "unknown";
        public string Name { get; set; } = "名もなき敵";
        public string Description { get; set; } = "正体不明の存在。";
        
        // ステータス
        public int MaxHP { get; set; } = 50;
        public int CurrentHP { get; set; } = 50;
        public int STR { get; set; } = 5;
        public int DEF { get; set; } = 2;
        public int INT { get; set; } = 2;
        public int DEX { get; set; } = 2;
        public int AGI { get; set; } = 2;
        public int LUK { get; set; } = 2;
        public int Faith { get; set; } = 2; // 敵にも信仰ステータスを追加

        // 戦闘中の一時的なステータス
        public int Barrier { get; set; } = 0;
        public int Shield { get; set; } = 0;
        public int Reflect { get; set; } = 0;
        
        // 所持スキル (DBには保存しないため直接Listで保持)
        public System.Collections.Generic.List<Skill> EnemySkills { get; set; } = new();
        
        // 状態異常・バフ
        public System.Collections.Generic.List<StatusEffect> ActiveEffects { get; set; } = new();
        
        // 報酬
        public int ExpReward { get; set; } = 10;
        
        // ボス判定
        public bool IsBoss { get; set; } = false;
        
        // 画像等
        public string ImageUrl { get; set; } = ""; // 敵の画像のURL(あれば)

        public (int STR, int DEF, int INT, int DEX, int AGI, int Faith) GetTotalStats()
        {
            int tSTR = STR, tDEF = DEF, tINT = INT, tDEX = DEX, tAGI = AGI, tFaith = Faith;

            if (ActiveEffects != null)
            {
                foreach (var effect in ActiveEffects)
                {
                    int val = effect.Type == EffectType.Buff ? effect.Value : (effect.Type == EffectType.Debuff ? -effect.Value : 0);
                    if (val != 0)
                    {
                        switch (effect.TargetStat)
                        {
                            case "STR": tSTR += val; break;
                            case "DEF": tDEF += val; break;
                            case "INT": tINT += val; break;
                            case "DEX": tDEX += val; break;
                            case "AGI": tAGI += val; break;
                            case "Faith": tFaith += val; break;
                        }
                    }
                }
            }

            // 最低値は1を下回らない（必要に応じて調整可能）
            return (
                System.Math.Max(1, tSTR),
                System.Math.Max(1, tDEF),
                System.Math.Max(1, tINT),
                System.Math.Max(1, tDEX),
                System.Math.Max(1, tAGI),
                System.Math.Max(1, tFaith)
            );
        }
    }
}
