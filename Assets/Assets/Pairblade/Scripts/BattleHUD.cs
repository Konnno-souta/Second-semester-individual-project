using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Pairblade
{
    /// <summary>
    /// 戦闘画面の右側パネル（敵・HP・攻撃までのカウント・ログ・説明）と、演出（数字のポップアップや揺れ）。
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        RectTransform panel;
        RectTransform effects;       // ポップアップを置く最前面のレイヤー
        RectTransform shakeTarget;   // 画面ごと揺らす対象

        Text floorText, nameText, countText, attackText, traitText, shieldText, comboText, logText;
        Image portrait, portraitFlash, countIcon, screenFlash;
        RectTransform portraitRoot;
        Bar enemyBar, playerBar;

        public static BattleHUD Create(RectTransform stage, RectTransform effects)
        {
            var root = UI.Node("HUD", stage).Fill();
            var hud = root.gameObject.AddComponent<BattleHUD>();
            hud.effects = effects;
            hud.shakeTarget = stage;
            hud.Build(root);
            return hud;
        }

        void Build(RectTransform root)
        {
            var bg = UI.MakeImage(root, "Panel", Res.GetSprite("UI/Panel"), Color.white);
            panel = bg.rectTransform.At(625, 0, 590, 1010);

            floorText = UI.MakeText(panel, "", 28, UI.Gold);
            floorText.rectTransform.At(0, 462, 520, 40);
            nameText = UI.MakeText(panel, "", 50, UI.Cream);
            nameText.rectTransform.At(0, 410, 540, 64);

            // 敵の絵
            portraitRoot = UI.Node("Portrait", panel).At(0, 218, 330, 330);
            var glow = UI.MakeImage(portraitRoot, "Glow", Res.GetSprite("UI/Glow"), new Color(0.95f, 0.85f, 0.6f, 0.18f));
            glow.rectTransform.At(0, 0, 420, 420);
            portrait = UI.MakeImage(portraitRoot, "Image", null, Color.white);
            portrait.rectTransform.Fill();
            portrait.preserveAspect = true;
            portraitFlash = UI.MakeImage(portraitRoot, "Flash", null, new Color(1, 1, 1, 0));
            portraitFlash.rectTransform.Fill();
            portraitFlash.preserveAspect = true;

            enemyBar = Bar.Create(panel, new Vector2(0, 30), new Vector2(480, 34), UI.Red);

            // 攻撃までのカウント
            countIcon = UI.MakeImage(panel, "Hourglass", Res.GetSprite("UI/Hourglass"), UI.Gold);
            countIcon.rectTransform.At(-200, -32, 46, 46);
            countText = UI.MakeText(panel, "", 30, UI.Cream, TextAnchor.MiddleLeft);
            countText.rectTransform.At(-30, -32, 300, 50);
            var sword = UI.MakeImage(panel, "Sword", Res.GetSprite("UI/Sword"), UI.Red);
            sword.rectTransform.At(140, -32, 42, 42);
            attackText = UI.MakeText(panel, "", 34, UI.Cream, TextAnchor.MiddleLeft);
            attackText.rectTransform.At(210, -32, 90, 50);

            traitText = UI.MakeText(panel, "", 23, UI.Dim);
            traitText.rectTransform.At(0, -80, 520, 36);

            var line = UI.MakeImage(panel, "Line", Res.GetSprite("UI/Square"), new Color(UI.Gold.r, UI.Gold.g, UI.Gold.b, 0.5f));
            line.rectTransform.At(0, -112, 500, 2);

            // プレイヤー
            var you = UI.MakeText(panel, "あなた", 30, UI.Cream, TextAnchor.MiddleLeft);
            you.rectTransform.At(0, -145, 480, 40);
            playerBar = Bar.Create(panel, new Vector2(0, -190), new Vector2(480, 34), UI.Green);
            shieldText = UI.IconLabel(panel, "Shield", UI.Ice, "盾 0", 30, new Vector2(-120, -240), 240);
            comboText = UI.MakeText(panel, "", 28, UI.Gold, TextAnchor.MiddleRight);
            comboText.rectTransform.At(100, -240, 320, 44);

            logText = UI.MakeText(panel, "", 24, UI.Cream);
            logText.rectTransform.At(0, -295, 520, 70);

            // カードの効果の説明
            var legendTitle = UI.MakeText(panel, "─ 揃えたカードの効果 ─", 22, UI.Gold);
            legendTitle.rectTransform.At(0, -350, 500, 30);
            LegendItem("Spade", UI.Cream, "攻撃", "数字ぶん", -190, -405);
            LegendItem("Diamond", UI.Red, "魔法", "7割・装甲無視", -63, -405);
            LegendItem("Club", UI.Cream, "防御", "数字ぶんの盾", 63, -405);
            LegendItem("Heart", UI.Red, "回復", "数字の半分", 190, -405);
            var note = UI.MakeText(panel, "A=11  J・Q・K=10  ジョーカー2枚で炎20", 21, UI.Dim);
            note.rectTransform.At(0, -470, 520, 30);

            // 画面全体のフラッシュ（被弾時に赤く光る）
            screenFlash = UI.MakeImage(effects, "ScreenFlash", Res.GetSprite("UI/Square"), new Color(0.8f, 0.1f, 0.05f, 0f));
            screenFlash.rectTransform.Fill();
        }

        void LegendItem(string icon, Color c, string title, string desc, float x, float y)
        {
            var img = UI.MakeImage(panel, icon, Res.GetSprite("UI/" + icon), c);
            img.rectTransform.At(x, y + 20, 30, 30);
            var t = UI.MakeText(panel, title + "\n<size=17>" + desc + "</size>", 20, UI.Cream);
            t.rectTransform.At(x, y - 18, 126, 50);
        }

        // ---------- 表示の更新 ----------

        public void SetEnemy(EnemyData e, int floor, int floorCount)
        {
            floorText.text = floor == floorCount ? "最終層" : "第 " + floor + " 層 ／ " + floorCount;
            nameText.text = e.displayName;
            portrait.sprite = e.GetPortrait();
            portraitFlash.sprite = portrait.sprite;
            portrait.color = Color.white;
            portraitRoot.anchoredPosition = new Vector2(0, 218);

            string traits = "";
            if (e.armor > 0) traits += "装甲" + e.armor + "（♠を軽減）";
            string skill = e.swapsCards && e.freezesCards ? "攻撃後にカード入替＋凍結"
                         : e.swapsCards ? "攻撃後にカードを入れ替える"
                         : e.freezesCards ? "攻撃後にカードを凍らせる" : "";
            if (skill.Length > 0) traits += (traits.Length > 0 ? "　" : "") + skill;
            traitText.text = traits;
            traitText.fontSize = traits.Length > 20 ? 21 : 23;
        }

        public void SetEnemyHp(int hp, int max, bool instant = false) => enemyBar.Set(Mathf.Max(0, hp), max, instant);
        public void SetPlayerHp(int hp, int max, bool instant = false) => playerBar.Set(Mathf.Max(0, hp), max, instant);

        public void SetCountdown(int turns, int attack)
        {
            countText.text = "攻撃まで <size=40>" + turns + "</size>";
            countText.color = turns <= 1 ? UI.Red : UI.Cream;
            countIcon.color = turns <= 1 ? UI.Red : UI.Gold;
            attackText.text = attack.ToString();
        }

        public void SetShield(int shield)
        {
            shieldText.text = "盾 " + shield;
            shieldText.color = shield > 0 ? UI.Ice : UI.Dim;
        }

        public void SetCombo(int combo, float multiplier)
        {
            comboText.text = combo >= 2 ? combo + " コンボ ×" + multiplier.ToString("0.##") : "";
        }

        public void Log(string message) { logText.text = message; }

        // ---------- 演出 ----------

        public Vector3 EnemyPosition => portraitRoot.position;
        public Vector3 PlayerPosition => playerBar.transform.position;
        public Vector3 ShieldPosition => shieldText.transform.position;

        /// <summary>world 座標の位置に、数字や文字をふわっと出して消す。</summary>
        public void Popup(Vector3 worldPos, string text, Color color, int size = 56)
        {
            StartCoroutine(PopupRoutine(worldPos, text, color, size));
        }

        IEnumerator PopupRoutine(Vector3 worldPos, string text, Color color, int size)
        {
            var t = UI.MakeText(effects, text, size, color);
            t.rectTransform.sizeDelta = new Vector2(400, 100);
            t.transform.position = worldPos;
            var outline = t.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(2, -2);
            Vector2 start = t.rectTransform.anchoredPosition + new Vector2(Random.Range(-20f, 20f), 0);
            yield return Tw.Lerp(0.9f, k =>
            {
                t.rectTransform.anchoredPosition = start + new Vector2(0, 90f * Tw.EaseOut(k));
                float s = 1f + 0.35f * Tw.Bump(Mathf.Min(1f, k * 3f));
                t.rectTransform.localScale = Vector3.one * s;
                var c = color; c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                t.color = c;
            });
            Destroy(t.gameObject);
        }

        public IEnumerator EnemyHurt()
        {
            Vector2 home = new Vector2(0, 218);
            yield return Tw.Lerp(0.3f, t =>
            {
                portraitRoot.anchoredPosition = home + new Vector2(Mathf.Sin(t * Mathf.PI * 7f) * 14f * (1f - t), 0);
                portraitFlash.color = new Color(1, 1, 1, 0.85f * (1f - t));
            });
            portraitRoot.anchoredPosition = home;
        }

        public IEnumerator EnemyLunge()
        {
            yield return Tw.Lerp(0.14f, t => portraitRoot.localScale = Vector3.one * (1f + 0.25f * Tw.EaseIn(t)));
            yield return Tw.Lerp(0.2f, t => portraitRoot.localScale = Vector3.one * (1.25f - 0.25f * Tw.EaseOut(t)));
        }

        public IEnumerator EnemyDefeated()
        {
            Vector2 home = portraitRoot.anchoredPosition;
            yield return Tw.Lerp(0.7f, t =>
            {
                portrait.color = new Color(1, 1, 1, 1f - t);
                portraitRoot.anchoredPosition = home + new Vector2(0, -40f * t);
            });
        }

        public IEnumerator ScreenHit(float strength)
        {
            Vector2 home = shakeTarget.anchoredPosition;
            yield return Tw.Lerp(0.35f, t =>
            {
                float s = strength * (1f - t);
                shakeTarget.anchoredPosition = home + new Vector2(Random.Range(-s, s), Random.Range(-s, s));
                screenFlash.color = new Color(0.8f, 0.1f, 0.05f, 0.35f * (1f - t));
            });
            shakeTarget.anchoredPosition = home;
        }
    }
}
