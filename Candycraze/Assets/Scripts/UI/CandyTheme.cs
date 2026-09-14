using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CandyCraze
{
    public static class CandyTheme
    {
        public static readonly Color Ink = Hex("54215F"), Pink = Hex("EC458D"), Purple = Hex("7955CB");
        public static readonly Color Cream = Hex("FFF8EF"), Gold = Hex("FFD36B"), Mint = Hex("4FC8B6");
        private static Sprite rounded;
        private static Sprite gameBackdrop;
        private static Sprite circle;
        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                var texture = new Texture2D(128,128,TextureFormat.RGBA32,false);
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.wrapMode = TextureWrapMode.Clamp;
                for(int y=0;y<128;y++) for(int x=0;x<128;x++)
                    texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(63.5f-Vector2.Distance(new Vector2(x,y),new Vector2(63.5f,63.5f)))));
                texture.Apply();
                circle=Sprite.Create(texture,new Rect(0,0,128,128),Vector2.one*.5f,100);
                circle.hideFlags=HideFlags.HideAndDontSave;
                return circle;
            }
        }
        public static Sprite GameBackdrop
        {
            get
            {
                if (gameBackdrop != null) return gameBackdrop;
                var texture = new Texture2D(36, 64, TextureFormat.RGB24, false);
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < 64; y++) for (int x = 0; x < 36; x++)
                    texture.SetPixel(x, y, Color.Lerp(Hex("BADFE7"), Hex("F6CCE7"), y / 63f));
                texture.Apply();
                gameBackdrop = Sprite.Create(texture, new Rect(0,0,36,64), Vector2.one*.5f, 36);
                gameBackdrop.hideFlags = HideFlags.HideAndDontSave;
                return gameBackdrop;
            }
        }
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                texture.hideFlags = HideFlags.HideAndDontSave;
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - 8, 0);
                    float dy = Mathf.Max(Mathf.Abs(y - 31.5f) - 8, 0);
                    texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(24 - Mathf.Sqrt(dx * dx + dy * dy))));
                }
                texture.Apply(); texture.wrapMode = TextureWrapMode.Clamp;
                rounded = Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * 24);
                rounded.hideFlags = HideFlags.HideAndDontSave;
                return rounded;
            }
        }
        public static RectTransform Rect(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }
        public static RectTransform Page(string name, bool background = true)
        {
            // Disable rendering/input only; never deactivate gameplay manager objects.
            foreach (var old in Object.FindObjectsOfType<Canvas>()) old.enabled = false;
            foreach (var old in Object.FindObjectsOfType<GraphicRaycaster>()) old.enabled = false;
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            if (background) Backdrop(go.transform);
            var safe = Rect(go.transform, "SafeArea", 0, 0, 1, 1);
            safe.gameObject.AddComponent<CandySafeArea>();
            return safe;
        }
        public static Image Card(Transform parent, string name, float x0, float y0, float x1, float y1, Color color, bool shadow = true)
        {
            var image = Rect(parent, name, x0, y0, x1, y1).gameObject.AddComponent<Image>();
            image.sprite = Rounded; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = false;
            if (shadow)
            {
                var shade = image.gameObject.AddComponent<Shadow>();
                shade.effectColor = new Color(.24f, .06f, .3f, .20f); shade.effectDistance = new Vector2(0, -9);
            }
            return image;
        }
        public static Text Label(Transform parent, string value, float x0, float y0, float x1, float y1, int size = 36, Color? color = null)
        {
            var text = Rect(parent, "Text", x0, y0, x1, y1).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size; text.fontStyle = FontStyle.Bold; text.color = color ?? Ink;
            text.text = value; text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 18; text.resizeTextMaxSize = size;
            text.raycastTarget = false; text.supportRichText = false;
            return text;
        }
        public static Button Button(Transform parent, string value, float x0, float y0, float x1, float y1, Color color, UnityEngine.Events.UnityAction action, int size = 38)
        {
            var image = Card(parent, value, x0, y0, x1, y1, color);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            image.gameObject.AddComponent<CandyButtonMotion>();
            var colors = button.colors; colors.pressedColor = new Color(.86f,.86f,.86f); colors.disabledColor = new Color(.65f,.65f,.65f,.7f); button.colors = colors;
            Label(image.transform, value, .05f, .10f, .95f, .90f, size, Color.white);
            button.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button); action?.Invoke(); });
            return button;
        }
        public static Image SpriteImage(Transform parent, Sprite sprite, float x0, float y0, float x1, float y1)
        {
            var image = Rect(parent, "Decoration", x0, y0, x1, y1).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }
        public static void Backdrop(Transform parent)
        {
            var bg = Rect(parent, "SorbetSky", 0, 0, 1, 1).gameObject.AddComponent<CandyGradient>();
            bg.raycastTarget = false;
            var circle = Circle;
            for (int i = 0; i < 5; i++)
            {
                var hill = SpriteImage(parent, circle, -.4f + i * .3f, -.32f + (i % 2) * .08f, .55f + i * .3f, .39f + (i % 2) * .08f);
                hill.preserveAspect = false;
                hill.color = i % 2 == 0 ? Hex("DDB4ED") : Hex("B9DCE6");
            }
            for (int i = 0; i < 15; i++)
            {
                float x = .05f + (i * .137f) % .9f, y = .18f + (i * .193f) % .78f;
                var star = SpriteImage(parent, Resources.Load<Sprite>("UI/Star"), x, y, x + .025f, y + .018f);
                star.color = new Color(1, 1, 1, .6f);
            }
        }
        public static RectTransform Modal(Transform safe, string title, out GameObject overlay)
        {
            var shade = Rect(safe.parent, title + "Overlay", 0, 0, 1, 1).gameObject.AddComponent<Image>();
            shade.color = new Color(.16f,.04f,.24f,.78f);
            var card = Card(shade.transform, "Dialog", .08f, .27f, .92f, .75f, Cream);
            Label(card.transform, title, .06f, .77f, .94f, .94f, 60);
            overlay = shade.gameObject;
            return card.rectTransform;
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public class CandyGradient : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            var bottom = CandyTheme.Hex("F8D5DC"); var top = CandyTheme.Hex("F4C3EB");
            vh.AddVert(new Vector3(r.xMin,r.yMin), bottom, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin,r.yMax), top, Vector2.up);
            vh.AddVert(new Vector3(r.xMax,r.yMax), top, Vector2.one);
            vh.AddVert(new Vector3(r.xMax,r.yMin), bottom, Vector2.right);
            vh.AddTriangle(0,1,2); vh.AddTriangle(2,3,0);
        }
    }
    public class CandyButtonMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private float target = 1;
        public void OnPointerDown(PointerEventData data) { if (GetComponent<Button>().IsInteractable()) target = .95f; }
        public void OnPointerUp(PointerEventData data) { target = 1; }
        public void OnPointerExit(PointerEventData data) { target = 1; }
        private void OnDisable() { target = 1; transform.localScale = Vector3.one; }
        private void Update() { transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, 1-Mathf.Exp(-24*Time.unscaledDeltaTime)); }
    }
    public class CandySafeArea : MonoBehaviour
    {
        private Rect last; private Vector2 size;
        private void OnEnable() { Refresh(); }
        private void Update() { if (last != Screen.safeArea || size != new Vector2(Screen.width, Screen.height)) Refresh(); }
        private void Refresh()
        {
            last = Screen.safeArea; size = new Vector2(Screen.width, Screen.height);
            if (size.x <= 0 || size.y <= 0) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(last.xMin / size.x, last.yMin / size.y);
            rt.anchorMax = new Vector2(last.xMax / size.x, last.yMax / size.y);
        }
    }
}
