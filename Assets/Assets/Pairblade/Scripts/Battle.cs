using System;
using System.Collections;
using UnityEngine;

namespace Pairblade
{
    /// <summary>
    /// 1回の戦闘の進行。
    /// 「2枚めくる → 当たり外れ → 効果 → 敵のカウントを減らす → 0なら敵の攻撃」を1ターンとして繰り返す。
    /// </summary>
    public class Battle : MonoBehaviour
    {
        public const float DiamondRate = 0.7f;
        public const int JokerDamage = 20;

        GameManager gm;
        EnemyData enemy;
        PlayerState player;
        Board board;
        BattleHUD hud;

        int enemyHp;
        int countdown;
        int shield;
        int combo;

        public Action<bool> Finished;   // true = 勝ち

        int Interval => enemy.interval + player.ExtraInterval;
        float ComboMultiplier => 1f + player.ComboStep * Mathf.Max(0, combo - 1);

        public void Begin(GameManager gm, EnemyData enemy, PlayerState player, Board board, BattleHUD hud)
        {
            this.gm = gm;
            this.enemy = enemy;
            this.player = player;
            this.board = board;
            this.hud = hud;

            enemyHp = enemy.maxHp;
            countdown = Interval;
            shield = 0;
            combo = 0;

            board.PairChosen = OnPairChosen;
            RefreshHud(true);
            hud.Log("2枚めくって、同じ数字をそろえよう");
            StartCoroutine(DealRoutine());
        }

        bool ShouldAddJoker() => player.AlwaysJoker || UnityEngine.Random.value < gm.jokerChance;

        IEnumerator DealRoutine()
        {
            yield return board.Deal(enemy.SafeCardCount, ShouldAddJoker(), player.Peek ? gm.peekSeconds : 0f);
            board.InputEnabled = true;
        }

        void RefreshHud(bool instant = false)
        {
            hud.SetEnemyHp(enemyHp, enemy.maxHp, instant);
            hud.SetPlayerHp(player.Hp, player.MaxHp, instant);
            hud.SetCountdown(countdown, enemy.attack);
            hud.SetShield(shield);
            hud.SetCombo(combo, ComboMultiplier);
        }

        void OnPairChosen(CardView a, CardView b)
        {
            StartCoroutine(ResolveTurn(a, b));
        }

        IEnumerator ResolveTurn(CardView a, CardView b)
        {
            yield return Tw.Wait(0.35f);   // 2枚目がめくれ終わるのを待つ
            player.TotalTurns++;

            if (a.Info.Matches(b.Info))
            {
                combo++;
                player.TotalMatches++;
                player.BestCombo = Mathf.Max(player.BestCombo, combo);
                Sfx.Play("Match");
                StartCoroutine(a.PlayMatched());
                StartCoroutine(b.PlayMatched());
                yield return Tw.Wait(0.2f);
                yield return ApplyPair(a, b);
            }
            else
            {
                combo = 0;
                Sfx.Play("Miss");
                hud.Log("はずれ… " + a.Info.Label + " と " + b.Info.Label);
                yield return Tw.Wait(gm.mismatchShowSeconds);
                StartCoroutine(a.Wiggle());
                yield return b.Wiggle();
                StartCoroutine(a.Flip(false));
                yield return b.Flip(false);
            }
            RefreshHud();

            if (enemyHp <= 0)
            {
                yield return Victory();
                yield break;
            }

            // 敵のターン
            board.TickFrozen();
            countdown--;
            hud.SetCountdown(countdown, enemy.attack);
            if (countdown <= 0)
            {
                yield return EnemyAttack();
                if (player.Hp <= 0)
                {
                    yield return Defeat();
                    yield break;
                }
                countdown = Interval;
                RefreshHud();
            }

            if (board.AllMatched)
            {
                hud.Log("全部そろった！ 新しく配り直します");
                yield return Tw.Wait(0.4f);
                yield return board.Deal(enemy.SafeCardCount, ShouldAddJoker(), player.Peek ? gm.peekSeconds : 0f);
            }

            board.EnsurePlayable();
            board.InputEnabled = true;
        }

        /// <summary>揃った2枚それぞれのマークの効果を順番に発動する。</summary>
        IEnumerator ApplyPair(CardView a, CardView b)
        {
            float mult = ComboMultiplier;
            string comboNote = combo >= 2 ? "（" + combo + "コンボ ×" + mult.ToString("0.##") + "）" : "";

            if (a.Info.IsJoker)
            {
                int dmg = Mathf.RoundToInt(JokerDamage * mult);
                Sfx.Play("Joker");
                hud.Log("ジョーカー！ 炎で " + dmg + " ダメージ" + comboNote);
                yield return DamageEnemy(dmg, new Color(1f, 0.55f, 0.2f), 72);
                yield break;
            }

            string log = a.Info.Label + " ＆ " + b.Info.Label + "：";
            int totalDamage = 0, totalShield = 0, totalHeal = 0;
            foreach (var card in new[] { a, b })
            {
                var info = card.Info;
                int v = info.Value;
                switch (info.Suit)
                {
                    case Suit.Spade:
                    {
                        int dmg = Mathf.RoundToInt(Mathf.Max(1, v + player.SpadeBonus - enemy.armor) * mult);
                        totalDamage += dmg;
                        Sfx.Play("Slash");
                        hud.Popup(card.Rect.position, "♠" + dmg, UI.Cream, 48);
                        yield return DamageEnemy(dmg, UI.Cream, 60);
                        break;
                    }
                    case Suit.Diamond:
                    {
                        int dmg = Mathf.RoundToInt((Mathf.CeilToInt(v * DiamondRate) + player.DiamondBonus) * mult);
                        totalDamage += dmg;
                        Sfx.Play("Magic");
                        hud.Popup(card.Rect.position, "♦" + dmg, UI.Red, 48);
                        yield return DamageEnemy(dmg, new Color(1f, 0.6f, 0.55f), 60);
                        break;
                    }
                    case Suit.Club:
                    {
                        int s = v + player.ClubBonus;
                        shield += s;
                        totalShield += s;
                        Sfx.Play("Guard");
                        hud.Popup(card.Rect.position, "♣+" + s, UI.Ice, 48);
                        hud.Popup(hud.ShieldPosition, "盾 +" + s, UI.Ice, 40);
                        hud.SetShield(shield);
                        yield return Tw.Wait(0.3f);
                        break;
                    }
                    case Suit.Heart:
                    {
                        int h = Mathf.CeilToInt(v / 2f) + player.HeartBonus;
                        int before = player.Hp;
                        player.Heal(h);
                        totalHeal += player.Hp - before;
                        Sfx.Play("Heal");
                        hud.Popup(card.Rect.position, "♥+" + h, UI.Red, 48);
                        hud.Popup(hud.PlayerPosition, "+" + (player.Hp - before), UI.Green, 44);
                        hud.SetPlayerHp(player.Hp, player.MaxHp);
                        yield return Tw.Wait(0.3f);
                        break;
                    }
                }
                if (enemyHp <= 0) break;
            }

            if (totalDamage > 0) log += " " + totalDamage + "ダメージ";
            if (totalShield > 0) log += " 盾+" + totalShield;
            if (totalHeal > 0) log += " 回復" + totalHeal;
            hud.Log(log + comboNote);
        }

        IEnumerator DamageEnemy(int amount, Color color, int size)
        {
            enemyHp -= amount;
            hud.SetEnemyHp(enemyHp, enemy.maxHp);
            hud.Popup(hud.EnemyPosition, amount.ToString(), color, size);
            yield return hud.EnemyHurt();
        }

        IEnumerator EnemyAttack()
        {
            yield return Tw.Wait(0.2f);
            yield return hud.EnemyLunge();

            int blocked = Mathf.Min(shield, enemy.attack);
            int damage = enemy.attack - blocked;
            shield = 0;

            if (damage > 0)
            {
                Sfx.Play("Hurt");
                player.Hp -= damage;
                hud.Popup(hud.PlayerPosition, "-" + damage, UI.Red, 60);
                StartCoroutine(hud.ScreenHit(18f));
            }
            else
            {
                Sfx.Play("Guard");
            }
            if (blocked > 0) hud.Popup(hud.ShieldPosition, "防いだ " + blocked, UI.Ice, 40);

            hud.Log(enemy.displayName + "の攻撃！ " + (damage > 0 ? damage + " ダメージ" : "すべて防いだ") +
                    (blocked > 0 && damage > 0 ? "（盾で" + blocked + "防いだ）" : ""));
            RefreshHud();
            yield return Tw.Wait(0.5f);

            if (player.Hp <= 0) yield break;

            if (enemy.swapsCards)
            {
                hud.Log(enemy.displayName + "がカードを入れ替えた！");
                yield return board.SwapRandom();
                yield return Tw.Wait(0.15f);
            }
            if (enemy.freezesCards)
            {
                hud.Log(enemy.displayName + "がカードを凍らせた！");
                board.FreezeRandom(2, gm.freezeTurns);
                yield return Tw.Wait(0.4f);
            }
        }

        IEnumerator Victory()
        {
            board.InputEnabled = false;
            hud.Log(enemy.displayName + "をたおした！");
            Sfx.Play("Win");
            yield return hud.EnemyDefeated();
            yield return Tw.Wait(0.8f);
            Finished?.Invoke(true);
        }

        IEnumerator Defeat()
        {
            board.InputEnabled = false;
            hud.Log("力尽きた…");
            Sfx.Play("Lose");
            yield return Tw.Wait(1.4f);
            Finished?.Invoke(false);
        }
    }
}
