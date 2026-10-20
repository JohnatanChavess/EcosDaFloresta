using System.Collections;
using UnityEngine;

namespace Ecos
{
    public enum SfxId
    {
        Jump,
        Collect,
        Hurt,
        Death,
        GameOver,
        Victory,
        Stomp,
        Checkpoint,
        Heart,
        LevelComplete,
        Click,
        Crumble,
        Land
    }

    public enum MusicId
    {
        None,
        Menu,
        Forest,
        Cave,
        Ruins,
        Victory
    }

    /// <summary>
    /// Toca músicas (com transição suave) e efeitos sonoros. Persiste entre cenas.
    /// Cada cena tem uma instância do prefab; duplicatas se destroem automaticamente.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Fontes de áudio")]
        [SerializeField] AudioSource musicSource;
        [SerializeField] int sfxVoices = 8;

        [Header("Volumes")]
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.45f;
        [SerializeField, Range(0f, 1f)] float sfxVolume = 0.8f;

        [Header("Músicas")]
        [SerializeField] AudioClip menuMusic;
        [SerializeField] AudioClip forestMusic;
        [SerializeField] AudioClip caveMusic;
        [SerializeField] AudioClip ruinsMusic;
        [SerializeField] AudioClip victoryMusic;

        [Header("Efeitos sonoros")]
        [SerializeField] AudioClip jumpSfx;
        [SerializeField] AudioClip collectSfx;
        [SerializeField] AudioClip hurtSfx;
        [SerializeField] AudioClip deathSfx;
        [SerializeField] AudioClip gameOverSfx;
        [SerializeField] AudioClip victorySfx;
        [SerializeField] AudioClip stompSfx;
        [SerializeField] AudioClip checkpointSfx;
        [SerializeField] AudioClip heartSfx;
        [SerializeField] AudioClip levelCompleteSfx;
        [SerializeField] AudioClip clickSfx;
        [SerializeField] AudioClip crumbleSfx;
        [SerializeField] AudioClip landSfx;

        AudioSource[] voices;
        int nextVoice;
        MusicId currentMusic = MusicId.None;
        Coroutine musicFade;
        float duckFactor = 1f;

        public bool Muted { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            voices = new AudioSource[Mathf.Max(1, sfxVoices)];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }

            Muted = SaveSystem.Muted;
            AudioListener.volume = Muted ? 0f : 1f;
        }

        void Update()
        {
            if (InputReader.MutePressed) ToggleMute();
        }

        public static void Play(SfxId id, float volume = 1f, float pitchVariation = 0.05f)
        {
            if (Instance != null) Instance.PlaySfx(id, volume, pitchVariation);
        }

        public static void PlayMusic(MusicId id)
        {
            if (Instance != null) Instance.SwitchMusic(id);
        }

        public static void StopMusic(float fadeTime = 0.5f)
        {
            if (Instance != null) Instance.FadeOutMusic(fadeTime);
        }

        /// <summary>Abaixa a música (usado no menu de pausa).</summary>
        public static void Duck(bool ducked)
        {
            if (Instance == null) return;
            Instance.duckFactor = ducked ? 0.35f : 1f;
            if (Instance.musicFade == null) Instance.musicSource.volume = Instance.musicVolume * Instance.duckFactor;
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            SaveSystem.Muted = Muted;
            AudioListener.volume = Muted ? 0f : 1f;
        }

        void PlaySfx(SfxId id, float volume, float pitchVariation)
        {
            var clip = GetClip(id);
            if (clip == null) return;
            var source = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.volume = sfxVolume * volume;
            source.clip = clip;
            source.Play();
        }

        void SwitchMusic(MusicId id)
        {
            if (id == currentMusic && musicSource.isPlaying) return;
            currentMusic = id;
            if (musicFade != null) StopCoroutine(musicFade);
            musicFade = StartCoroutine(CrossFade(GetMusic(id)));
        }

        void FadeOutMusic(float time)
        {
            currentMusic = MusicId.None;
            if (musicFade != null) StopCoroutine(musicFade);
            musicFade = StartCoroutine(FadeOut(time));
        }

        IEnumerator CrossFade(AudioClip next)
        {
            if (musicSource.isPlaying) yield return FadeOut(0.4f);
            if (next == null)
            {
                musicFade = null;
                yield break;
            }
            musicSource.clip = next;
            musicSource.volume = 0f;
            musicSource.Play();
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume * duckFactor, t / 0.6f);
                yield return null;
            }
            musicSource.volume = musicVolume * duckFactor;
            musicFade = null;
        }

        IEnumerator FadeOut(float time)
        {
            float start = musicSource.volume;
            float t = 0f;
            while (t < time && musicSource.isPlaying)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(start, 0f, t / time);
                yield return null;
            }
            musicSource.Stop();
            musicFade = null;
        }

        AudioClip GetMusic(MusicId id)
        {
            switch (id)
            {
                case MusicId.Menu: return menuMusic;
                case MusicId.Forest: return forestMusic;
                case MusicId.Cave: return caveMusic;
                case MusicId.Ruins: return ruinsMusic;
                case MusicId.Victory: return victoryMusic;
                default: return null;
            }
        }

        AudioClip GetClip(SfxId id)
        {
            switch (id)
            {
                case SfxId.Jump: return jumpSfx;
                case SfxId.Collect: return collectSfx;
                case SfxId.Hurt: return hurtSfx;
                case SfxId.Death: return deathSfx;
                case SfxId.GameOver: return gameOverSfx;
                case SfxId.Victory: return victorySfx;
                case SfxId.Stomp: return stompSfx;
                case SfxId.Checkpoint: return checkpointSfx;
                case SfxId.Heart: return heartSfx;
                case SfxId.LevelComplete: return levelCompleteSfx;
                case SfxId.Click: return clickSfx;
                case SfxId.Crumble: return crumbleSfx;
                case SfxId.Land: return landSfx;
                default: return null;
            }
        }
    }
}
