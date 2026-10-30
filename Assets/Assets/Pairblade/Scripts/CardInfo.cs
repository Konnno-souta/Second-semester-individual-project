using UnityEngine;

namespace Pairblade
{
    public enum Suit { Spade, Heart, Diamond, Club, Joker }

    /// <summary>
    /// 1枚のカードの中身（数字とマーク）。見た目は CardView が担当する。
    /// </summary>
    [System.Serializable]
    public struct CardInfo
    {
        public int Rank;   // 1=A, 2〜10, 11=J, 12=Q, 13=K（ジョーカーは 1=黒, 2=赤）
        public Suit Suit;

        public CardInfo(int rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        public bool IsJoker => Suit == Suit.Joker;

        /// <summary>ペアになるか：同じ数字どうし、またはジョーカーどうし。</summary>
        public bool Matches(CardInfo other)
        {
            if (IsJoker || other.IsJoker) return IsJoker && other.IsJoker;
            return Rank == other.Rank;
        }

        /// <summary>効果の強さ。A=11、J・Q・K=10、それ以外は数字のまま。</summary>
        public int Value
        {
            get
            {
                if (Rank == 1) return 11;
                if (Rank >= 11) return 10;
                return Rank;
            }
        }

        public string RankLabel
        {
            get
            {
                switch (Rank)
                {
                    case 1: return "A";
                    case 11: return "J";
                    case 12: return "Q";
                    case 13: return "K";
                    default: return Rank.ToString();
                }
            }
        }

        public string SuitMark
        {
            get
            {
                switch (Suit)
                {
                    case Suit.Spade: return "♠";
                    case Suit.Heart: return "♥";
                    case Suit.Diamond: return "♦";
                    case Suit.Club: return "♣";
                    default: return "";
                }
            }
        }

        /// <summary>Resources/Pairblade/Cards/ にある画像のファイル名。</summary>
        public string SpriteName
        {
            get
            {
                if (IsJoker) return Rank == 1 ? "Joker1" : "Joker2";
                string s = Suit == Suit.Spade ? "S" : Suit == Suit.Heart ? "H" : Suit == Suit.Diamond ? "D" : "C";
                return RankLabel + s;
            }
        }

        public string Label => IsJoker ? "JOKER" : SuitMark + RankLabel;

        public bool IsRed => Suit == Suit.Heart || Suit == Suit.Diamond;
    }
}
