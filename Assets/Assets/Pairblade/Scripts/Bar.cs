using UnityEngine;
using UnityEngine.UI;

namespace Pairblade
{
    /// <summary>
    /// HPバー。減ったときは白い「残像」が少し遅れて追いかける。
    /// </summary>
    public class Bar : MonoBehaviour
    {
        Image fill;
        Image ghost;
        Text label;
        float target = 1f;
        float shown = 1f;
        float ghostShown = 1f;

        public static Bar Create(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var border = UI.MakeImage(parent, "Bar", Res.GetSprite("UI/Square"), UI.Gold);
            border.rectTransform.At(pos.x, pos.y, size.x + 4, size.y + 4);
            var bar = border.gameObject.AddComponent<Bar>();

            var bg = UI.MakeImage(border.transform, "Back", Res.GetSprite("UI/Square"), new Color(0.06f, 0.05f, 0.05f, 1f));
            bg.rectTransform.Fill();
            bg.rectTransform.offsetMin = new Vector2(2, 2);
            bg.rectTransform.offsetMax = new Vector2(-2, -2);

            bar.ghost = MakeFill(bg.transform, new Color(1f, 0.95f, 0.85f, 0.85f));
            bar.fill = MakeFill(bg.transform, color);

            bar.label = UI.MakeText(bg.transform, "", Mathf.RoundToInt(size.y * 0.72f), Color.white);
            bar.label.rectTransform.Fill();
            return bar;
        }

        static Image MakeFill(Transform parent, Color c)
        {
            var img = UI.MakeImage(parent, "Fill", Res.GetSprite("UI/Square"), c);
            img.rectTransform.Fill();
            img.rectTransform.offsetMin = new Vector2(2, 2);
            img.rectTransform.offsetMax = new Vector2(-2, -2);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            return img;
        }

        public void SetColor(Color c) { fill.color = c; }

        public void Set(int value, int max, bool instant = false)
        {
            target = max > 0 ? Mathf.Clamp01(value / (float)max) : 0f;
            label.text = value + " / " + max;
            if (instant) { shown = target; ghostShown = target; }
        }

        void Update()
        {
            shown = Mathf.MoveTowards(shown, target, Time.deltaTime * 2.5f);
            if (ghostShown < shown) ghostShown = shown;
            else ghostShown = Mathf.MoveTowards(ghostShown, shown, Time.deltaTime * 0.6f);
            fill.fillAmount = shown;
            ghost.fillAmount = ghostShown;
        }
    }
}
