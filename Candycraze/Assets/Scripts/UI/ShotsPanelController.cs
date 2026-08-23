// ============================================================
// ShotsPanelController.cs
// The "SHOTS" screen — an in-game screenshot gallery.
//   • CAPTURE grabs the current view (hiding this panel first)
//     and saves an original PNG to persistent storage.
//   • On open, previously captured shots load into a scroll grid.
//
// Fully original feature — no third-party assets.
// ============================================================

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class ShotsPanelController : MonoBehaviour
    {
        RectTransform _content;
        Text _empty;
        Button _capture;
        Font _font;
        bool _busy;

        string Dir => Path.Combine(Application.persistentDataPath, "Shots");

        public void Setup(RectTransform content, Text empty, Button capture, Font font)
        {
            _content = content;
            _empty = empty;
            _capture = capture;
            _font = font;
            if (_capture != null)
                _capture.onClick.AddListener(() =>
                {
                    if (!_busy) StartCoroutine(CaptureRoutine());
                });
        }

        void OnEnable() => Reload();

        // ── Gallery ──────────────────────────────────────────
        void Reload()
        {
            if (_content == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
            string[] files = Directory.GetFiles(Dir, "*.png");
            Array.Sort(files);
            Array.Reverse(files); // newest first

            if (_empty != null) _empty.gameObject.SetActive(files.Length == 0);

            foreach (var f in files) AddThumb(f);
        }

        void AddThumb(string path)
        {
            try
            {
                byte[] data = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(data)) { Destroy(tex); return; }
                tex.wrapMode = TextureWrapMode.Clamp;

                var go = new GameObject("Shot");
                go.transform.SetParent(_content, false);
                var img = go.AddComponent<Image>();
                img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);
                img.preserveAspect = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Shots] Failed to load {path}: {e.Message}");
            }
        }

        // ── Capture ──────────────────────────────────────────
        IEnumerator CaptureRoutine()
        {
            _busy = true;
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);

            // Hide this overlay (keep the GameObject active so the
            // coroutine keeps running) so the shot shows the menu.
            var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            float prev = cg.alpha;
            cg.alpha = 0f;

            yield return new WaitForEndOfFrame();

            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();

            cg.alpha = prev;

            try
            {
                if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
                string file = Path.Combine(Dir, $"shot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
                File.WriteAllBytes(file, shot.EncodeToPNG());
                Debug.Log($"[Shots] Saved {file}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Shots] Save failed: {e.Message}");
            }
            finally
            {
                if (shot != null) Destroy(shot);
            }

            Reload();
            _busy = false;
        }
    }
}
