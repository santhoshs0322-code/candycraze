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
            // Highlight the active booster with a gold glow; others keep
            // their own candy color (handled in RefreshUI).
            HighlightActive(_hammerBtn,     type == BoosterType.Hammer);
            HighlightActive(_rowBlastBtn,   type == BoosterType.RowBlast);
            HighlightActive(_shuffleBtn,    type == BoosterType.Shuffle);
            HighlightActive(_extraMovesBtn, type == BoosterType.ExtraMoves);
            HighlightActive(_colorBlastBtn, type == BoosterType.ColorBlast);
        }

        private void HighlightActive(Button btn, bool active)
        {
            if (btn == null) return;
            if (active) SetButtonColor(btn, _activeColor);
            else SetButtonAlpha(btn, 1f);
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

            // Find the count label — assigned, direct child, nested badge,
            // or any descendant named "Label".
            if (countTxt == null)
            {
                var lbl = btn.transform.Find("Label")
                       ?? btn.transform.Find("CountBadge/Label");
                if (lbl != null) countTxt = lbl.GetComponent<Text>();
                if (countTxt == null)
                {
                    foreach (var t in btn.GetComponentsInChildren<Text>(true))
                        if (t.name == "Label") { countTxt = t; break; }
                }
            }
            if (countTxt != null) countTxt.text = $"x{count}";

            // Preserve each booster's own candy color (set at build time).
            // Just fade the whole slot when empty so the design stays intact.
            SetButtonAlpha(btn, count > 0 ? 1f : 0.45f);
            btn.interactable = count > 0 &&
                (GameManager.Instance?.State == GameState.Playing ||
                 GameManager.Instance?.State == GameState.WaitingForBoard);
        }

        // Capture each button's original (candy) color once so we can
        // restore it instead of overwriting with a generic blue/grey.
        private readonly System.Collections.Generic.Dictionary<Button, Color> _baseColors
            = new System.Collections.Generic.Dictionary<Button, Color>();

        private void SetButtonAlpha(Button btn, float alpha)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            if (!_baseColors.TryGetValue(btn, out var baseCol))
            {
                baseCol = img.color;
                _baseColors[btn] = baseCol;
            }
            img.color = new Color(baseCol.r, baseCol.g, baseCol.b, alpha);
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
