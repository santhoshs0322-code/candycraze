// ============================================================
// AudioManager.cs
// Plays all music and sound effects.
// Persists across scenes via DontDestroyOnLoad.
//
// Usage:
//   AudioManager.Instance.PlaySFX(AudioManager.SFX.Match);
//   AudioManager.Instance.PlayMusic(myClip);
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    public class AudioManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static AudioManager Instance { get; private set; }

        // ── Inspector clips ──────────────────────────────────
        [Header("Music")]
        [SerializeField] private AudioClip _menuMusic;
        [SerializeField] private AudioClip _gameMusic;

        [Header("SFX")]
        [SerializeField] private AudioClip _sfxMatch;
        [SerializeField] private AudioClip _sfxSwap;
        [SerializeField] private AudioClip _sfxInvalidSwap;
        [SerializeField] private AudioClip _sfxSpecialPiece;
        [SerializeField] private AudioClip _sfxCombo;
        [SerializeField] private AudioClip _sfxLevelWin;
        [SerializeField] private AudioClip _sfxLevelFail;
        [SerializeField] private AudioClip _sfxButton;
        [SerializeField] private AudioClip _sfxCoin;

        // ── AudioSources ─────────────────────────────────────
        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        // ── State ────────────────────────────────────────────
        public bool SoundOn { get; private set; } = true;
        public bool MusicOn { get; private set; } = true;

        // ── SFX enum for clean call sites ────────────────────
        public enum SFX
        {
            Match, Swap, InvalidSwap, SpecialPiece,
            Combo, LevelWin, LevelFail, Button, Coin
        }

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;

            LoadPreferences();
        }

        // ── Public API ───────────────────────────────────────

        public void PlayMenuMusic()  => PlayMusic(_menuMusic);
        public void PlayGameMusic()  => PlayMusic(_gameMusic);

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;

            _musicSource.clip = clip;
            _musicSource.volume = MusicOn ? 0.7f : 0f;
            _musicSource.Play();
        }

        public void StopMusic() => _musicSource.Stop();

        public void PlaySFX(SFX sfx)
        {
            if (!SoundOn) return;
            AudioClip clip = GetClip(sfx);
            if (clip != null)
                _sfxSource.PlayOneShot(clip);
        }

        public void PlaySFX(AudioClip clip)
        {
            if (!SoundOn || clip == null) return;
            _sfxSource.PlayOneShot(clip);
        }

        public void SetSoundOn(bool on)
        {
            SoundOn = on;
            PlayerPrefs.SetInt(Constants.PREF_SOUND_ON, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetMusicOn(bool on)
        {
            MusicOn = on;
            _musicSource.volume = on ? 0.7f : 0f;
            PlayerPrefs.SetInt(Constants.PREF_MUSIC_ON, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ToggleSound() => SetSoundOn(!SoundOn);
        public void ToggleMusic() => SetMusicOn(!MusicOn);

        // ── Private ──────────────────────────────────────────

        private AudioClip GetClip(SFX sfx) => sfx switch
        {
            SFX.Match        => _sfxMatch,
            SFX.Swap         => _sfxSwap,
            SFX.InvalidSwap  => _sfxInvalidSwap,
            SFX.SpecialPiece => _sfxSpecialPiece,
            SFX.Combo        => _sfxCombo,
            SFX.LevelWin     => _sfxLevelWin,
            SFX.LevelFail    => _sfxLevelFail,
            SFX.Button       => _sfxButton,
            SFX.Coin         => _sfxCoin,
            _                => null
        };

        private void LoadPreferences()
        {
            SoundOn = PlayerPrefs.GetInt(Constants.PREF_SOUND_ON, 1) == 1;
            MusicOn = PlayerPrefs.GetInt(Constants.PREF_MUSIC_ON,  1) == 1;
        }
    }
}
