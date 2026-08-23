// ============================================================
// DailyRewardPanelController.cs
// Shows 7-day reward calendar and handles claiming.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class DailyRewardPanelController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("UI")]
        [SerializeField] private Text       _titleText;
        [SerializeField] private Text       _countdownText;
        [SerializeField] private Text       _rewardText;
        [SerializeField] private Text       _statusText;
        [SerializeField] private Button     _claimButton;
        [SerializeField] private Text       _claimButtonText;

        // Day indicators (7 items)
        [SerializeField] private Image[]    _dayIndicators;
        [SerializeField] private Text[]     _dayLabels;
        [SerializeField] private Color      _dayCompletedColor = new Color(0.2f,0.7f,0.2f);
        [SerializeField] private Color      _dayCurrentColor   = new Color(1f,0.8f,0.1f);
        [SerializeField] private Color      _dayLockedColor    = new Color(0.3f,0.3f,0.3f);

        private Coroutine _countdownCoroutine;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_claimButton != null) _claimButton.onClick.AddListener(OnClaim);
        }

        // ── Public API ───────────────────────────────────────

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            RefreshUI();

            if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = StartCoroutine(CountdownRoutine());
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
        }

        // ── UI Refresh ───────────────────────────────────────

        private void RefreshUI()
        {
            var mgr = DailyRewardManager.Instance;
            if (mgr == null) return;

            bool canClaim = mgr.CanClaimToday();
            int  currentDay = mgr.GetCurrentDay();
            var  reward = mgr.GetTodaysReward();

            // Title
            if (_titleText != null)
                _titleText.text = $"Day {currentDay} of {mgr.GetTotalDays()}";

            // Today's reward
            if (_rewardText != null)
                _rewardText.text = canClaim
                    ? $"Today's reward:\n{reward.DisplayText}"
                    : "Come back tomorrow!";

            // Claim button
            if (_claimButton != null)
            {
                _claimButton.interactable = canClaim;
                if (_claimButtonText != null)
                    _claimButtonText.text = canClaim ? "🎁 CLAIM!" : "✓ Claimed";
            }

            // Day indicators
            if (_dayIndicators != null)
            {
                for (int i = 0; i < _dayIndicators.Length; i++)
                {
                    if (_dayIndicators[i] == null) continue;
                    int dayNum = i + 1;
                    if (dayNum < currentDay)
                        _dayIndicators[i].color = _dayCompletedColor;
                    else if (dayNum == currentDay)
                        _dayIndicators[i].color = _dayCurrentColor;
                    else
                        _dayIndicators[i].color = _dayLockedColor;

                    if (_dayLabels != null && i < _dayLabels.Length && _dayLabels[i] != null)
                        _dayLabels[i].text = $"Day {dayNum}";
                }
            }
        }

        // ── Claim ────────────────────────────────────────────

        private void OnClaim()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Coin);
            var mgr = DailyRewardManager.Instance;
            if (mgr == null) return;

            if (mgr.ClaimReward())
            {
                var reward = mgr.GetTodaysReward();
                // Note: day incremented already, so get previous
                StartCoroutine(ShowClaimAnimation(reward.DisplayText));
                RefreshUI();
            }
        }

        private IEnumerator ShowClaimAnimation(string rewardText)
        {
            if (_statusText != null)
            {
                _statusText.text = $"✦ {rewardText} ✦";
                _statusText.gameObject.SetActive(true);

                // Scale pop
                float elapsed = 0f;
                while (elapsed < 0.4f)
                {
                    elapsed += Time.deltaTime;
                    float s = 1f + Mathf.Sin(elapsed / 0.4f * Mathf.PI) * 0.3f;
                    _statusText.transform.localScale = Vector3.one * s;
                    yield return null;
                }
                _statusText.transform.localScale = Vector3.one;
                yield return new WaitForSeconds(2f);
                _statusText.gameObject.SetActive(false);
            }
        }

        // ── Countdown ────────────────────────────────────────

        private IEnumerator CountdownRoutine()
        {
            while (true)
            {
                if (_countdownText != null && DailyRewardManager.Instance != null)
                {
                    var ts = DailyRewardManager.Instance.TimeUntilNextReward();
                    if (ts == System.TimeSpan.Zero)
                    {
                        _countdownText.text = "";
                        RefreshUI();
                    }
                    else
                    {
                        _countdownText.text = $"Next reward in: {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
                    }
                }
                yield return new WaitForSeconds(1f);
            }
        }
    }
}
