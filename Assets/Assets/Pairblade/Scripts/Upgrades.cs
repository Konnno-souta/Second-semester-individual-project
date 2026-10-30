using System.Collections.Generic;
using UnityEngine;

namespace Pairblade
{
    public enum UpgradeId { Spade, Diamond, Club, Heart, Vitality, Joker, Combo, Peek, Patience }

    public class Upgrade
    {
        public UpgradeId Id;
        public string Name;
        public string Description;
        public string Icon;     // Resources/Pairblade/UI/ のアイコン名
        public Color IconColor;
        public bool Unique;     // 1回しか取れない

        public Upgrade(UpgradeId id, string name, string desc, string icon, Color iconColor, bool unique)
        {
            Id = id; Name = name; Description = desc; Icon = icon; IconColor = iconColor; Unique = unique;
        }
    }

    /// <summary>敵を倒したあとに選べる強化の一覧。</summary>
    public static class Upgrades
    {
        public static readonly Upgrade[] All =
        {
            new Upgrade(UpgradeId.Spade,    "研いだ刃",     "♠ のダメージ +2",                    "Spade",     UI.Cream, false),
            new Upgrade(UpgradeId.Diamond,  "古い魔導書",   "♦ のダメージ +2",                    "Diamond",   UI.Red,   false),
            new Upgrade(UpgradeId.Club,     "鉄の盾",       "♣ の防御 +2",                        "Club",      UI.Cream, false),
            new Upgrade(UpgradeId.Heart,    "薬草袋",       "♥ の回復 +2",                        "Heart",     UI.Red,   false),
            new Upgrade(UpgradeId.Vitality, "頑丈な体",     "最大HP +10\n（HPも10回復）",          "Heart",     UI.Green, false),
            new Upgrade(UpgradeId.Combo,    "連撃の心得",   "コンボ1段ごとの\n倍率 +0.15",          "Sword",     UI.Gold,  false),
            new Upgrade(UpgradeId.Joker,    "道化の招待状", "配るたびに必ず\nジョーカーが入る",    "Flame",     UI.Gold,  true),
            new Upgrade(UpgradeId.Peek,     "鷹の目",       "配った直後に\n全カードを1秒見せる",   "Snow",      UI.Ice,   true),
            new Upgrade(UpgradeId.Patience, "砂時計",       "敵の攻撃間隔 +1",                    "Hourglass", UI.Gold,  true),
        };

        /// <summary>まだ取れる強化から、重複なしで count 個をランダムに選ぶ。</summary>
        public static List<Upgrade> Roll(PlayerState p, int count)
        {
            var pool = new List<Upgrade>();
            foreach (var u in All)
                if (!(u.Unique && p.Owned.Contains(u.Id))) pool.Add(u);

            var result = new List<Upgrade>();
            while (result.Count < count && pool.Count > 0)
            {
                int i = Random.Range(0, pool.Count);
                result.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return result;
        }
    }
}
