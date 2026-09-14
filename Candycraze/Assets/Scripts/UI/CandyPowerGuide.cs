using System;
using UnityEngine;
namespace CandyCraze
{
    public static class CandyPowerGuide
    {
        public static GameObject Show(Transform root)
        {
            var card = CandyTheme.Modal(root, "Power candy recipe book", out var overlay);
            GemSpecialType[] types = { GemSpecialType.LineBlast, GemSpecialType.LineBlast, GemSpecialType.AreaBomb, GemSpecialType.ColorCrystal };
            string[] recipes = { "4 down\nClears a row", "4 across\nClears a column", "5 in an L or T\n3 x 3 blast twice", "5 in a straight line\nClears one color" };
            for(int i=0;i<4;i++)
            {
                float x=.06f+(i%2)*.47f, y=i<2?.43f:.16f;
                var icon=CandyTheme.SpriteImage(card,CandyArtwork.GetPower(types[i],i==1),x+.09f,y+.10f,x+.32f,y+.31f);
                CandyTheme.Label(card,recipes[i],x,y,x+.41f,y+.11f,24);
            }
            CandyTheme.Button(card,"COMBINATIONS",.12f,.025f,.88f,.135f,CandyTheme.Purple,() => {
                UnityEngine.Object.Destroy(overlay);
                var combos=CandyTheme.Modal(root,"Mix your power candies",out var shade);
                CandyTheme.Label(combos,"Stripe + stripe: row AND column\nStripe + wrapped: 3 rows + 3 columns\nWrapped + wrapped: 5 x 5 blast twice\nRainbow + stripe: color becomes stripes\nRainbow + wrapped: color becomes wraps\nRainbow + rainbow: clear the board",.06f,.19f,.94f,.75f,30);
                CandyTheme.Button(combos,"SWEET, GOT IT!",.12f,.025f,.88f,.135f,CandyTheme.Pink,()=>UnityEngine.Object.Destroy(shade),30);
            },30);
            CandyTheme.Button(card,"X",.89f,.91f,.98f,.99f,CandyTheme.Pink,()=>UnityEngine.Object.Destroy(overlay),24);
            return overlay;
        }
    }
}
