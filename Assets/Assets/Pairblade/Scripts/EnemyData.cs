using UnityEngine;

namespace Pairblade
{
    /// <summary>
    /// 敵1体ぶんの設定。GameManager のインスペクターで数値を書き換えられる。
    /// </summary>
    [System.Serializable]
    public class EnemyData
    {
        [Tooltip("画面に表示する名前")]
        public string displayName = "スライム";

        [Tooltip("Resources/Pairblade/Enemies/ にある画像のファイル名（portrait が空のときに使う）")]
        public string spriteName = "Slime";

        [Tooltip("ここに画像を入れると、spriteName より優先される")]
        public Sprite portrait;

        [Min(1)] public int maxHp = 40;
        [Tooltip("1回の攻撃のダメージ")]
        [Min(0)] public int attack = 7;
        [Tooltip("何ターンごとに攻撃するか")]
        [Min(1)] public int interval = 3;
        [Tooltip("♠ のダメージをこの数だけ減らす（♦ とジョーカーには効かない）")]
        [Min(0)] public int armor = 0;
        [Tooltip("攻撃のあと、裏向きのカード2枚の位置を入れ替える")]
        public bool swapsCards = false;
        [Tooltip("攻撃のあと、裏向きのカード2枚を3ターン凍らせる（めくれなくなる）")]
        public bool freezesCards = false;
        [Tooltip("場に並べるカードの枚数（偶数、4〜26）")]
        [Range(4, 26)] public int cardCount = 16;

        public EnemyData() { }

        public EnemyData(string displayName, string spriteName, int maxHp, int attack, int interval,
                         int armor, bool swapsCards, bool freezesCards, int cardCount)
        {
            this.displayName = displayName;
            this.spriteName = spriteName;
            this.maxHp = maxHp;
            this.attack = attack;
            this.interval = interval;
            this.armor = armor;
            this.swapsCards = swapsCards;
            this.freezesCards = freezesCards;
            this.cardCount = cardCount;
        }

        public Sprite GetPortrait()
        {
            return portrait != null ? portrait : Res.GetSprite("Enemies/" + spriteName);
        }

        /// <summary>偶数かつ 4〜26 に収めた枚数。</summary>
        public int SafeCardCount
        {
            get
            {
                int n = Mathf.Clamp(cardCount, 4, 26);
                return n - (n % 2);
            }
        }
    }
}
