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
            if (!NormalLoaded[type]) { Normal[type] = Resources.Load<Sprite>("CandySprites/" + NormalNames[type]); NormalLoaded[type] = true; }
            return Normal[type];
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
    }
}
