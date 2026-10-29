using UnityEngine;

namespace TreadmillMayhem
{
    public enum Sfx { Bump, Pickup, Beep, Go, Out, Fall, Whoosh, Shockwave, Coin, Click, Orb, Accel, SuperReady, SuperGo, Cheer, Shutter }

    /// <summary>
    /// Audio manager with master / music / SFX volume. Every sound has a synthesised placeholder so the game
    /// is never silent without assets; assign real clips to the matching override fields to replace them.
    /// </summary>
    public class AudioFX : MonoBehaviour
    {
        static AudioFX instance;
        const int Rate = 44100;

        [Header("Optional real clips (leave empty to use synthesised placeholders)")]
        public AudioClip musicOverride;
        public AudioClip crowdOverride;

        AudioSource sfx, hum, music, crowd;
        AudioClip[] clips;
        Treadmill treadmill;
        float nextShutter;

        public static void Play(Sfx s, float volume = 1f, float pitch = 1f)
        {
            if (instance == null) return;
            instance.sfx.pitch = pitch * Random.Range(0.94f, 1.06f);
            instance.sfx.PlayOneShot(instance.clips[(int)s], volume);
        }

        /// <summary>Applies the saved volume settings (called on start and from the Settings screen).</summary>
        public static void ApplyVolumes()
        {
            var d = SaveSystem.Data;
            AudioListener.volume = d.masterVolume;
            if (instance == null) return;
            instance.sfx.volume = d.sfxVolume;
            instance.music.volume = 0.35f * d.musicVolume;
            instance.crowd.volume = 0.12f * d.sfxVolume;
        }

        void Awake()
        {
            instance = this;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.ignoreListenerPause = true; // pause-menu clicks stay audible

            clips = new AudioClip[System.Enum.GetValues(typeof(Sfx)).Length];
            clips[(int)Sfx.Bump] = Make("bump", 0.18f, (t, p) => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(140f, 55f, p) * t) * Decay(p, 6f) + Noise() * 0.25f * Decay(p, 20f));
            clips[(int)Sfx.Pickup] = Make("pickup", 0.3f, (t, p) => Square(t, p < 0.33f ? 660f : p < 0.66f ? 880f : 1320f) * 0.35f * Decay(p, 2f));
            clips[(int)Sfx.Beep] = Make("beep", 0.15f, (t, p) => Mathf.Sin(2f * Mathf.PI * 880f * t) * Decay(p, 3f));
            clips[(int)Sfx.Go] = Make("go", 0.35f, (t, p) => (Mathf.Sin(2f * Mathf.PI * 1320f * t) + Square(t, 660f) * 0.3f) * 0.7f * Decay(p, 2.5f));
            clips[(int)Sfx.Out] = Make("out", 0.5f, (t, p) => Square(t, Mathf.Lerp(600f, 120f, p)) * 0.3f * (1f - p));
            clips[(int)Sfx.Fall] = Make("fall", 0.4f, (t, p) => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(700f, 200f, p) * t) * 0.4f * (1f - p));
            clips[(int)Sfx.Whoosh] = Make("whoosh", 0.4f, (t, p) => Noise() * Mathf.Sin(p * Mathf.PI) * 0.4f);
            clips[(int)Sfx.Shockwave] = Make("shock", 0.5f, (t, p) => (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(90f, 30f, p) * t) + Noise() * 0.5f) * Decay(p, 4f));
            clips[(int)Sfx.Coin] = Make("coin", 0.18f, (t, p) => Mathf.Sin(2f * Mathf.PI * (p < 0.3f ? 1568f : 2093f) * t) * 0.6f * Decay(p, 3f));
            clips[(int)Sfx.Click] = Make("click", 0.06f, (t, p) => Square(t, 1200f) * 0.3f * Decay(p, 8f));
            clips[(int)Sfx.Orb] = Make("orb", 0.35f, (t, p) => (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(700f, 1400f, p) * t) + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(1050f, 2100f, p) * t) * 0.5f) * 0.45f * Decay(p, 3f));
            clips[(int)Sfx.Accel] = Make("accel", 0.45f, (t, p) => Saw(t, Mathf.Lerp(220f, 660f, p)) * 0.18f * Mathf.Sin(p * Mathf.PI));
            clips[(int)Sfx.SuperReady] = Make("superready", 0.6f, (t, p) => Square(t, p < 0.25f ? 523f : p < 0.5f ? 659f : p < 0.75f ? 784f : 1047f) * 0.25f * Decay(p, 1f));
            clips[(int)Sfx.SuperGo] = Make("supergo", 0.8f, (t, p) => (Saw(t, Mathf.Lerp(110f, 440f, p)) * 0.3f + Noise() * 0.2f) * Mathf.Sin(p * Mathf.PI));
            clips[(int)Sfx.Cheer] = Make("cheer", 1.2f, (t, p) => Noise() * 0.35f * Mathf.Sin(p * Mathf.PI) * (0.7f + 0.3f * Mathf.Sin(t * 37f)));
            clips[(int)Sfx.Shutter] = Make("shutter", 0.09f, (t, p) => Noise() * (p < 0.25f || (p > 0.55f && p < 0.75f) ? 0.6f : 0f));

            hum = gameObject.AddComponent<AudioSource>();
            hum.clip = Make("hum", 1f, (t, p) => (Saw(t, 55f) * 0.5f + Saw(t, 110f) * 0.25f) * 0.5f);
            hum.loop = true;
            hum.volume = 0f;
            hum.Play();

            music = gameObject.AddComponent<AudioSource>();
            music.clip = musicOverride != null ? musicOverride : MakeMusic();
            music.loop = true;
            music.Play();

            crowd = gameObject.AddComponent<AudioSource>();
            crowd.clip = crowdOverride != null ? crowdOverride : MakeChatter();
            crowd.loop = true;
            crowd.Play();

            ApplyVolumes();
        }

        void Start()
        {
            treadmill = FindAnyObjectByType<Treadmill>();
            nextShutter = Time.time + Random.Range(2f, 5f);
        }

        void Update()
        {
            if (treadmill != null)
            {
                hum.pitch = 0.6f + treadmill.speed * 0.09f;
                float target = treadmill.speed > 0.1f ? (0.07f + treadmill.speed * 0.008f) * SaveSystem.Data.sfxVolume : 0f;
                hum.volume = Mathf.Lerp(hum.volume, target, Time.unscaledDeltaTime * 3f);
            }
            // The crowd keeps snapping photos.
            if (Time.time >= nextShutter)
            {
                nextShutter = Time.time + Random.Range(1.5f, 5f);
                Play(Sfx.Shutter, 0.15f, Random.Range(0.9f, 1.2f));
            }
        }

        static float Decay(float p, float k) => Mathf.Exp(-p * k);
        static float Noise() => Random.value * 2f - 1f;
        static float Square(float t, float f) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t));
        static float Saw(float t, float f) => 2f * (t * f - Mathf.Floor(t * f + 0.5f));

        static AudioClip Make(string name, float seconds, System.Func<float, float, float> wave)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float edge = Mathf.Clamp01(i / 200f) * Mathf.Clamp01((n - i) / 200f);
                data[i] = Mathf.Clamp(wave(t, (float)i / n), -1f, 1f) * edge;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Bouncy 8-second loop: bass on the beat plus a plucky arpeggio over I–V–vi–IV.</summary>
        static AudioClip MakeMusic()
        {
            const float bpm = 120f, seconds = 8f;
            float beat = 60f / bpm;
            float[][] chords = { new[] { 261.6f, 329.6f, 392f }, new[] { 196f, 246.9f, 293.7f }, new[] { 220f, 261.6f, 329.6f }, new[] { 174.6f, 220f, 261.6f } };
            return Make("music", seconds, (t, p) =>
            {
                int bar = Mathf.Min(3, (int)(t / (beat * 4f)));
                var c = chords[bar];
                float inBeat = (t % beat) / beat;
                float bass = Mathf.Sin(2f * Mathf.PI * c[0] * 0.5f * t) * Mathf.Exp(-inBeat * 4f) * 0.5f;
                int step = (int)(t / (beat * 0.5f));
                float inStep = (t % (beat * 0.5f)) / (beat * 0.5f);
                float note = c[step % 3] * 2f;
                float arp = Square(t, note) * 0.12f * Mathf.Exp(-inStep * 6f);
                return (bass + arp) * 0.8f;
            });
        }

        /// <summary>Soft murmur: band-limited noise with a slow swell.</summary>
        static AudioClip MakeChatter()
        {
            float lp = 0f;
            return Make("crowd", 6f, (t, p) =>
            {
                lp = Mathf.Lerp(lp, Noise(), 0.08f);
                return lp * (0.6f + 0.4f * Mathf.Sin(t * 1.3f) * Mathf.Sin(t * 0.7f + 1f));
            });
        }
    }
}
