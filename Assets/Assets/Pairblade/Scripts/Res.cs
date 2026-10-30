using System.Collections.Generic;
using UnityEngine;

namespace Pairblade
{
    /// <summary>
    /// Resources/Pairblade/ 以下の素材を名前で読み込む。
    /// 一度読んだものは覚えておき、2回目からはすぐ返す。
    /// </summary>
    public static class Res
    {
        const string Root = "Pairblade/";
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static Font font;

        public static Sprite GetSprite(string path)
        {
            if (sprites.TryGetValue(path, out Sprite s)) return s;

            s = Resources.Load<Sprite>(Root + path);
            if (s == null)
            {
                // 画像が「Sprite」として取り込まれていなかった場合の保険
                var tex = Resources.Load<Texture2D>(Root + path);
                if (tex != null)
                {
                    if (path.StartsWith("Enemies/")) tex.filterMode = FilterMode.Point;
                    s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
            if (s == null) Debug.LogWarning("[Pairblade] 画像が見つかりません: Resources/" + Root + path);
            sprites[path] = s;
            return s;
        }

        public static AudioClip GetClip(string name)
        {
            if (clips.TryGetValue(name, out AudioClip c)) return c;
            c = Resources.Load<AudioClip>(Root + "Audio/" + name);
            clips[name] = c;
            return c;
        }

        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<Font>(Root + "Fonts/ShipporiMincho-Bold");
                    if (font == null)
                    {
                        Debug.LogWarning("[Pairblade] フォントが見つからないため、標準フォントを使います（日本語が表示されません）");
                        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                }
                return font;
            }
        }
    }
}
