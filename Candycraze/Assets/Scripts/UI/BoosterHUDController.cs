// ============================================================
// BoosterHUDController.cs
// In-game booster buttons with inventory counts.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class BoosterHUDController : MonoBehaviour
    {
        [Header("Booster Buttons")]
        [SerializeField] private Button _hammerBtn;
        [SerializeField] private Button _rowBlastBtn;
        [SerializeField] private Button _shuffleBtn;
        [SerializeField] private Button _extraMovesBtn;
        [SerializeField] private Button _colorBlastBtn;

        [Header("Count Labels")]
        [SerializeField] private Text _hammerCount;
        [SerializeField] private Text _rowBlastCount;
        [SerializeField] private Text _shuffleCount;
        [SerializeField] private Text _extraMovesCount;
        [SerializeField] private Text _colorBlastCount;

        [Header("Active Highlight")]
        [SerializeField] private Color _activeColor  = new Color(1f,0.9f,0.1f);
        [SerializeField] private Color _normalColor  = new Color(0.18f,0.38f,0.85f);
        [SerializeField] private Color _emptyColor   = new Color(0.3f,0.3f,0.3f);

        // ────────────────────────────────────────────────────
        private void Start()
        {
            if (BoosterManager.Instance != null)
            {
                BoosterManager.Instance.OnInventoryChanged.AddListener(RefreshUI);
                BoosterManager.Instance.OnBoosterActivated.AddListener(OnBoosterActivated);
                BoosterManager.Instance.OnBoosterCancelled.AddListener(OnBoosterCancelled);
            }

            // Wire buttons
            if (_hammerBtn     != null) _hammerBtn.onClick.AddListener(    () => OnBoosterPressed(BoosterType.Hammer));
            if (_rowBlastBtn   != null) _rowBlastBtn.onClick.AddListener(  () => OnBoosterPressed(BoosterType.RowBlast));
            if (_shuffleBtn    != null) _shuffleBtn.onClick.AddListener(   () => OnBoosterPressed(BoosterType.Shuffle));
            if (_extraMovesBtn != null) _extraMovesBtn.onClick.AddListener(() => OnBoosterPressed(BoosterType.ExtraMoves));
            if (_colorBlastBtn != null) _colorBlastBtn.onClick.AddListener(() => OnBoosterPressed(BoosterType.ColorBlast));

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (BoosterManager.Instance != null)
            {
                BoosterManager.Instance.OnInventoryChanged.RemoveListener(RefreshUI);
                BoosterManager.Instance.OnBoosterActivated.RemoveListener(OnBoosterActivated);
                BoosterManager.Instance.OnBoosterCancelled.RemoveListener(OnBoosterCancelled);
            }
        }

        // ── Callbacks ────────────────────────────────────────

        private void OnBoosterPressed(BoosterType type)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);

            // If same booster is already active, cancel it
            if (BoosterManager.Instance?.ActiveBooster == type)
            {
                BoosterManager.Instance.Cancel();
                return;
            }

            BoosterManager.Instance?.TryActivate(type);
        }

        private void OnBoosterActivated(BoosterType type)
        {
            RefreshUI();
            // Highlight the active booster button
            SetButtonColor(_hammerBtn,     type == BoosterType.Hammer     ? _activeColor : GetColor(BoosterType.Hammer));
            SetButtonColor(_rowBlastBtn,   type == BoosterType.RowBlast   ? _activeColor : GetColor(BoosterType.RowBlast));
            SetButtonColor(_colorBlastBtn, type == BoosterType.ColorBlast ? _activeColor : GetColor(BoosterType.ColorBlast));
        }

        private void OnBoosterCancelled() => RefreshUI();

        // ── UI Refresh ───────────────────────────────────────

        private void RefreshUI()
        {
            UpdateBoosterButton(_hammerBtn,     _hammerCount,     BoosterType.Hammer,     "🔨");
            UpdateBoosterButton(_rowBlastBtn,   _rowBlastCount,   BoosterType.RowBlast,   "💥");
            UpdateBoosterButton(_shuffleBtn,    _shuffleCount,    BoosterType.Shuffle,    "🔀");
            UpdateBoosterButton(_extraMovesBtn, _extraMovesCount, BoosterType.ExtraMoves, "➕");
            UpdateBoosterButton(_colorBlastBtn, _colorBlastCount, BoosterType.ColorBlast, "🌈");
        }

        private void UpdateBoosterButton(Button btn, Text countTxt,
            BoosterType type, string icon)
        {
            if (btn == null) return;
            int count = BoosterManager.Instance?.GetCount(type) ?? 0;

            if (countTxt != null) countTxt.text = count > 0 ? $"x{count}" : "";

            SetButtonColor(btn, count > 0 ? _normalColor : _emptyColor);
            btn.interactable = count > 0 &&
                (GameManager.Instance?.State == GameState.Playing);
        }

        private void SetButtonColor(Button btn, Color col)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = col;
        }

        private Color GetColor(BoosterType type)
        {
            int count = BoosterManager.Instance?.GetCount(type) ?? 0;
            return count > 0 ? _normalColor : _emptyColor;
        }
    }
}
