using System;
using System.Collections.Generic;
using System.Linq;

namespace YamiiDarkFantasy.Models
{
    public static class EnemyDictionary
    {
        // ここに存在する全てのモンスターの基礎パラメーターを定義します。
        // 将来的に「スライムの群れ」等のグループ定義を追加する場合も、ここを拡張します。
        public static List<Enemy> AvailableEnemies = new List<Enemy>
        {
            new Enemy 
            { 
                EnemyId = "wild_dog", 
                Name = "狂った野犬", 
                Description = "飢えにより正気を失った野良犬。鋭い牙を持つ。",
                MaxHP = 35, STR = 8, DEF = 2, AGI = 5,
                ImageUrl = "https://example.com/wild_dog.png",
                EnemySkills = new List<Skill> { Skill.GetSkill("bite"), Skill.GetSkill("attack") },
                ExpReward = 15,
                Barrier = 0, Shield = 0, Reflect = 0, Faith = 2
            },
            new Enemy 
            { 
                EnemyId = "ghost", 
                Name = "悪しき怨霊", 
                Description = "かつて冒険者だった者の怨念。実体がないため攻撃が通りにくい。",
                MaxHP = 20, STR = 2, INT = 12, DEF = 1, AGI = 8, ExpReward = 20,
                EnemySkills = new List<Skill> { Skill.GetSkill("curse"), Skill.GetSkill("attack") }
            },
            new Enemy 
            { 
                EnemyId = "goblin", 
                Name = "ゴブリンの残党", 
                Description = "小柄だが狡猾な魔物。油断してはいけない。",
                MaxHP = 45, STR = 6, DEF = 4, DEX = 5, AGI = 6, ExpReward = 12,
                EnemySkills = new List<Skill> { Skill.GetSkill("slash"), Skill.GetSkill("throw_stone") }
            },
            new Enemy 
            { 
                EnemyId = "living_armor", 
                Name = "彷徨う甲冑", 
                Description = "持ち主を失い、魔力だけで動く重装の鎧。分厚い装甲が厄介だ。",
                MaxHP = 60, STR = 10, DEF = 15, AGI = 1, ExpReward = 35,
                EnemySkills = new List<Skill> { Skill.GetSkill("heavy_blow"), Skill.GetSkill("shield_bash") }
            },
            new Enemy 
            { 
                EnemyId = "dire_wolf", 
                Name = "ダイアウルフ", 
                Description = "魔力を含んだ肉を食らい、巨大化した恐ろしい狼。",
                MaxHP = 50, STR = 12, DEF = 5, AGI = 8,
                EnemySkills = new List<Skill> { Skill.GetSkill("poison"), Skill.GetSkill("curse") },
                ExpReward = 20,
                Barrier = 0, Shield = 0, Reflect = 0, Faith = 5
            },
            new Enemy 
            { 
                EnemyId = "skeleton", 
                Name = "骸骨兵", 
                Description = "かつての戦士の成れの果て。乾いた音を立てて襲い来る。",
                MaxHP = 25, STR = 8, DEF = 5, AGI = 3, ExpReward = 15,
                EnemySkills = new List<Skill> { Skill.GetSkill("slash"), Skill.GetSkill("guard_stance") }
            },
            
            // --- ボス敵 ---
            new Enemy 
            { 
                EnemyId = "boss_dragon", 
                Name = "深淵の古竜", 
                Description = "迷宮の奥底に巣食う、闇に染まった巨大な竜。",
                MaxHP = 200, STR = 25, DEF = 15, INT = 20,
                EnemySkills = new List<Skill> { Skill.GetSkill("dragon_breath"), Skill.GetSkill("heavy_blow"), Skill.GetSkill("attack") },
                ExpReward = 100,
                IsBoss = true,
                Barrier = 2, Shield = 50, Reflect = 10, Faith = 10
            },
            new Enemy 
            { 
                EnemyId = "boss_demon", 
                Name = "上位悪魔", 
                Description = "人間の魂をすする邪悪な存在。その魔力は絶大だ。",
                MaxHP = 150, STR = 15, DEF = 10, INT = 30, ExpReward = 200, IsBoss = true,
                EnemySkills = new List<Skill> { Skill.GetSkill("curse"), Skill.GetSkill("small_heal"), Skill.GetSkill("fireball") }
            }
        };

        // 各「出現セット」には、どのEnemyIdの敵が含まれるかを定義する
        public static Dictionary<string, List<string>> EncounterSets = new Dictionary<string, List<string>>
        {
            { "beast_set", new List<string> { "wild_dog", "dire_wolf", "goblin" } },
            { "undead_set", new List<string> { "ghost", "skeleton", "living_armor" } },
            { "boss_set", new List<string> { "boss_dragon", "boss_demon" } }
        };

        // 指定されたセットの中からランダムに敵を1体選んで生成する
        public static Enemy GetRandomEnemyFromSet(string setId, int floor)
        {
            var rand = new Random();
            if (EncounterSets.TryGetValue(setId, out var enemyIds) && enemyIds.Any())
            {
                string randomEnemyId = enemyIds[rand.Next(enemyIds.Count)];
                return GetEnemy(randomEnemyId, floor);
            }
            
            // セットが見つからない場合はデフォルトを返す
            return GetEnemy("wild_dog", floor);
        }

        // 敵のIDからコピーを取得してフロアごとにステータスを強化するシステム
        public static Enemy GetEnemy(string enemyId, int floor)
        {
            var template = AvailableEnemies.FirstOrDefault(e => e.EnemyId == enemyId) ?? AvailableEnemies.First();
            
            var rand = new Random();
            int hpVariance = rand.Next(-5, 6); // HPに若干のランダム性を付与

            return new Enemy
            {
                EnemyId = template.EnemyId,
                Name = template.Name,
                Description = template.Description,
                // フロア数に応じたバフ補正を加算
                MaxHP = template.MaxHP + hpVariance + (floor * 5),
                CurrentHP = template.MaxHP + hpVariance + (floor * 5),
                STR = template.STR + (floor * 1),
                DEF = template.DEF + (floor / 2),
                INT = template.INT + (floor * 1),
                DEX = template.DEX + (floor / 2),
                AGI = template.AGI + (floor / 2),
                LUK = template.LUK,
                ExpReward = template.ExpReward + (floor * 2),
                ImageUrl = template.ImageUrl,
                IsBoss = template.IsBoss,
                EnemySkills = template.EnemySkills // 初期スキルはコピーしておく
            };
        }
    }
}
