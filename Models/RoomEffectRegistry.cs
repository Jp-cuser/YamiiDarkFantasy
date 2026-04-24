using System.Collections.Generic;

namespace YamiiDarkFantasy.Models
{
    public class RoomEffectTemplate
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "❓";
    }

    public static class RoomEffectRegistry
    {
        public static readonly Dictionary<string, RoomEffectTemplate> Effects = new()
        {
            { "red_remains", new RoomEffectTemplate { Id = "red_remains", Name = "亡骸", Description = "その階層で倒された他のプレイヤーの亡骸が襲いかかる...", Icon = "💀" } },
            { "vortex", new RoomEffectTemplate { Id = "vortex", Name = "渦", Description = "不吉な渦が巻いており、状態変化の抵抗ができない！", Icon = "🌀" } },
            { "tailwind", new RoomEffectTemplate { Id = "tailwind", Name = "追い風", Description = "追い風が吹き、スキルのクールダウンが最大1になる！", Icon = "🍃" } },
            { "monster_gate", new RoomEffectTemplate { Id = "monster_gate", Name = "怪獣", Description = "不可解な力により、なんらかのスキルを失う...", Icon = "👹" } },
            { "puppet", new RoomEffectTemplate { Id = "puppet", Name = "傀儡", Description = "「混乱」状態になるが、受けるダメージが半減する！", Icon = "🪆" } },
            { "cramp", new RoomEffectTemplate { Id = "cramp", Name = "窮屈", Description = "足場が悪く、回避が一切不可能になる！", Icon = "🦶" } },
            { "rest", new RoomEffectTemplate { Id = "rest", Name = "休息", Description = "安らぎを感じる場所。HPが回復する。", Icon = "🧘" } },
            { "darkness", new RoomEffectTemplate { Id = "darkness", Name = "暗闇", Description = "深い闇に包まれ、敵の姿が見えない...", Icon = "🌑" } },
            { "barrier", new RoomEffectTemplate { Id = "barrier", Name = "結界", Description = "シールド現象が停止し、状態変化が無効化される。", Icon = "✨" } },
            { "high_temp", new RoomEffectTemplate { Id = "high_temp", Name = "高温", Description = "凄まじい熱気により、回復が一切受け付けられない！", Icon = "🔥" } },
            { "vast", new RoomEffectTemplate { Id = "vast", Name = "広大", Description = "あまりに広大で距離感が狂い、命中率がダウンする。", Icon = "🏜️" } },
            { "track", new RoomEffectTemplate { Id = "track", Name = "痕跡", Description = "ボスの痕跡を見つけ、目的地までの距離が縮まる！", Icon = "👣" } },
            { "submerged", new RoomEffectTemplate { Id = "submerged", Name = "水没", Description = "水没したエリア。敵のレベルが上昇している！", Icon = "🌊" } },
            { "sculpture", new RoomEffectTemplate { Id = "sculpture", Name = "石像", Description = "例外なく徐々に石化していく呪いが漂っている...", Icon = "🗿" } },
            { "current", new RoomEffectTemplate { Id = "current", Name = "電流", Description = "電撃が走り、すべての攻撃が必ずクリティカルになる！", Icon = "⚡" } },
            { "empathy", new RoomEffectTemplate { Id = "empathy", Name = "道場", Description = "追撃、反射、継続ダメージがすべて無効化される。", Icon = "🤝" } },
            { "door", new RoomEffectTemplate { Id = "door", Name = "扉", Description = "中身が一切見えない不思議な扉。入るまで中身は不明だ。", Icon = "🚪" } },
            { "swamp", new RoomEffectTemplate { Id = "swamp", Name = "沼", Description = "足を取られ、スキルのクールダウンが+1増加する！", Icon = "🍄" } },
            { "peace", new RoomEffectTemplate { Id = "peace", Name = "平和", Description = "敵は弱くなるが、経験値を得ることができない。", Icon = "🕊️" } },
            { "treasure_vault", new RoomEffectTemplate { Id = "treasure_vault", Name = "宝物庫", Description = "伝説の武具が眠る。ただし強大な敵も待ち受けている！", Icon = "💎" } },
            { "magic_circle", new RoomEffectTemplate { Id = "magic_circle", Name = "魔法陣", Description = "スキルレベルが上がるが、敵にバリアが付与される！", Icon = "🧙" } },
            { "maze", new RoomEffectTemplate { Id = "maze", Name = "迷路", Description = "構造が複雑で、ボスまでの距離が伸びてしまった。", Icon = "🧩" } },
            { "gear", new RoomEffectTemplate { Id = "gear", Name = "歯車", Description = "ミスや回避が発生するたび、反動で大ダメージを受ける！", Icon = "⚙️" } },
            { "illusion", new RoomEffectTemplate { Id = "illusion", Name = "幻影", Description = "自身のスキルがランダムに入れ替わってしまう！", Icon = "🌈" } },
            { "mirage", new RoomEffectTemplate { Id = "mirage", Name = "蜃気楼", Description = "敵の名前や姿が偽りの表示になる...", Icon = "🎇" } },
            { "icicle", new RoomEffectTemplate { Id = "icicle", Name = "氷柱", Description = "行動のたび、消費したスキルのCTに応じたダメージを受ける！", Icon = "❄️" } },
            { "low_temp", new RoomEffectTemplate { Id = "low_temp", Name = "低温", Description = "極低温により、クリティカルダメージが劇的に増える！", Icon = "🧊" } },
            { "mine", new RoomEffectTemplate { Id = "mine", Name = "鉱山", Description = "アイテムの能力数が通常よりも多くなりやすい場所だ。", Icon = "⛏️" } },
            { "petal", new RoomEffectTemplate { Id = "petal", Name = "花びら", Description = "HP・シールド・バリアの獲得量が2倍になる！", Icon = "🌸" } },
            { "hazy", new RoomEffectTemplate { Id = "hazy", Name = "朧", Description = "ランダムに「無敵」状態が付与される不安定な空間。", Icon = "🌫️" } },
            { "miasma", new RoomEffectTemplate { Id = "miasma", Name = "瘴気", Description = "瘴気が漂い、敵味方双方に状態変化が付与される。", Icon = "☢️" } },
            { "muddy", new RoomEffectTemplate { Id = "muddy", Name = "泥濘", Description = "CDが最小2、最大4に固定されてしまう！", Icon = "💩" } },
            { "breeze", new RoomEffectTemplate { Id = "breeze", Name = "そよ風", Description = "状態異常がすぐに治りやすい、穏やかな空間。", Icon = "🌬️" } },
            { "prediction", new RoomEffectTemplate { Id = "prediction", Name = "予見", Description = "予見の力により、回避・クリティカル率が3倍になる！", Icon = "👁️" } },
            { "whiteout", new RoomEffectTemplate { Id = "whiteout", Name = "ホワイトアウト", Description = "猛吹雪により、クリティカル率が低下する...", Icon = "🌨️" } },
            { "curse_gear", new RoomEffectTemplate { Id = "curse_gear", Name = "呪縛", Description = "毎ターンバリアが減り、石化が進行していく...", Icon = "⛓️" } },
            { "pot_room", new RoomEffectTemplate { Id = "pot_room", Name = "つぼの間", Description = "入ると近くに「さまようむくろ」が増える不気味な部屋。", Icon = "🏺" } },
            { "distortion", new RoomEffectTemplate { Id = "distortion", Name = "歪み", Description = "受ける状態異常が2倍になるが、早く解除される空間。", Icon = "🧶" } },
            { "refrain", new RoomEffectTemplate { Id = "refrain", Name = "リフレイン", Description = "自身のターンに受けるダメージが3倍になる危険な空間！", Icon = "🔄" } },
            { "dread", new RoomEffectTemplate { Id = "dread", Name = "怖気", Description = "シールドがない間、状態異常の抵抗が一切できない。", Icon = "😨" } },
            { "chill", new RoomEffectTemplate { Id = "chill", Name = "寒気", Description = "自身の攻撃力に応じて、HPがじわじわと削れていく...", Icon = "🥶" } },
            { "silence", new RoomEffectTemplate { Id = "silence", Name = "静寂", Description = "CTがターン経過で減らず、全スキルCD時に即全回復する。", Icon = "🤫" } },
            { "corpse_pile", new RoomEffectTemplate { Id = "corpse_pile", Name = "死骸の山", Description = "あらゆる即死効果の発動率が3倍になる恐ろしい場所。", Icon = "🏔️" } },
            { "dusk", new RoomEffectTemplate { Id = "dusk", Name = "夕暮れ", Description = "戦闘開始時、敵に一時的な状態変化をすべて受け渡す。", Icon = "🌇" } },
            { "dread_beat", new RoomEffectTemplate { Id = "dread_beat", Name = "戦慄", Description = "合計クールダウンが50を超えた瞬間、即死する！", Icon = "💓" } },
            { "snow_path", new RoomEffectTemplate { Id = "snow_path", Name = "雪道", Description = "シールド・バリアがない間、スキルのCDが2倍になる。", Icon = "❄️" } },
            { "chocolate", new RoomEffectTemplate { Id = "chocolate", Name = "チョコまみれ", Description = "バリアを得る代わりに、HPが全回復した！", Icon = "🍫" } },
            { "drowsy", new RoomEffectTemplate { Id = "drowsy", Name = "眠気", Description = "HP回復のたび、ランダムスキルのCDが倍に増える。", Icon = "💤" } },
            { "earthquake", new RoomEffectTemplate { Id = "earthquake", Name = "地震", Description = "足元の揺れにより、全スキルのCDが2倍になる！", Icon = "🌋" } },
            { "scorching", new RoomEffectTemplate { Id = "scorching", Name = "灼熱", Description = "毎ターン、最大HPの5%のダメージを受ける領域。", Icon = "💢" } },
            { "frozen", new RoomEffectTemplate { Id = "frozen", Name = "凍結", Description = "たまにスキル使用後のCDが+5されてしまう極寒地。", Icon = "🧊" } }
        };

        public static readonly Dictionary<string, RoomEffectTemplate> FloorThemes = new()
        {
            { "invisible_rooms", new RoomEffectTemplate { Id = "invisible_rooms", Name = "すべての へやが みえない！", Description = "フロア全体が濃霧に包まれ、部屋の種類が判別できない。" } },
            { "long_status", new RoomEffectTemplate { Id = "long_status", Name = "じょうたいへんかが いつもよりも ながびく。", Description = "このフロアでは、あらゆるバフ・デバフの持続ターンが増加する。" } },
            { "high_treasure_sculpture", new RoomEffectTemplate { Id = "high_treasure_sculpture", Name = "「石像」「宝物庫」へや だらけだ！", Description = "特殊な部屋の出現率が極端に高くなっている。" } },
            { "high_puppet", new RoomEffectTemplate { Id = "high_puppet", Name = "「傀儡」へや だらけだ！", Description = "混乱の魔力がフロア中に漂っている。" } },
            { "no_effects", new RoomEffectTemplate { Id = "no_effects", Name = "へやこうかが いっさいない。", Description = "静寂なフロア。部屋の効果がすべて無効化されている。" } },
            { "double_all", new RoomEffectTemplate { Id = "double_all", Name = "ダメージ かいふく じょうたいへんかが 2ばい！", Description = "フロアのエネルギーが活性化し、すべての数値が倍増する。" } },
            { "straight_path", new RoomEffectTemplate { Id = "straight_path", Name = "まっすぐにしか すすめない！", Description = "迷路のようなフロア。分岐が存在しないか極めて少ない。" } },
            { "cooldown_reset", new RoomEffectTemplate { Id = "cooldown_reset", Name = "せんとう しゅうりょうじ すべてのスキルのCDが3になる", Description = "戦闘後のリフレッシュ効果が変化している。" } },
            { "exp_down_equip_up", new RoomEffectTemplate { Id = "exp_down_equip_up", Name = "けいけんちが すくなく そうびひんが おおく てにはいる", Description = "成長よりも装備を整えるのに適したフロア。" } },
            { "skill_focus", new RoomEffectTemplate { Id = "skill_focus", Name = "そうびが てにはいりにくく スキルばかりが てにはいる", Description = "技術の習得に適した魔法のフロア。" } },
            { "luck_crit_risk", new RoomEffectTemplate { Id = "luck_crit_risk", Name = "このフロアでは うんのよさが あがるが てきの クリティカルりつも あがる", Description = "幸運と危険が隣り合わせのフロア。" } },
            { "full_monster", new RoomEffectTemplate { Id = "full_monster", Name = "すべての へやに てきが あらわれる", Description = "魔物の巣窟。安息の地は存在しない。" } }
        };
    }
}
