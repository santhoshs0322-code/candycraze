// ============================================================
// ScreenShake.cs
// Attach to Main Camera. Call ScreenShake.Instance.Shake()
// on big combos and special piece explosions.
// ============================================================

using System.Collections;
using UnityEngine;

namespace CandyCraze
{
    public class ScreenShake : MonoBehaviour
    {
        public static ScreenShake Instance { get; private set; }

        [SerializeField] private float _defaultDuration  = 0.25f;
        [SerializeField] private float _defaultMagnitude = 0.12f;

        private Vector3 _originPos;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _originPos = transform.localPosition;
        }

        public void Shake(float duration = -1f, float magnitude = -1f)
        {
            float d = duration  < 0 ? _defaultDuration  : duration;
            float m = magnitude < 0 ? _defaultMagnitude : magnitude;
            StartCoroutine(ShakeRoutine(d, m));
        }

        public void ShakeHeavy() => Shake(0.4f, 0.22f);
        public void ShakeLight()  => Shake(0.15f, 0.06f);

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float dampened = magnitude * (1f - progress); // fade out

                float x = _originPos.x + Random.Range(-1f, 1f) * dampened;
                float y = _originPos.y + Random.Range(-1f, 1f) * dampened;
                transform.localPosition = new Vector3(x, y, _originPos.z);
                yield return null;
            }
            transform.localPosition = _originPos;
        }
    }
}
