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
