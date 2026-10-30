using UnityEngine;

namespace Pairblade
{
    /// <summary>
    /// 効果音とBGMを鳴らす。Sfx.Play("Flip") のように名前で呼ぶ。
    /// </summary>
    public static class Sfx
    {
        static AudioSource se;
        static AudioSource music;
        public static float Volume = 0.8f;

        public static void Init(GameObject host)
        {
            if (Object.FindAnyObjectByType<AudioListener>() == null)
                host.AddComponent<AudioListener>();

            se = host.AddComponent<AudioSource>();
            se.playOnAwake = false;

            // Resources/Pairblade/Audio/BGM という名前の曲を置くと自動で流れる（任意）
            var bgm = Res.GetClip("BGM");
            if (bgm != null)
            {
                music = host.AddComponent<AudioSource>();
                music.clip = bgm;
                music.loop = true;
                music.volume = 0.35f;
                music.Play();
            }
        }

        public static void Play(string name, float volume = 1f)
        {
            if (se == null) return;
            var clip = Res.GetClip(name);
            if (clip != null) se.PlayOneShot(clip, volume * Volume);
        }
    }
}
