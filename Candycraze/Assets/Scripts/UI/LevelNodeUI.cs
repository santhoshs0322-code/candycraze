// ============================================================
// LevelNodeUI.cs
// Uses standard UnityEngine.UI.Text (no TMP dependency).
// ============================================================

using System;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class LevelNodeUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Text       _levelNumberText;
        [SerializeField] private Image[]    _starImages;
        [SerializeField] private GameObject _lockOverlay;
        [SerializeField] private Button     _button;

        [Header("Star Colors")]
        [SerializeField] private Color _starFilledColor = Color.yellow;
        [SerializeField] private Color _starEmptyColor  = Color.grey;

        private int         _levelNumber;
        private Action<int> _onSelected;

        // ────────────────────────────────────────────────────
        public void Setup(int levelNumber, bool unlocked, int stars, Action<int> onSelected)
        {
            _levelNumber = levelNumber;
            _onSelected  = onSelected;

            if (_levelNumberText != null) _levelNumberText.text = levelNumber.ToString();
            if (_lockOverlay     != null) _lockOverlay.SetActive(!unlocked);

            if (_button != null)
            {
                _button.interactable = unlocked;
                _button.onClick.AddListener(OnButtonClicked);
            }

            if (_starImages != null)
                for (int i = 0; i < _starImages.Length; i++)
                    if (_starImages[i] != null)
                        _starImages[i].color = i < stars ? _starFilledColor : _starEmptyColor;
        }

        private void OnButtonClicked() => _onSelected?.Invoke(_levelNumber);
    }
}
