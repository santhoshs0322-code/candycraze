// ============================================================
// BoosterIconSetup.cs (EDITOR ONLY)
// CandyCraze → Create Booster Icons
// Generates premium procedural icons for all 5 boosters.
// ============================================================
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;

namespace CandyCraze.Editor
{
    public static class BoosterIconSetup
    {
        [MenuItem("CandyCraze/Create Booster Icons")]
        public static void CreateBoosterIcons()
        {
            string folder = "Assets/Art/UI/Boosters";
            if (!AssetDatabase.IsValidFolder("Assets/Art/UI"))
                AssetDatabase.CreateFolder("Assets/Art","UI");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Art/UI","Boosters");

            CreateHammerIcon(folder);
            CreateRowBlastIcon(folder);
            CreateShuffleIcon(folder);
            CreateExtraMovesIcon(folder);
            CreateColorBlastIcon(folder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Booster Icons Created",
                "5 premium booster icons generated!\n\nAssign them in the BoosterHUDController.", "Done");
        }

        static Texture2D MakeTex(int size) => new Texture2D(size,size,TextureFormat.RGBA32,false);

        static void SaveTex(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit = 128;
                imp.filterMode = FilterMode.Bilinear;
                imp.SaveAndReimport();
            }
        }

        static Color BG = new Color(0.12f,0.08f,0.25f,1f);
        static Color Trans = Color.clear;

        // Rounded square background
        static float RoundedSquare(float u, float v, float r)
        {
            float ax = Mathf.Abs(u) - (1f-r); float ay = Mathf.Abs(v) - (1f-r);
            float outside = Mathf.Sqrt(Mathf.Max(0,ax)*Mathf.Max(0,ax)+Mathf.Max(0,ay)*Mathf.Max(0,ay));
            return 1f - (outside + Mathf.Min(0,Mathf.Max(ax,ay))) / r;
        }

        // ── Hammer ───────────────────────────────────────────
        static void CreateHammerIcon(string folder)
        {
            int s=128; var tex=MakeTex(s);
            for(int y=0;y<s;y++) for(int x=0;x<s;x++)
            {
                float u=(x/127f)*2f-1f, v=(y/127f)*2f-1f;
                float bg = RoundedSquare(u,v,0.25f);
                if(bg<=0f){tex.SetPixel(x,y,Trans);continue;}

                // Hammer head (rectangle top)
                bool head = u>-0.55f && u<0.55f && v>0.1f && v<0.65f;
                // Handle (thin rectangle)
                bool handle = u>-0.12f && u<0.12f && v>-0.7f && v<0.2f;
                // Shading
                Color c;
                if(head)
                {
                    float bright = 0.5f + u*0.3f + v*0.2f;
                    c = new Color(0.8f,0.6f,0.2f) * bright;
                }
                else if(handle)
                {
                    float bright = 0.6f + u*0.2f;
                    c = new Color(0.55f,0.35f,0.15f) * bright;
                }
                else
                    c = new Color(0.15f,0.10f,0.30f);

                c.a = Mathf.Clamp01(bg*8f);
                tex.SetPixel(x,y,c);
            }
            tex.Apply();
            SaveTex(tex, $"{folder}/Icon_Hammer.png");
        }

        // ── Row Blast ────────────────────────────────────────
        static void CreateRowBlastIcon(string folder)
        {
            int s=128; var tex=MakeTex(s);
            for(int y=0;y<s;y++) for(int x=0;x<s;x++)
            {
                float u=(x/127f)*2f-1f, v=(y/127f)*2f-1f;
                float bg=RoundedSquare(u,v,0.25f);
                if(bg<=0f){tex.SetPixel(x,y,Trans);continue;}

                Color c = new Color(0.15f,0.10f,0.30f);

                // Horizontal beam
                float beamH = 1f - Mathf.Abs(v) / 0.08f;
                if(beamH>0f)
                {
                    float intensity = beamH * (0.5f + (Mathf.Sin(u*8f)+1f)*0.3f);
                    c = Color.Lerp(c, new Color(1f,0.5f,0.1f), intensity);
                }
                // Explosion star in centre
                float angle = Mathf.Atan2(v,u);
                float r2 = Mathf.Sqrt(u*u+v*v);
                float star = 0.15f + 0.08f*Mathf.Cos(angle*8f);
                if(r2 < star)
                    c = Color.Lerp(Color.white, new Color(1f,0.9f,0.1f), r2/star);

                c.a = Mathf.Clamp01(bg*8f);
                tex.SetPixel(x,y,c);
            }
            tex.Apply();
            SaveTex(tex, $"{folder}/Icon_RowBlast.png");
        }

        // ── Shuffle ──────────────────────────────────────────
        static void CreateShuffleIcon(string folder)
        {
            int s=128; var tex=MakeTex(s);
            for(int y=0;y<s;y++) for(int x=0;x<s;x++)
            {
                float u=(x/127f)*2f-1f, v=(y/127f)*2f-1f;
                float bg=RoundedSquare(u,v,0.25f);
                if(bg<=0f){tex.SetPixel(x,y,Trans);continue;}

                Color c = new Color(0.15f,0.10f,0.30f);

                // Two circular arrows
                float r = Mathf.Sqrt(u*u+v*v);
                float angle = Mathf.Atan2(v,u) * Mathf.Rad2Deg;

                // Upper arc (right-pointing)
                float rU = Mathf.Abs(r - 0.45f);
                bool upperArc = rU < 0.1f && v > -0.1f;
                // Lower arc (left-pointing)
                bool lowerArc = rU < 0.1f && v < 0.1f;

                if(upperArc || lowerArc)
                {
                    float t2 = 1f - rU/0.1f;
                    c = Color.Lerp(c, new Color(0.2f,0.8f,1f), t2);
                }
                // Arrow heads
                bool arrowR = u > 0.5f && Mathf.Abs(v) < 0.15f - Mathf.Abs(u-0.6f)*0.5f;
                bool arrowL = u <-0.5f && Mathf.Abs(v) < 0.15f - Mathf.Abs(u+0.6f)*0.5f;
                if(arrowR||arrowL) c = new Color(0.2f,0.8f,1f);

                c.a = Mathf.Clamp01(bg*8f);
                tex.SetPixel(x,y,c);
            }
            tex.Apply();
            SaveTex(tex, $"{folder}/Icon_Shuffle.png");
        }

        // ── Extra Moves ──────────────────────────────────────
        static void CreateExtraMovesIcon(string folder)
        {
            int s=128; var tex=MakeTex(s);
            for(int y=0;y<s;y++) for(int x=0;x<s;x++)
            {
                float u=(x/127f)*2f-1f, v=(y/127f)*2f-1f;
                float bg=RoundedSquare(u,v,0.25f);
                if(bg<=0f){tex.SetPixel(x,y,Trans);continue;}

                Color c = new Color(0.15f,0.10f,0.30f);

                // Plus sign
                bool horiz = Mathf.Abs(v) < 0.12f && Mathf.Abs(u) < 0.65f;
                bool vert  = Mathf.Abs(u) < 0.12f && Mathf.Abs(v) < 0.65f;

                if(horiz || vert)
                {
                    float edge = horiz
                        ? 1f - Mathf.Abs(v)/0.12f
                        : 1f - Mathf.Abs(u)/0.12f;
                    Color plus = new Color(0.1f,0.9f,0.4f);
                    // Gradient shine
                    float shine = 0.5f + v * 0.4f;
                    c = Color.Lerp(plus * 0.7f, Color.white, edge * shine * 0.5f);
                }

                c.a = Mathf.Clamp01(bg*8f);
                tex.SetPixel(x,y,c);
            }
            tex.Apply();
            SaveTex(tex, $"{folder}/Icon_ExtraMoves.png");
        }

        // ── Color Blast ──────────────────────────────────────
        static void CreateColorBlastIcon(string folder)
        {
            int s=128; var tex=MakeTex(s);
            for(int y=0;y<s;y++) for(int x=0;x<s;x++)
            {
                float u=(x/127f)*2f-1f, v=(y/127f)*2f-1f;
                float bg=RoundedSquare(u,v,0.25f);
                if(bg<=0f){tex.SetPixel(x,y,Trans);continue;}

                Color c = new Color(0.15f,0.10f,0.30f);

                // Rainbow segments (6 wedges)
                float angle = (Mathf.Atan2(v,u)+Mathf.PI)/(2f*Mathf.PI);
                float r2 = Mathf.Sqrt(u*u+v*v);
                if(r2 < 0.7f && r2 > 0.15f)
                {
                    Color rainbow = Color.HSVToRGB(angle,0.9f,1f);
                    float t2 = Mathf.Clamp01((0.7f-r2)/0.55f);
                    c = Color.Lerp(c, rainbow, t2);
                }
                // White centre star
                if(r2 < 0.2f)
                {
                    float star2 = Mathf.Abs(Mathf.Sin(angle*6f*Mathf.PI*2f));
                    c = Color.Lerp(Color.white, c, r2/0.2f + star2*0.3f);
                }

                c.a = Mathf.Clamp01(bg*8f);
                tex.SetPixel(x,y,c);
            }
            tex.Apply();
            SaveTex(tex, $"{folder}/Icon_ColorBlast.png");
        }
    }
}
#endif
