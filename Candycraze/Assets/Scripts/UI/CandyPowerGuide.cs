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
            string[] recipes = { "4 across\nClears a row", "4 down\nClears a column", "5 in an L or T\n5 x 5 wrapped blast", "5 in a straight line\nClears one color" };
            for(int i=0;i<4;i++)
            {
                float x=.06f+(i%2)*.47f, y=i<2?.43f:.16f;
                var icon=CandyTheme.SpriteImage(card,CandyArtwork.GetPower(types[i],i==1),x+.09f,y+.10f,x+.32f,y+.31f);
                CandyTheme.Label(card,recipes[i],x,y,x+.41f,y+.11f,24);
            }
            CandyTheme.Button(card,"SWEET, GOT IT!",.12f,.025f,.88f,.135f,CandyTheme.Pink,() => UnityEngine.Object.Destroy(overlay),30);
            return overlay;
        }
    }
}
