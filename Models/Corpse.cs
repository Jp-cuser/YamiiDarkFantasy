using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YamiiDarkFantasy.Models
{
    public class Corpse
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Job { get; set; } = "";
        public int Level { get; set; }
        public int Floor { get; set; }
        public string StageId { get; set; } = "";

        [Column(TypeName = "TEXT")]
        public string SkillsJson { get; set; } = "[]";

        [Column(TypeName = "TEXT")]
        public string EquipmentsJson { get; set; } = "[]";

        public DateTime DiedAt { get; set; } = DateTime.UtcNow;

        // 敵としてのステータス計算用（亡骸はプレイヤーのステータスの一部を引き継ぐ）
        public int MaxHP { get; set; }
        public int STR { get; set; }
        public int DEF { get; set; }
        public int INT { get; set; }
        public int AGI { get; set; }
    }
}
