using System.Collections.Generic;

namespace Pairblade
{
    /// <summary>
    /// 1回の挑戦（ラン）を通して持ち越すプレイヤーの状態。
    /// </summary>
    public class PlayerState
    {
        public int MaxHp;
        public int Hp;

        // 強化で増える値
        public int SpadeBonus;
        public int DiamondBonus;
        public int ClubBonus;
        public int HeartBonus;
        public float ComboStep;     // コンボ1段ごとの倍率の上がり幅
        public bool AlwaysJoker;    // 毎回ジョーカーを場に出す
        public bool Peek;           // 配るたびに全カードを見せる
        public int ExtraInterval;   // 敵の攻撃間隔を延ばす

        public readonly HashSet<UpgradeId> Owned = new HashSet<UpgradeId>();

        // 記録
        public int TotalTurns;
        public int TotalMatches;
        public int BestCombo;

        public PlayerState(int maxHp, float comboStep)
        {
            MaxHp = maxHp;
            Hp = maxHp;
            ComboStep = comboStep;
        }

        public void Heal(int amount)
        {
            Hp = System.Math.Min(MaxHp, Hp + amount);
        }

        public void Apply(Upgrade u)
        {
            Owned.Add(u.Id);
            switch (u.Id)
            {
                case UpgradeId.Spade: SpadeBonus += 2; break;
                case UpgradeId.Diamond: DiamondBonus += 2; break;
                case UpgradeId.Club: ClubBonus += 2; break;
                case UpgradeId.Heart: HeartBonus += 2; break;
                case UpgradeId.Vitality: MaxHp += 10; Hp += 10; break;
                case UpgradeId.Joker: AlwaysJoker = true; break;
                case UpgradeId.Combo: ComboStep += 0.15f; break;
                case UpgradeId.Peek: Peek = true; break;
                case UpgradeId.Patience: ExtraInterval += 1; break;
            }
        }
    }
}
