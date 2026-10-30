using System;
using System.Collections;
using UnityEngine;

namespace Pairblade
{
    /// <summary>
    /// 簡単な動き（トゥイーン）用の道具。外部アセットなしでアニメーションさせる。
    /// </summary>
    public static class Tw
    {
        /// <summary>dur 秒かけて t を 0→1 に進め、毎フレーム step(t) を呼ぶ。</summary>
        public static IEnumerator Lerp(float dur, Action<float> step)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                step(Mathf.Clamp01(t / dur));
                yield return null;
            }
            step(1f);
        }

        public static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds) { t += Time.deltaTime; yield return null; }
        }

        public static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        public static float EaseIn(float t) => t * t;
        public static float EaseInOut(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        /// <summary>0→1→0 と山なりに動く値（跳ねる演出用）。</summary>
        public static float Bump(float t) => Mathf.Sin(t * Mathf.PI);
    }
}
