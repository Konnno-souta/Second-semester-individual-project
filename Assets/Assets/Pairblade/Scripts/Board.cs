using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pairblade
{
    /// <summary>
    /// カードの盤面。配る・並べる・2枚選ばれたら知らせる・凍結や入れ替えを行う。
    /// 当たり外れの判定とその結果（攻撃など）は Battle が担当する。
    /// </summary>
    public class Board : MonoBehaviour
    {
        const float Gap = 18f;
        const float CardAspect = 436f / 300f;   // カード画像の 縦/横
        const float MaxCardHeight = 270f;

        RectTransform area;
        readonly List<CardView> cards = new List<CardView>();
        CardView first;

        public bool InputEnabled { get; set; }
        public IReadOnlyList<CardView> Cards => cards;

        /// <summary>2枚目がめくられた瞬間に呼ばれる。</summary>
        public Action<CardView, CardView> PairChosen;

        public static Board Create(Transform parent, Vector2 center, Vector2 size)
        {
            var rt = UI.Node("Board", parent).At(center.x, center.y, size.x, size.y);
            var b = rt.gameObject.AddComponent<Board>();
            b.area = rt;
            return b;
        }

        // ---------- 配る ----------

        /// <summary>count 枚（偶数）の山を作る。数字が重ならないように選び、マークは2枚で別々にする。</summary>
        public static List<CardInfo> MakeDeck(int count, bool joker)
        {
            int pairs = count / 2;
            int numberPairs = joker ? pairs - 1 : pairs;

            var ranks = new List<int>();
            for (int r = 1; r <= 13; r++) ranks.Add(r);
            Shuffle(ranks);

            var deck = new List<CardInfo>();
            var suits = new List<Suit> { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };
            for (int i = 0; i < numberPairs; i++)
            {
                Shuffle(suits);
                deck.Add(new CardInfo(ranks[i], suits[0]));
                deck.Add(new CardInfo(ranks[i], suits[1]));
            }
            if (joker)
            {
                deck.Add(new CardInfo(1, Suit.Joker));
                deck.Add(new CardInfo(2, Suit.Joker));
            }
            Shuffle(deck);
            return deck;
        }

        public static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }

        public IEnumerator Deal(int count, bool joker, float peekSeconds)
        {
            InputEnabled = false;
            first = null;
            foreach (var c in cards) Destroy(c.gameObject);
            cards.Clear();

            var deck = MakeDeck(count, joker);

            // 並べ方：12枚以下は3段、それ以上は4段
            int rows = count <= 12 ? 3 : 4;
            int cols = Mathf.CeilToInt(count / (float)rows);
            Vector2 areaSize = area.rect.size;
            float h = Mathf.Min(
                (areaSize.y - Gap * (rows - 1)) / rows,
                (areaSize.x - Gap * (cols - 1)) / cols * CardAspect,
                MaxCardHeight);
            var size = new Vector2(h / CardAspect, h);

            var targets = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                int inThisRow = Mathf.Min(cols, count - row * cols);   // 最後の段が短いときは中央に寄せる
                float rowWidth = inThisRow * size.x + (inThisRow - 1) * Gap;
                float x = -rowWidth / 2f + size.x / 2f + col * (size.x + Gap);
                float totalHeight = rows * size.y + (rows - 1) * Gap;
                float y = totalHeight / 2f - size.y / 2f - row * (size.y + Gap);
                targets.Add(new Vector2(x, y));
            }

            // 山札の位置（盤面の右下の外）から1枚ずつ飛ばす
            var origin = new Vector2(areaSize.x / 2f + 120f, -areaSize.y / 2f - 60f);
            for (int i = 0; i < count; i++)
            {
                var cv = CardView.Create(area, deck[i], size);
                cv.Rect.anchoredPosition = origin;
                cv.Clicked = OnCardClicked;
                cv.InputAllowed = () => InputEnabled;
                cards.Add(cv);
            }

            const float stagger = 0.035f;
            const float fly = 0.28f;
            for (int i = 0; i < count; i++)
            {
                StartCoroutine(FlyTo(cards[i].Rect, targets[i], fly, (i % 2 == 0)));
                yield return Tw.Wait(stagger);
            }
            yield return Tw.Wait(fly);

            if (peekSeconds > 0f)
            {
                foreach (var c in cards) StartCoroutine(c.Flip(true));
                Sfx.Play("Flip");
                yield return Tw.Wait(0.2f + peekSeconds);
                foreach (var c in cards) StartCoroutine(c.Flip(false));
                Sfx.Play("Flip");
                yield return Tw.Wait(0.25f);
            }
        }

        IEnumerator FlyTo(RectTransform rt, Vector2 target, float dur, bool sound)
        {
            if (sound) Sfx.Play("Flip", 0.5f);
            Vector2 start = rt.anchoredPosition;
            float rot = UnityEngine.Random.Range(-12f, 12f);
            yield return Tw.Lerp(dur, t =>
            {
                float e = Tw.EaseOut(t);
                rt.anchoredPosition = Vector2.LerpUnclamped(start, target, e);
                rt.localRotation = Quaternion.Euler(0, 0, rot * (1f - e));
            });
        }

        // ---------- めくる ----------

        void OnCardClicked(CardView c)
        {
            if (!InputEnabled || !c.Clickable) return;

            Sfx.Play("Flip");
            StartCoroutine(c.Flip(true));

            if (first == null)
            {
                first = c;
                return;
            }

            InputEnabled = false;   // 判定が終わるまでクリックを止める
            var a = first;
            first = null;
            PairChosen?.Invoke(a, c);
        }

        public bool AllMatched => cards.TrueForAll(c => c.Matched);

        // ---------- 敵の妨害 ----------

        List<CardView> FaceDownCandidates(bool allowFrozen)
        {
            var list = new List<CardView>();
            foreach (var c in cards)
                if (!c.Matched && !c.FaceUp && (allowFrozen || c.FrozenTurns <= 0)) list.Add(c);
            return list;
        }

        /// <summary>裏向きのカード2枚を入れ替える。2枚が互いの位置へ動いて戻り、中身が入れ替わる。</summary>
        public IEnumerator SwapRandom()
        {
            var list = FaceDownCandidates(true);
            if (list.Count < 2) yield break;
            Shuffle(list);
            CardView a = list[0], b = list[1];
            Vector2 pa = a.Rect.anchoredPosition, pb = b.Rect.anchoredPosition;
            a.Rect.SetAsLastSibling();
            b.Rect.SetAsLastSibling();
            Sfx.Play("Swap");
            yield return Tw.Lerp(0.45f, t =>
            {
                float e = Tw.EaseInOut(t);
                Vector2 lift = new Vector2(0, Tw.Bump(t) * 40f);
                a.Rect.anchoredPosition = Vector2.Lerp(pa, pb, e) + lift;
                b.Rect.anchoredPosition = Vector2.Lerp(pb, pa, e) - lift;
            });
            // 見た目上は入れ替わったので、位置を戻して中身を入れ替える（裏面は同じ絵なので気づかれない）
            a.Rect.anchoredPosition = pa;
            b.Rect.anchoredPosition = pb;
            CardView.SwapContents(a, b);
        }

        /// <summary>裏向きのカードを count 枚、turns ターン凍らせる。</summary>
        public void FreezeRandom(int count, int turns)
        {
            var list = FaceDownCandidates(false);
            Shuffle(list);
            for (int i = 0; i < Mathf.Min(count, list.Count); i++) list[i].SetFrozen(turns);
            Sfx.Play("Freeze");
        }

        public void TickFrozen()
        {
            foreach (var c in cards) c.TickFrozen();
        }

        /// <summary>凍結のせいで2枚めくれない状態になったら、凍結を全部解く。</summary>
        public void EnsurePlayable()
        {
            int unmatched = 0, flippable = 0;
            foreach (var c in cards)
            {
                if (c.Matched) continue;
                unmatched++;
                if (c.FrozenTurns <= 0) flippable++;
            }
            if (unmatched >= 2 && flippable < 2)
                foreach (var c in cards) c.SetFrozen(0);
        }
    }
}
