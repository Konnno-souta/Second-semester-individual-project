using UnityEngine;
using UnityEngine.UI;

namespace Pairblade
{
    /// <summary>
    /// UI部品をコードで作るための道具。色もここでまとめて決めている。
    /// </summary>
    public static class UI
    {
        public static readonly Color Cream = new Color32(239, 227, 200, 255);
        public static readonly Color Gold = new Color32(201, 169, 106, 255);
        public static readonly Color Ink = new Color32(21, 17, 15, 255);
        public static readonly Color Wine = new Color32(110, 28, 38, 255);
        public static readonly Color Red = new Color32(214, 74, 58, 255);
        public static readonly Color Green = new Color32(126, 186, 110, 255);
        public static readonly Color Ice = new Color32(168, 216, 236, 255);
        public static readonly Color Dim = new Color32(150, 140, 120, 255);

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>親の中央（anchor）を基準に、位置とサイズを決める。</summary>
        public static RectTransform Set(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform At(this RectTransform rt, float x, float y, float w, float h)
        {
            return rt.Set(new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));
        }

        /// <summary>親いっぱいに広げる。</summary>
        public static RectTransform Fill(this RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image MakeImage(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        public static Text MakeText(Transform parent, string text, int size, Color color,
                                    TextAnchor align = TextAnchor.MiddleCenter, bool shadow = true)
        {
            var rt = Node("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Res.Font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.05f;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0, 0, 0, 0.6f);
                s.effectDistance = new Vector2(2, -2);
            }
            return t;
        }

        public static Button MakeButton(Transform parent, string label, Vector2 size, System.Action onClick, int fontSize = 38)
        {
            var img = MakeImage(parent, "Button", Res.GetSprite("UI/Button"), Color.white, true);
            img.rectTransform.sizeDelta = size;
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.88f, 0.78f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.onClick.AddListener(() => { Sfx.Play("Click"); onClick?.Invoke(); });

            var t = MakeText(img.transform, label, fontSize, Cream);
            t.rectTransform.Fill();
            return btn;
        }

        /// <summary>アイコンと文字を横に並べた小さな表示。</summary>
        public static Text IconLabel(Transform parent, string icon, Color iconColor, string text, int size, Vector2 pos, float width)
        {
            var row = Node("IconLabel", parent).At(pos.x, pos.y, width, size + 12);
            var ic = MakeImage(row, "Icon", Res.GetSprite("UI/" + icon), iconColor);
            ic.rectTransform.Set(new Vector2(0, 0.5f), new Vector2(size * 0.6f, 0), new Vector2(size * 1.1f, size * 1.1f));
            ic.preserveAspect = true;
            var t = MakeText(row, text, size, Cream, TextAnchor.MiddleLeft);
            t.rectTransform.Fill();
            t.rectTransform.offsetMin = new Vector2(size * 1.4f, 0);
            return t;
        }
    }
}
