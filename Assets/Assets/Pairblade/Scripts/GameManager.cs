using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pairblade
{
    /// <summary>
    /// ゲーム全体の司令塔。シーンの空の GameObject にこれを1つ付けるだけで動く。
    /// タイトル → 戦闘 → 強化を選ぶ → 戦闘 … → エンディング の流れを管理する。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("プレイヤー")]
        [Min(1)] public int playerMaxHp = 50;
        [Tooltip("敵を倒すたびに回復する量")]
        [Min(0)] public int healAfterBattle = 10;
        [Tooltip("コンボ1段ごとにダメージ倍率がどれだけ上がるか")]
        [Range(0f, 1f)] public float comboStep = 0.2f;

        [Header("ルール")]
        [Tooltip("配るときにジョーカーのペアが入る確率")]
        [Range(0f, 1f)] public float jokerChance = 0.25f;
        [Tooltip("強化「鷹の目」で全カードを見せる秒数")]
        [Min(0f)] public float peekSeconds = 1f;
        [Tooltip("凍結が続くターン数")]
        [Min(1)] public int freezeTurns = 3;
        [Tooltip("はずれたカードを表のまま見せておく秒数")]
        [Min(0.1f)] public float mismatchShowSeconds = 0.6f;

        [Header("敵（上から順に戦う。最後がボス）")]
        public List<EnemyData> enemies = DefaultEnemies();

        public static List<EnemyData> DefaultEnemies()
        {
            return new List<EnemyData>
            {
                //             名前            画像        HP  攻撃 間隔 装甲 入替  凍結  枚数
                new EnemyData("スライム",       "Slime",     40,  7,  3,  0, false, false, 12),
                new EnemyData("コウモリ",       "Bat",       60,  9,  3,  0, true,  false, 16),
                new EnemyData("がいこつ剣士",   "Skeleton",  85, 12,  4,  3, false, false, 16),
                new EnemyData("氷の亡霊",       "Wraith",    95, 11,  3,  0, false, true,  20),
                new EnemyData("ディーラー",     "Dealer",   150, 14,  4,  2, true,  true,  24),
            };
        }

        RectTransform canvasRoot;
        RectTransform screen;     // いま表示している画面（切り替えるたびに作り直す）
        Image fader;
        PlayerState player;
        int floorIndex;
        bool transitioning;

        void Awake()
        {
            BuildCanvas();
            Sfx.Init(gameObject);
        }

        void Start()
        {
            ShowTitle();
        }

        // ---------- 土台 ----------

        void BuildCanvas()
        {
            var go = new GameObject("Pairblade Canvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            canvasRoot = (RectTransform)go.transform;

            var table = UI.MakeImage(canvasRoot, "Table", Res.GetSprite("UI/Table"), Color.white);
            table.rectTransform.Fill();

            fader = UI.MakeImage(canvasRoot, "Fader", Res.GetSprite("UI/Square"), Color.black, true);
            fader.rectTransform.Fill();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        /// <summary>暗転 → 画面を作り直す → 明転。</summary>
        void Go(Action<RectTransform> build)
        {
            if (transitioning) return;
            StartCoroutine(GoRoutine(build));
        }

        IEnumerator GoRoutine(Action<RectTransform> build)
        {
            transitioning = true;
            fader.raycastTarget = true;
            float from = fader.color.a;
            yield return Tw.Lerp(from < 0.99f ? 0.3f : 0f, t => fader.color = new Color(0, 0, 0, Mathf.Lerp(from, 1f, t)));

            if (screen != null) Destroy(screen.gameObject);
            screen = UI.Node("Screen", canvasRoot).Fill();
            screen.SetSiblingIndex(1);   // 背景の上、暗転用の幕の下
            build(screen);

            yield return Tw.Lerp(0.35f, t => fader.color = new Color(0, 0, 0, 1f - t));
            fader.raycastTarget = false;
            transitioning = false;
        }

        // ---------- タイトル ----------

        void ShowTitle() => Go(BuildTitle);

        void BuildTitle(RectTransform root)
        {
            // 扇形に広げたカード
            string[] fan = { "Back", "QS", "Joker2", "AH", "Back" };
            for (int i = 0; i < fan.Length; i++)
            {
                float k = i - (fan.Length - 1) / 2f;
                var img = UI.MakeImage(root, "Fan", Res.GetSprite("Cards/" + fan[i]), Color.white);
                var rt = img.rectTransform.At(k * 120f, 190f - Mathf.Abs(k) * 22f, 210, 305);
                rt.localRotation = Quaternion.Euler(0, 0, -k * 11f);
                var sh = img.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0, 0, 0, 0.4f);
                sh.effectDistance = new Vector2(6, -8);
            }

            var title = UI.MakeText(root, "PAIRBLADE", 150, UI.Cream);
            title.rectTransform.At(0, -110, 1400, 180);
            var sub = UI.MakeText(root, "─  神 経 衰 弱 バ ト ル  ─", 40, UI.Gold);
            sub.rectTransform.At(0, -205, 900, 60);

            UI.MakeButton(root, "はじめる", new Vector2(380, 92), StartRun, 44).GetComponent<RectTransform>().At(0, -320, 380, 92);
            var rulesBtn = UI.MakeButton(root, "遊び方", new Vector2(260, 70), null, 32);
            rulesBtn.GetComponent<RectTransform>().At(0, -425, 260, 70);

            if (!Application.isEditor)
            {
                UI.MakeButton(root, "終了", new Vector2(160, 60), Application.Quit, 28)
                  .GetComponent<RectTransform>().Set(new Vector2(1, 0), new Vector2(-110, 60), new Vector2(160, 60));
            }

            // 遊び方の説明（ボタンで開閉）
            var rules = UI.MakeImage(root, "Rules", Res.GetSprite("UI/Panel"), Color.white, true);
            rules.rectTransform.At(0, 0, 1100, 760);
            var heading = UI.MakeText(rules.transform, "遊び方", 52, UI.Gold);
            heading.rectTransform.At(0, 310, 800, 70);
            var body = UI.MakeText(rules.transform,
                "裏向きのカードを2枚めくり、同じ数字がそろえば\nそのカードのマークの効果が発動します。\n\n" +
                "♠ 攻撃　数字ぶんのダメージ（敵の装甲で減る）\n" +
                "♦ 魔法　数字の7割のダメージ（装甲を無視）\n" +
                "♣ 防御　数字ぶんの盾。次の敵の攻撃を受け止める\n" +
                "♥ 回復　数字の半分だけHPが回復\n" +
                "ジョーカー　2枚そろえると炎で20ダメージ\n\n" +
                "A は 11、J・Q・K は 10 として数えます。\n" +
                "続けてそろえるとコンボになり、ダメージが上がります。\n" +
                "敵は「攻撃まで」の数字が0になると攻撃してきます。\n" +
                "盾を張るカードの場所は覚えておき、攻撃の直前にそろえるのがコツ。",
                30, UI.Cream, TextAnchor.UpperLeft);
            body.rectTransform.At(0, -30, 900, 560);
            var close = UI.MakeButton(rules.transform, "閉じる", new Vector2(220, 64), () => rules.gameObject.SetActive(false), 30);
            close.GetComponent<RectTransform>().At(0, -320, 220, 64);
            rules.gameObject.SetActive(false);

            rulesBtn.onClick.AddListener(() => rules.gameObject.SetActive(true));
        }

        // ---------- 戦闘 ----------

        void StartRun()
        {
            player = new PlayerState(playerMaxHp, comboStep);
            floorIndex = 0;
            Go(BuildBattle);
        }

        void BuildBattle(RectTransform root)
        {
            var stage = UI.Node("Stage", root).Fill();
            var effects = UI.Node("Effects", root).Fill();
            var board = Board.Create(stage, new Vector2(-300, -10), new Vector2(1180, 960));
            var hud = BattleHUD.Create(stage, effects);
            var enemy = enemies[floorIndex];
            hud.SetEnemy(enemy, floorIndex + 1, enemies.Count);

            var battle = root.gameObject.AddComponent<Battle>();
            battle.Finished = OnBattleFinished;
            battle.Begin(this, enemy, player, board, hud);
        }

        void OnBattleFinished(bool won)
        {
            if (!won)
            {
                Go(r => BuildEnding(r, false));
                return;
            }
            floorIndex++;
            if (floorIndex >= enemies.Count)
            {
                Go(r => BuildEnding(r, true));
                return;
            }
            player.Heal(healAfterBattle);
            Go(BuildReward);
        }

        // ---------- 強化を選ぶ ----------

        void BuildReward(RectTransform root)
        {
            var beaten = enemies[floorIndex - 1];
            var next = enemies[floorIndex];

            var head = UI.MakeText(root, beaten.displayName + "をたおした！", 64, UI.Cream);
            head.rectTransform.At(0, 390, 1400, 90);
            var sub = UI.MakeText(root, "HPが " + healAfterBattle + " 回復した（HP " + player.Hp + " / " + player.MaxHp + "）　強化を1つ選んでください", 30, UI.Gold);
            sub.rectTransform.At(0, 305, 1500, 50);

            var choices = Upgrades.Roll(player, 3);
            for (int i = 0; i < choices.Count; i++)
            {
                var u = choices[i];
                float x = (i - (choices.Count - 1) / 2f) * 440f;
                var card = UI.MakeImage(root, "Choice", Res.GetSprite("UI/Panel"), Color.white, true);
                card.rectTransform.At(x, -20, 380, 470);
                var btn = card.gameObject.AddComponent<Button>();
                var colors = btn.colors;
                colors.highlightedColor = new Color(1f, 0.9f, 0.75f, 1f);
                btn.colors = colors;

                var icon = UI.MakeImage(card.transform, "Icon", Res.GetSprite("UI/" + u.Icon), u.IconColor);
                icon.rectTransform.At(0, 120, 130, 130);
                icon.preserveAspect = true;
                var name = UI.MakeText(card.transform, u.Name, 42, UI.Cream);
                name.rectTransform.At(0, 10, 340, 60);
                var desc = UI.MakeText(card.transform, u.Description, 30, UI.Cream);
                desc.rectTransform.At(0, -90, 330, 120);
                if (u.Unique)
                {
                    var tag = UI.MakeText(card.transform, "1回限り", 22, UI.Gold);
                    tag.rectTransform.At(0, -185, 200, 30);
                }

                btn.onClick.AddListener(() =>
                {
                    if (transitioning) return;
                    Sfx.Play("Match");
                    player.Apply(u);
                    Go(BuildBattle);
                });
            }

            var nextText = UI.MakeText(root, "次の相手：" + next.displayName + (floorIndex == enemies.Count - 1 ? "（ボス）" : ""), 32, UI.Dim);
            nextText.rectTransform.At(0, -380, 1000, 50);
        }

        // ---------- エンディング ----------

        void BuildEnding(RectTransform root, bool cleared)
        {
            var head = UI.MakeText(root, cleared ? "踏破！" : "力尽きた…", 110, cleared ? UI.Gold : UI.Red);
            head.rectTransform.At(0, 260, 1200, 140);
            var sub = UI.MakeText(root,
                cleared ? "ディーラーを打ち負かし、すべての層を突破した。" : enemies[floorIndex].displayName + "に敗れた。",
                36, UI.Cream);
            sub.rectTransform.At(0, 150, 1400, 60);

            var stats = UI.MakeImage(root, "Stats", Res.GetSprite("UI/Panel"), Color.white);
            stats.rectTransform.At(0, -40, 640, 270);
            int reached = Mathf.Min(floorIndex + (cleared ? 0 : 1), enemies.Count);
            var t = UI.MakeText(stats.transform,
                "到達　　　第 " + reached + " 層 ／ " + enemies.Count + "\n" +
                "ターン数　" + player.TotalTurns + "\n" +
                "そろえた数　" + player.TotalMatches + " ペア\n" +
                "最大コンボ　" + player.BestCombo,
                34, UI.Cream);
            t.rectTransform.At(0, 0, 560, 230);

            UI.MakeButton(root, "もう一度", new Vector2(320, 84), StartRun, 38).GetComponent<RectTransform>().At(-190, -300, 320, 84);
            UI.MakeButton(root, "タイトルへ", new Vector2(320, 84), ShowTitle, 38).GetComponent<RectTransform>().At(190, -300, 320, 84);
        }
    }
}
