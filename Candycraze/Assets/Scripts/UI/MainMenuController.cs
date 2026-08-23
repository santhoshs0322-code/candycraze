// ============================================================
// MainMenuController.cs
// Complete main menu — Play, Shop, Daily Reward, Settings.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _livesText;

        [Header("Panels")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _shopPanel;
        [SerializeField] private GameObject _dailyRewardPanel;
        [SerializeField] private GameObject _shotsPanel;

        [Header("Settings Buttons")]
        [SerializeField] private Text _soundBtnText;
        [SerializeField] private Text _musicBtnText;

        // ── Sub-controllers ──────────────────────────────────
        private ShopPanelController        _shopCtrl;
        private DailyRewardPanelController _dailyCtrl;

        // ────────────────────────────────────────────────────
        private void Start()
        {
            AudioManager.Instance?.PlayMenuMusic();

            _shopCtrl  = _shopPanel  != null ? _shopPanel.GetComponent<ShopPanelController>()        : null;
            _dailyCtrl = _dailyRewardPanel != null ? _dailyRewardPanel.GetComponent<DailyRewardPanelController>() : null;

            HideAllPanels();
            RefreshHUD();

            // Do NOT auto-show daily reward — let player open it manually
            // Uncomment below to re-enable auto-show:
            // if (DailyRewardManager.Instance != null &&
            //     DailyRewardManager.Instance.CanClaimToday())
            //     Invoke(nameof(AutoShowDailyReward), 1.0f);
        }

        private void AutoShowDailyReward() => OnDailyRewardPressed();

        // ── HUD ──────────────────────────────────────────────

        private void RefreshHUD()
        {
            if (SaveManager.Instance == null) return;
            var data = SaveManager.Instance.Data;

            if (_coinsText != null)
                _coinsText.text = $"✦ {data.Coins:N0}";

            if (_livesText != null)
            {
                string hearts = "";
                for (int i = 0; i < Constants.MAX_LIVES; i++)
                    hearts += i < data.Lives ? "♥" : "♡";
                _livesText.text = hearts;
            }

            RefreshAudioButtons();
        }

        private void RefreshAudioButtons()
        {
            if (AudioManager.Instance == null) return;
            if (_soundBtnText != null)
                _soundBtnText.text = AudioManager.Instance.SoundOn ? "🔊 Sound: ON" : "🔇 Sound: OFF";
            if (_musicBtnText != null)
                _musicBtnText.text = AudioManager.Instance.MusicOn ? "🎵 Music: ON" : "🎵 Music: OFF";
        }

        // ── Button Callbacks ─────────────────────────────────

        public void OnPlayPressed()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            // Use static method — works even if SceneController singleton isn't ready
            SceneController.NavigateTo(Constants.SCENE_LEVEL_MAP);
        }

        public void OnSettingsPressed()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            if (_settingsPanel == null) return;
            bool isOpen = _settingsPanel.activeSelf;
            HideAllPanels();
            if (!isOpen) _settingsPanel.SetActive(true);
        }

        public void OnShopPressed()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            if (_shopPanel == null) { Debug.Log("[Menu] Shop panel not assigned."); return; }
            bool isOpen = _shopPanel.activeSelf;
            HideAllPanels();
            if (!isOpen)
            {
                _shopPanel.SetActive(true);
                _shopCtrl?.Show();
            }
        }

        public void OnDailyRewardPressed()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            if (_dailyRewardPanel == null) { Debug.Log("[Menu] Daily reward panel not assigned."); return; }
            bool isOpen = _dailyRewardPanel.activeSelf;
            HideAllPanels();
            if (!isOpen)
            {
                _dailyRewardPanel.SetActive(true);
                _dailyCtrl?.Show();
            }
        }

        public void OnShotsPressed()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            if (_shotsPanel == null) { Debug.Log("[Menu] Shots panel not assigned."); return; }
            bool isOpen = _shotsPanel.activeSelf;
            HideAllPanels();
            if (!isOpen) _shotsPanel.SetActive(true);
        }

        /// <summary>
        /// Called by CandyHomeMenu after runtime UI is built so the
        /// dynamically-created overlay panels are wired to the menu
        /// buttons. Only fills references that were left null.
        /// </summary>
        public void InjectMenuPanels(GameObject settings, GameObject daily, GameObject shots)
        {
            if (_settingsPanel == null)    _settingsPanel = settings;
            if (_dailyRewardPanel == null) _dailyRewardPanel = daily;
            if (_shotsPanel == null)       _shotsPanel = shots;
            HideAllPanels();
        }

        public void OnSoundToggle()
        {
            AudioManager.Instance?.ToggleSound();
            RefreshAudioButtons();
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
        }

        public void OnMusicToggle()
        {
            AudioManager.Instance?.ToggleMusic();
            RefreshAudioButtons();
        }

    public void OnCloseAllPanels()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            HideAllPanels();
        }

        // ── Private ──────────────────────────────────────────

        private void HideAllPanels()
        {
            if (_settingsPanel    != null) _settingsPanel.SetActive(false);
            if (_shopPanel        != null) _shopPanel.SetActive(false);
            if (_dailyRewardPanel != null) _dailyRewardPanel.SetActive(false);
            if (_shotsPanel       != null) _shotsPanel.SetActive(false);
        }

        // ── Called by RuntimeUIBuilder ───────────────────────────
        /// <summary>
        /// RuntimeUIBuilder calls this after building the UI so that
        /// dynamically-created Text refs are injected when the Inspector
        /// fields were left null (pure runtime scenes).
        /// </summary>
        public void UpdateHUDRefs(Text coinsText, Text livesText)
        {
            if (_coinsText == null) _coinsText = coinsText;
            if (_livesText == null) _livesText = livesText;
            RefreshHUD();
        }
    }
}
