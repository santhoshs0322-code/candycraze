// ============================================================
// UIStyle.cs
// Shared UI factory so ALL pages (Menu, Level Map, Game) use
// the SAME button style, fonts, panels and back buttons.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public static class UIStyle
    {
        // ── Palette (matches home page) ──────────────────────
        public static readonly Color Gold  = Hex("#FFD700");
        public static readonly Color Cyan  = Hex("#33E6FF");
        public static readonly Color Green = Hex("#2ECC71");
        public static readonly Color Blue  = Hex("#3399F5");
        public static readonly Color Red   = Hex("#E74C3C");
        public static readonly Color Gold2 = Hex("#F5A623");
        public static readonly Color Purple= Hex("#8E44E8");
        public static readonly Color Dark  = Hex("#2D1B5E");
        public static readonly Color White = Color.white;

        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        // ── Rounded button (consistent everywhere) ───────────
        public static Button Button(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color color, int fontSize)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var img = g.AddComponent<Image>();
            Sprite sp = PickSprite(color);
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; img.color = Color.white; }
            else img.color = color;

            var btn = g.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f,1.1f,1.1f);
            cb.pressedColor = new Color(0.82f,0.82f,0.82f);
            cb.fadeDuration = 0.08f;
            btn.colors = cb; btn.targetGraphic = img;

            // Label with shadow for readability
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(g.transform, false);
            var shRt = shadow.AddComponent<RectTransform>();
            shRt.anchorMin = Vector2.zero; shRt.anchorMax = Vector2.one;
            shRt.offsetMin = new Vector2(2,-4); shRt.offsetMax = new Vector2(2,-4);
            var shTxt = shadow.AddComponent<Text>();
            shTxt.text = label; shTxt.fontSize = fontSize; shTxt.font = Font;
            shTxt.color = new Color(0,0,0,0.5f); shTxt.alignment = TextAnchor.MiddleCenter;
            shTxt.fontStyle = FontStyle.Bold; shTxt.raycastTarget = false;

            var lbl = new GameObject("Label");
            lbl.transform.SetParent(g.transform, false);
            var lRt = lbl.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;
            var t = lbl.AddComponent<Text>();
            t.text = label; t.fontSize = fontSize; t.font = Font;
            t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = FontStyle.Bold; t.raycastTarget = false;

            return btn;
        }

        // ── Back button (consistent — top-left rounded) ──────
        public static Button BackButton(Transform parent, System.Action onClick)
        {
            var btn = Button(parent, "BackBtn", "‹ BACK",
                new Vector2(0.02f, 0.90f), new Vector2(0.24f, 0.98f), Blue, 30);
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        // ── Title text with gold + shadow ────────────────────
        public static Text Title(Transform parent, string text,
            Vector2 anchor, int fontSize)
        {
            // Shadow
            var sh = Label(parent, text + "_sh", text, fontSize,
                new Color(0.5f,0.25f,0f,0.7f),
                new Vector2(anchor.x, anchor.y), true);
            sh.rectTransform.anchoredPosition += new Vector2(3,-3);
            // Main
            return Label(parent, text, text, fontSize, Gold, anchor, true);
        }

        public static Text Label(Transform parent, string name, string text,
            int fontSize, Color color, Vector2 anchor, bool bold = false)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchor.x-0.45f, anchor.y-0.08f);
            rt.anchorMax = new Vector2(anchor.x+0.45f, anchor.y+0.08f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var t = g.AddComponent<Text>();
            t.text = text; t.fontSize = fontSize; t.font = Font;
            t.color = color; t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.raycastTarget = false;
            return t;
        }

        // ── Panel with rounded sprite ────────────────────────
        public static GameObject Panel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = g.AddComponent<Image>();
            var sp = Resources.Load<Sprite>("UI/Panel");
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; img.color = color; }
            else img.color = color;
            return g;
        }

        // ── Helpers ──────────────────────────────────────────
        static Sprite PickSprite(Color c)
        {
            if (c.g > c.r && c.g > c.b) return Resources.Load<Sprite>("UI/BtnGreen");
            if (c.b > c.r && c.b > c.g) return Resources.Load<Sprite>("UI/BtnBlue");
            if (c.r > 0.8f && c.g > 0.5f) return Resources.Load<Sprite>("UI/BtnGold");
            if (c.r > c.g && c.r > c.b) return Resources.Load<Sprite>("UI/BtnRed");
            if (c.r > 0.4f && c.b > 0.6f) return Resources.Load<Sprite>("UI/BtnPurple");
            return Resources.Load<Sprite>("UI/BtnDark");
        }

        static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out Color c);
            return c;
        }
    }
}
