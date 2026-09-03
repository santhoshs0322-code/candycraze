// ============================================================
// PowerIconGenerator.cs (EDITOR ONLY)
// CandyCraze → Generate Power Icons
// Creates icon sprites for the 5 boosters and saves them to
// Resources/Powers so the booster bar can load them.
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace CandyCraze.Editor
{
    public static class PowerIconGenerator
    {
        [MenuItem("CandyCraze/Generate Power Icons")]
        public static void Generate()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Powers");
            string folder = "Assets/Resources/Powers";

            SaveIcon(HammerIcon(),    $"{folder}/Hammer.png");
            SaveIcon(RowBlastIcon(),  $"{folder}/RowBlast.png");
            SaveIcon(ShuffleIcon(),   $"{folder}/Shuffle.png");
            SaveIcon(ExtraMovesIcon(),$"{folder}/ExtraMoves.png");
            SaveIcon(ColorBlastIcon(),$"{folder}/ColorBlast.png");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Power Icons Created!",
                "5 booster icons saved to Resources/Powers/\n\n" +
                "Run 'Build All Scenes' to apply them to the booster bar.",
                "Done");
        }

        static int S = 128;

        static void SaveIcon(Texture2D tex, string path)
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

        // Rounded panel background
        static Color Bg(float u, float v, Color tint)
        {
            float ax = Mathf.Abs(u), ay = Mathf.Abs(v);
            float rr = Mathf.Max(ax, ay, (ax+ay)*0.7f);
            if (rr > 0.92f) return Color.clear;
            float bright = 0.5f + v * 0.3f;
            Color c = tint * bright;
            c.a = rr > 0.85f ? (0.92f-rr)/0.07f : 1f;
            return c;
        }

        // ── Hammer ───────────────────────────────────────────
        static Texture2D HammerIcon()
        {
            var t = new Texture2D(S,S,TextureFormat.RGBA32,false);
            for (int y=0;y<S;y++) for (int x=0;x<S;x++)
            {
                float u=x/(float)(S-1)*2-1, v=y/(float)(S-1)*2-1;
                Color c = Bg(u,v,new Color(0.15f,0.30f,0.55f));
                // Hammer head (top rectangle, rotated slightly)
                if (u>-0.45f&&u<0.45f&&v>0.15f&&v<0.6f)
                    c = new Color(0.75f,0.55f,0.2f)*(0.7f+u*0.3f);
                // Handle
                if (u>-0.1f&&u<0.1f&&v>-0.65f&&v<0.2f)
                    c = new Color(0.5f,0.32f,0.15f);
                if (c.a>0) c.a=1;
                t.SetPixel(x,y,c);
            }
            t.Apply(); return t;
        }

        // ── Row Blast ────────────────────────────────────────
        static Texture2D RowBlastIcon()
        {
            var t = new Texture2D(S,S,TextureFormat.RGBA32,false);
            for (int y=0;y<S;y++) for (int x=0;x<S;x++)
            {
                float u=x/(float)(S-1)*2-1, v=y/(float)(S-1)*2-1;
                Color c = Bg(u,v,new Color(0.55f,0.25f,0.10f));
                // Horizontal energy beam
                if (Mathf.Abs(v)<0.12f)
                {
                    float glow = 1f-Mathf.Abs(v)/0.12f;
                    c = Color.Lerp(c, new Color(1f,0.7f,0.1f), glow);
                }
                // Arrow tips
                if (u>0.55f && Mathf.Abs(v)<0.3f-(u-0.55f)*0.7f) c=new Color(1f,0.9f,0.2f);
                if (u<-0.55f && Mathf.Abs(v)<0.3f-(-u-0.55f)*0.7f) c=new Color(1f,0.9f,0.2f);
                if (c.a>0) c.a=1;
                t.SetPixel(x,y,c);
            }
            t.Apply(); return t;
        }

        // ── Shuffle ──────────────────────────────────────────
        static Texture2D ShuffleIcon()
        {
            var t = new Texture2D(S,S,TextureFormat.RGBA32,false);
            for (int y=0;y<S;y++) for (int x=0;x<S;x++)
            {
                float u=x/(float)(S-1)*2-1, v=y/(float)(S-1)*2-1;
                Color c = Bg(u,v,new Color(0.15f,0.45f,0.25f));
                float r=Mathf.Sqrt(u*u+v*v);
                // Two curved arrows (circle outline)
                if (Mathf.Abs(r-0.5f)<0.09f)
                    c = new Color(0.3f,1f,0.5f);
                // Arrow heads
                if (u>0.35f&&u<0.6f&&v>0.35f&&v<0.6f) c=new Color(0.3f,1f,0.5f);
                if (u<-0.35f&&u>-0.6f&&v<-0.35f&&v>-0.6f) c=new Color(0.3f,1f,0.5f);
                if (c.a>0) c.a=1;
                t.SetPixel(x,y,c);
            }
            t.Apply(); return t;
        }

        // ── Extra Moves ──────────────────────────────────────
        static Texture2D ExtraMovesIcon()
        {
            var t = new Texture2D(S,S,TextureFormat.RGBA32,false);
            for (int y=0;y<S;y++) for (int x=0;x<S;x++)
            {
                float u=x/(float)(S-1)*2-1, v=y/(float)(S-1)*2-1;
                Color c = Bg(u,v,new Color(0.35f,0.15f,0.55f));
                // Big plus sign
                bool h = Mathf.Abs(v)<0.13f && Mathf.Abs(u)<0.55f;
                bool w = Mathf.Abs(u)<0.13f && Mathf.Abs(v)<0.55f;
                if (h||w) c = new Color(0.4f,1f,0.5f)*(0.8f+v*0.3f);
                if (c.a>0) c.a=1;
                t.SetPixel(x,y,c);
            }
            t.Apply(); return t;
        }

        // ── Color Blast ──────────────────────────────────────
        static Texture2D ColorBlastIcon()
        {
            var t = new Texture2D(S,S,TextureFormat.RGBA32,false);
            for (int y=0;y<S;y++) for (int x=0;x<S;x++)
            {
                float u=x/(float)(S-1)*2-1, v=y/(float)(S-1)*2-1;
                Color c = Bg(u,v,new Color(0.20f,0.10f,0.30f));
                float r=Mathf.Sqrt(u*u+v*v);
                float ang=(Mathf.Atan2(v,u)+Mathf.PI)/(2*Mathf.PI);
                // Rainbow disc
                if (r<0.6f)
                {
                    Color rainbow = Color.HSVToRGB(ang,0.9f,1f);
                    c = Color.Lerp(c, rainbow, 1f-r/0.6f*0.3f);
                }
                // White centre sparkle
                if (r<0.15f) c = Color.white;
                if (c.a>0) c.a=1;
                t.SetPixel(x,y,c);
            }
            t.Apply(); return t;
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string p = Path.GetDirectoryName(path).Replace('\\','/');
                string f = Path.GetFileName(path);
                AssetDatabase.CreateFolder(p, f);
            }
        }
    }
}
#endif
