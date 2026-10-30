using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pairblade
{
    /// <summary>
    /// 場に並ぶカード1枚の見た目と操作。
    /// 表裏の切り替え、めくるアニメーション、マウスを乗せたときの拡大、凍結表示を担当する。
    /// </summary>
    public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public CardInfo Info { get; private set; }
        public bool FaceUp { get; private set; }
        public bool Matched { get; private set; }
        public int FrozenTurns { get; private set; }

        /// <summary>クリックされたときに Board へ知らせる。</summary>
        public Action<CardView> Clicked;
        /// <summary>いまクリックを受け付けてよいか（Board が決める）。</summary>
        public Func<bool> InputAllowed;

        public RectTransform Rect { get; private set; }

        RectTransform visual;   // めくるときに横に縮める部分
        Image face;
        Image highlight;
        GameObject frost;
        Text frostText;
        CanvasGroup group;

        bool busy;              // アニメーション中
        bool hovered;
        float hoverScale = 1f;
        float extraScale = 1f;  // 演出用の拡大

        public bool Clickable => !FaceUp && !Matched && FrozenTurns <= 0 && !busy;

        public static CardView Create(Transform parent, CardInfo info, Vector2 size)
        {
            var rt = UI.Node("Card " + info.Label, parent);
            rt.sizeDelta = size;
            var cv = rt.gameObject.AddComponent<CardView>();
            cv.Build(info);
            return cv;
        }

        void Build(CardInfo info)
        {
            Info = info;
            Rect = (RectTransform)transform;
            group = gameObject.AddComponent<CanvasGroup>();

            // クリック判定用の透明な板
            var hit = gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);

            // 影
            var shadow = UI.MakeImage(Rect, "Shadow", Res.GetSprite("Cards/Back"), new Color(0, 0, 0, 0.35f));
            shadow.rectTransform.Fill();
            shadow.rectTransform.anchoredPosition = new Vector2(5, -7);

            visual = UI.Node("Visual", Rect).Fill();
            face = UI.MakeImage(visual, "Face", Res.GetSprite("Cards/Back"), Color.white);
            face.rectTransform.Fill();

            highlight = UI.MakeImage(visual, "Highlight", Res.GetSprite("UI/Frame"), UI.Gold);
            highlight.rectTransform.Fill();
            highlight.enabled = false;

            // 凍結の表示
            var f = UI.Node("Frost", visual).Fill();
            frost = f.gameObject;
            var ice = UI.MakeImage(f, "Ice", Res.GetSprite("Cards/Back"), new Color(0.66f, 0.85f, 0.95f, 0.72f));
            ice.rectTransform.Fill();
            var snow = UI.MakeImage(f, "Snow", Res.GetSprite("UI/Snow"), Color.white);
            snow.rectTransform.At(0, 12, 70, 70);
            frostText = UI.MakeText(f, "3", 34, Color.white);
            frostText.rectTransform.At(0, -48, 80, 50);
            frost.SetActive(false);
        }

        // ---------- 入力 ----------

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left) Clicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData e) { hovered = true; }
        public void OnPointerExit(PointerEventData e) { hovered = false; }

        void Update()
        {
            bool canHover = hovered && Clickable && (InputAllowed == null || InputAllowed());
            float target = canHover ? 1.07f : 1f;
            hoverScale = Mathf.Lerp(hoverScale, target, 1f - Mathf.Exp(-18f * Time.deltaTime));
            Rect.localScale = Vector3.one * hoverScale * extraScale;
            highlight.enabled = canHover;
        }

        // ---------- 表示の切り替え ----------

        void RefreshSprite()
        {
            face.sprite = Res.GetSprite(FaceUp ? "Cards/" + Info.SpriteName : "Cards/Back");
        }

        public void SetFaceInstant(bool up)
        {
            FaceUp = up;
            visual.localScale = Vector3.one;
            RefreshSprite();
        }

        /// <summary>横に縮めて、絵を差し替えて、また広げる。これで「めくる」動きになる。</summary>
        public IEnumerator Flip(bool up, float duration = 0.16f)
        {
            busy = true;
            yield return Tw.Lerp(duration * 0.5f, t => visual.localScale = new Vector3(1f - Tw.EaseIn(t), 1f, 1f));
            FaceUp = up;
            RefreshSprite();
            yield return Tw.Lerp(duration * 0.5f, t => visual.localScale = new Vector3(Tw.EaseOut(t), 1f, 1f));
            busy = false;
        }

        /// <summary>揃ったカード：ふくらんでから薄くなる。</summary>
        public IEnumerator PlayMatched()
        {
            Matched = true;
            yield return Tw.Lerp(0.22f, t => extraScale = 1f + 0.14f * Tw.Bump(t));
            extraScale = 1f;
            yield return Tw.Lerp(0.3f, t => group.alpha = Mathf.Lerp(1f, 0.28f, t));
        }

        /// <summary>外れたカード：小さく横に揺れる。</summary>
        public IEnumerator Wiggle()
        {
            yield return Tw.Lerp(0.25f, t => visual.anchoredPosition = new Vector2(Mathf.Sin(t * Mathf.PI * 6f) * 8f * (1f - t), 0));
            visual.anchoredPosition = Vector2.zero;
        }

        public void SetFrozen(int turns)
        {
            FrozenTurns = Mathf.Max(0, turns);
            frost.SetActive(FrozenTurns > 0);
            frostText.text = FrozenTurns.ToString();
        }

        public void TickFrozen()
        {
            if (FrozenTurns > 0) SetFrozen(FrozenTurns - 1);
        }

        /// <summary>中身だけを入れ替える（敵の「入れ替え」用）。</summary>
        public static void SwapContents(CardView a, CardView b)
        {
            var info = a.Info; a.Info = b.Info; b.Info = info;
            int fa = a.FrozenTurns; a.SetFrozen(b.FrozenTurns); b.SetFrozen(fa);
            a.RefreshSprite();
            b.RefreshSprite();
        }
    }
}
