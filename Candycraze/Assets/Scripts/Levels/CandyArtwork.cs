using UnityEngine;

namespace CandyCraze
{
    // IDs remain unchanged so saved objectives/progress still refer to the same types.
    public static class CandyArtwork
    {
        private static readonly string[] NormalNames =
            { "Candy_Red", "Candy_Blue", "Candy_Green", "Candy_Purple", "Candy_Yellow", "Candy_Orange" };
        private static readonly Sprite[] Normal = new Sprite[6];
        private static readonly Sprite[] Powers = new Sprite[4];
        private static readonly Sprite[] Stripes = new Sprite[12];
        private static readonly Sprite[] Wrapped = new Sprite[6];
        private static Texture2D plainWrappedAtlas;
        private static Texture2D stripeAtlas;
        public static Sprite GetStripe(int color, bool vertical)
        {
            if (color < 0 || color >= 6) return null;
            int index = color + (vertical ? 6 : 0);
            if (Stripes[index] != null) return Stripes[index];
            if (stripeAtlas == null) stripeAtlas = Resources.Load<Texture2D>("CandySprites/StripedCandyAtlas");
            if (stripeAtlas == null) return GetPower(GemSpecialType.LineBlast, vertical);
            float width = stripeAtlas.width / 6f, height = stripeAtlas.height / 2f;
            Stripes[index] = Sprite.Create(stripeAtlas,
                new Rect(color * width, vertical ? 0 : height, width, height),
                new Vector2(.5f, .5f), width, 0, SpriteMeshType.FullRect);
            return Stripes[index];
        }
        private static readonly bool[] NormalLoaded = new bool[6], PowerLoaded = new bool[4];
        public static Sprite GetNormal(int type)
        {
            if (type < 0 || type >= Normal.Length) return null;
            if (!NormalLoaded[type])
            {
                Normal[type] = GetPlainWrapped(type, false) ?? Resources.Load<Sprite>("CandySprites/" + NormalNames[type]);
                NormalLoaded[type] = true;
            }
            return Normal[type];
        }
        public static Sprite GetWrapped(int type)
        {
            if (type < 0 || type >= Wrapped.Length) return null;
            if (Wrapped[type] == null) Wrapped[type] = GetPlainWrapped(type, true);
            return Wrapped[type] ?? GetPower(GemSpecialType.AreaBomb);
        }

        private static Sprite GetPlainWrapped(int type, bool wrapped)
        {
            if (plainWrappedAtlas == null)
            {
                var source = Resources.Load<Texture2D>("CandySprites/PlainWrappedCandyAtlas");
                if (source == null) return null;
                // The generated sheet includes a neutral transparency-preview backdrop.
                // Mask only neutral pixels connected to the sheet edges; keep enclosed
                // white candy highlights. Cache the rendered texture for all twelve sprites.
                var pixels = ReadAtlasPixels(source);
                int width = source.width, height = source.height;
                var visited = new bool[pixels.Length];
                var queue = new System.Collections.Generic.Queue<int>();
                void Visit(int index)
                {
                    if (visited[index]) return;
                    visited[index] = true;
                    var p = pixels[index];
                    int max = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
                    int min = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
                    if (p.a != 0 && max - min > 45) return;
                    pixels[index].a = 0;
                    queue.Enqueue(index);
                }
                for (int x = 0; x < width; x++) { Visit(x); Visit((height - 1) * width + x); }
                for (int y = 0; y < height; y++) { Visit(y * width); Visit(y * width + width - 1); }
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue(), x = index % width;
                    if (x > 0) Visit(index - 1);
                    if (x < width - 1) Visit(index + 1);
                    if (index >= width) Visit(index - width);
                    if (index < pixels.Length - width) Visit(index + width);
                }
                plainWrappedAtlas = new Texture2D(width, height, TextureFormat.RGBA32, false);
                plainWrappedAtlas.name = "PlainWrappedCandySprites";
                plainWrappedAtlas.wrapMode = TextureWrapMode.Clamp;
                plainWrappedAtlas.filterMode = FilterMode.Bilinear;
                plainWrappedAtlas.SetPixels32(pixels);
                plainWrappedAtlas.Apply(false, true);
                Resources.UnloadAsset(source);
            }
            float cellWidth = plainWrappedAtlas.width / 6f, cellHeight = plainWrappedAtlas.height / 2f;
            return Sprite.Create(plainWrappedAtlas,
                new Rect(type * cellWidth, wrapped ? 0 : cellHeight, cellWidth, cellHeight),
                new Vector2(.5f, .5f), cellWidth, 0, SpriteMeshType.FullRect);
        }
        public static Sprite GetPower(GemSpecialType type, bool vertical = false)
        {
            int index = type == GemSpecialType.LineBlast ? (vertical ? 1 : 0) :
                type == GemSpecialType.AreaBomb ? 2 : type == GemSpecialType.ColorCrystal ? 3 : -1;
            if (index < 0) return null;
            if (!PowerLoaded[index])
            {
                string[] names = { "Power_Stripe_H", "Power_Stripe_V", "Power_Wrapped", "Power_Rainbow" };
                Powers[index] = Resources.Load<Sprite>("CandySprites/" + names[index]);
                PowerLoaded[index] = true;
            }
            return Powers[index];
        }

        private static Color32[] ReadAtlasPixels(Texture2D source)
        {
            if (source.isReadable) return source.GetPixels32();

            // Imported textures may have Read/Write disabled. Read a GPU copy
            // instead of accessing the imported texture's unavailable CPU data.
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(source.width, source.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D readable = null;
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                readable.Apply(false, false);
                return readable.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (readable != null)
                {
                    if (Application.isPlaying) Object.Destroy(readable);
                    else Object.DestroyImmediate(readable);
                }
            }
        }
    }
}
