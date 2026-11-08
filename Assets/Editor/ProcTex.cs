using System.IO; using UnityEditor; using UnityEngine;
public static class ProcTex
{
    const string Dir = "Assets/Game/Textures";
    static System.Random rng = new System.Random(99);
    static float U() => (float)rng.NextDouble();
    static float N(float x, float y, float s) => Mathf.PerlinNoise(x * s + 13.1f, y * s + 7.7f);
    static float FBM(float x, float y, float s) { float v = 0, a = 0.5f; for (int i = 0; i < 5; i++) { v += a * N(x, y, s); s *= 2; a *= 0.5f; } return v; }
    static void Save(Texture2D t, string name, bool alpha = false)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllBytes(Path.Combine(Dir, name + ".png"), t.EncodeToPNG());
    }

    [MenuItem("Game/Generate Textures")]
    public static void All()
    {
        Rangoli(); Hand(); Blood(); Paper(); Cloths(); Facade(); Moon(); Streak(); Glow(); Board(); Kolam();
        AssetDatabase.Refresh();
        foreach (var n in new[] { "rangoli", "hand", "blood", "streak", "glow", "moon", "kolam" })
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(Dir + "/" + n + ".png");
            if (imp) { imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport(); }
        }
    }

    static void Rangoli()
    {
        int S = 1024; var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
        Color[] pal = { new Color(0.85f, 0.05f, 0.35f), new Color(1f, 0.5f, 0.02f), new Color(1f, 0.82f, 0.1f), new Color(0.1f, 0.55f, 0.2f), new Color(0.1f, 0.3f, 0.75f), new Color(0.95f, 0.95f, 0.9f) };
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float u = (x - S / 2f) / (S / 2f), v = (y - S / 2f) / (S / 2f);
            float r = Mathf.Sqrt(u * u + v * v), a = Mathf.Atan2(v, u);
            Color c = Color.clear;
            float grain = U();
            if (r < 0.98f)
            {
                float petals8 = Mathf.Abs(Mathf.Cos(a * 4));
                float petals16 = Mathf.Abs(Mathf.Cos(a * 8));
                if (r < 0.12f) c = pal[2];
                else if (r < 0.16f) c = pal[5];
                else if (r < 0.42f && r < 0.16f + 0.3f * petals8) c = r < 0.3f ? pal[0] : pal[1];
                else if (r < 0.46f) c = pal[5];
                else if (r < 0.75f && (r - 0.46f) < 0.29f * petals16) c = (Mathf.Repeat(a * 8 / Mathf.PI, 2) < 1) ? pal[3] : pal[4];
                else if (r > 0.78f && r < 0.82f) c = pal[2];
                else if (r > 0.86f && r < 0.98f && Mathf.Abs(Mathf.Sin(a * 24)) * 0.12f > Mathf.Abs(r - 0.92f)) c = pal[0];
                if (r > 0.88f && r < 0.9f && Mathf.Repeat(a * 48 / Mathf.PI, 2) < 0.6f) c = pal[5];
            }
            if (c.a > 0) { c *= 0.85f + 0.3f * grain; c.a = grain > 0.08f ? 1 : 0.3f; }
            t.SetPixel(x, y, c);
        }
        t.Apply(); Save(t, "rangoli");
    }

    static void Kolam()
    {
        int S = 512; var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float u = x / (float)S * 5, v = y / (float)S * 5;
            float fx = u - Mathf.Floor(u) - 0.5f, fy = v - Mathf.Floor(v) - 0.5f;
            float d = Mathf.Sqrt(fx * fx + fy * fy);
            float dot = d < 0.06f ? 1 : 0;
            float ring = Mathf.Abs(d - 0.32f) < 0.025f ? 1 : 0;
            float a = Mathf.Max(dot, ring) * (U() > 0.15f ? 1 : 0.3f);
            t.SetPixel(x, y, new Color(0.95f, 0.94f, 0.9f, a));
        }
        t.Apply(); Save(t, "kolam");
    }

    static void Hand()
    {
        int S = 512; var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
        System.Func<Vector2, Vector2, Vector2, float, float> cap = (p, a, b, r) => { var pa = p - a; var ba = b - a; float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba)); return (pa - ba * h).magnitude - r; };
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            var p = new Vector2(x / (float)S, y / (float)S);
            float d = cap(p, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.45f), 0.14f);
            d = Mathf.Min(d, cap(p, new Vector2(0.4f, 0.55f), new Vector2(0.36f, 0.78f), 0.035f));
            d = Mathf.Min(d, cap(p, new Vector2(0.48f, 0.58f), new Vector2(0.47f, 0.86f), 0.035f));
            d = Mathf.Min(d, cap(p, new Vector2(0.56f, 0.58f), new Vector2(0.58f, 0.83f), 0.034f));
            d = Mathf.Min(d, cap(p, new Vector2(0.62f, 0.52f), new Vector2(0.68f, 0.72f), 0.03f));
            d = Mathf.Min(d, cap(p, new Vector2(0.38f, 0.38f), new Vector2(0.24f, 0.5f), 0.035f));
            // drips
            for (int k = 0; k < 4; k++) d = Mathf.Min(d, cap(p, new Vector2(0.42f + k * 0.05f, 0.25f), new Vector2(0.42f + k * 0.05f, 0.05f + k * 0.04f), 0.008f + 0.004f * (k % 2)));
            float n = FBM(p.x, p.y, 18);
            float a = Mathf.Clamp01((-d + (n - 0.5f) * 0.03f) * 120);
            float wet = 0.6f + 0.4f * n;
            t.SetPixel(x, y, new Color(0.35f * wet, 0.01f, 0.01f, a * (0.75f + 0.25f * n)));
        }
        t.Apply(); Save(t, "hand");
    }

    static void Blood()
    {
        int S = 512; var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float u = x / (float)S - 0.5f, v = y / (float)S - 0.5f; float r = Mathf.Sqrt(u * u + v * v);
            float edge = 0.3f + (FBM(u + 3, v + 3, 6) - 0.5f) * 0.25f;
            float a = Mathf.Clamp01((edge - r) * 60);
            float n = FBM(u, v, 12);
            t.SetPixel(x, y, new Color(0.25f + 0.1f * n, 0.0f, 0.0f, a));
        }
        t.Apply(); Save(t, "blood");
    }

    static void Paper()
    {
        int S = 512; var t = new Texture2D(S, S, TextureFormat.RGB24, false);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float u = x / (float)S, v = y / (float)S;
            float n = FBM(u, v, 4), fine = U() * 0.04f;
            float stain = Mathf.Clamp01((FBM(u + 9, v + 2, 3) - 0.62f) * 4) * 0.25f;
            float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v)) * 12);
            var c = new Color(0.93f, 0.89f, 0.78f) * (0.92f + 0.08f * n - fine) - new Color(0.12f, 0.15f, 0.25f) * stain;
            c = Color.Lerp(c * 0.8f, c, edge);
            t.SetPixel(x, y, c);
        }
        t.Apply(); Save(t, "paper");
    }

    static void Cloths()
    {
        int S = 256;
        // lungi: madras check
        var t = new Texture2D(S, S, TextureFormat.RGB24, false);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float a = Mathf.Repeat(x / 32f, 1), b = Mathf.Repeat(y / 32f, 1);
            Color c = new Color(0.08f, 0.25f, 0.55f);
            if (a < 0.2f) c = Color.Lerp(c, new Color(0.1f, 0.45f, 0.25f), 0.7f);
            if (b < 0.2f) c = Color.Lerp(c, new Color(0.1f, 0.45f, 0.25f), 0.6f);
            if (Mathf.Abs(a - 0.6f) < 0.03f || Mathf.Abs(b - 0.6f) < 0.03f) c = new Color(0.85f, 0.85f, 0.8f);
            c *= 0.9f + 0.1f * ((x + y) % 2);
            t.SetPixel(x, y, c);
        }
        t.Apply(); Save(t, "cloth_lungi");
        // towel: stripes
        t = new Texture2D(S, S, TextureFormat.RGB24, false);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float b = y / (float)S; Color c = new Color(0.85f, 0.75f, 0.55f);
            if ((b > 0.08f && b < 0.12f) || (b > 0.88f && b < 0.92f)) c = new Color(0.55f, 0.08f, 0.1f);
            c *= 0.85f + 0.15f * U();
            t.SetPixel(x, y, c);
        }
        t.Apply(); Save(t, "cloth_towel");
        // gamcha: red / white check
        t = new Texture2D(S, S, TextureFormat.RGB24, false);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            bool cx = Mathf.Repeat(x / 24f, 1) < 0.5f, cy = Mathf.Repeat(y / 24f, 1) < 0.5f;
            Color c = cx && cy ? new Color(0.92f, 0.9f, 0.85f) : (cx || cy) ? new Color(0.75f, 0.2f, 0.18f) : new Color(0.6f, 0.06f, 0.06f);
            t.SetPixel(x, y, c * (0.9f + 0.1f * U()));
        }
        t.Apply(); Save(t, "cloth_gamcha");
        // white saree with red border (the wrong one)
        t = new Texture2D(S, S, TextureFormat.RGB24, false);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float a = x / (float)S; Color c = new Color(0.9f, 0.89f, 0.85f);
            if (a < 0.1f || a > 0.9f) c = new Color(0.6f, 0.02f, 0.03f);
            if (Mathf.Abs(a - 0.12f) < 0.01f || Mathf.Abs(a - 0.88f) < 0.01f) c = new Color(0.8f, 0.6f, 0.1f);
            t.SetPixel(x, y, c * (0.92f + 0.08f * U()));
        }
        t.Apply(); Save(t, "cloth_saree");
    }

    static void Facade()
    {
        int W = 1024, H = 1024; var t = new Texture2D(W, H, TextureFormat.RGB24, false); var e = new Texture2D(W, H, TextureFormat.RGB24, false);
        int cols = 8, rows = 8;
        var lit = new bool[cols, rows]; var tv = new bool[cols, rows];
        for (int i = 0; i < cols; i++) for (int j = 0; j < rows; j++) { lit[i, j] = U() < 0.13f; tv[i, j] = !lit[i, j] && U() < 0.04f; }
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            float u = x / (float)W, v = y / (float)H;
            float n = FBM(u, v, 6);
            float streak = Mathf.Clamp01(FBM(u * 8, v * 0.6f, 3) - 0.45f) * 0.5f;
            Color c = new Color(0.62f, 0.58f, 0.5f) * (0.75f + 0.3f * n) * (1 - streak);
            float cu = u * cols, cv = v * rows; int ci = (int)cu, cj = (int)cv; float fu = cu - ci, fv = cv - cj;
            Color em = Color.black;
            if (fv > 0.08f && fv < 0.14f) c *= 0.55f; // slab line / chajja shadow
            if (fu > 0.25f && fu < 0.75f && fv > 0.3f && fv < 0.85f)
            {
                c = new Color(0.03f, 0.035f, 0.04f);
                bool bar = Mathf.Repeat(fu * 40, 1) < 0.18f;
                if (lit[ci, cj]) { em = new Color(1f, 0.72f, 0.35f) * (0.6f + 0.4f * n); c = em * 0.6f; }
                if (tv[ci, cj]) { em = new Color(0.35f, 0.45f, 1f) * 0.6f; c = em * 0.5f; }
                if (bar) { c = new Color(0.05f, 0.08f, 0.06f); em = Color.black; }
            }
            t.SetPixel(x, y, c); e.SetPixel(x, y, em);
        }
        t.Apply(); e.Apply(); Save(t, "facade"); Save(e, "facade_emit");
    }

    static void Moon()
    {
        int S = 256; var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float u = (x - S / 2f) / (S / 2f), v = (y - S / 2f) / (S / 2f); float r = Mathf.Sqrt(u * u + v * v);
            float disc = Mathf.Clamp01((0.5f - r) * 60);
            float glow = Mathf.Pow(Mathf.Clamp01(1 - r), 3) * 0.5f;
            float n = 0.8f + 0.2f * FBM(u, v, 4);
            t.SetPixel(x, y, new Color(n, n, n * 0.95f, Mathf.Clamp01(disc + glow)));
        }
        t.Apply(); Save(t, "moon");
    }

    static void Streak()
    {
        var t = new Texture2D(16, 128, TextureFormat.RGBA32, false);
        for (int y = 0; y < 128; y++) for (int x = 0; x < 16; x++)
        {
            float a = Mathf.Clamp01(1 - Mathf.Abs(x - 7.5f) / 4f) * Mathf.Sin(Mathf.PI * y / 127f);
            t.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        t.Apply(); Save(t, "streak");
    }

    static void Glow()
    {
        int S = 128; var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float u = (x - S / 2f) / (S / 2f), v = (y - S / 2f) / (S / 2f); float r = Mathf.Sqrt(u * u + v * v);
            t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - r), 2.5f)));
        }
        t.Apply(); Save(t, "glow");
    }

    static void Board()
    {
        int W = 512, H = 256; var t = new Texture2D(W, H, TextureFormat.RGB24, false);
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            float u = x / (float)W, v = y / (float)H; float n = FBM(u * 2, v, 5);
            Color c = new Color(0.05f, 0.18f, 0.42f) * (0.8f + 0.35f * n);
            float b = Mathf.Min(Mathf.Min(u, 1 - u) * 2, Mathf.Min(v, 1 - v));
            if (b < 0.035f) c = new Color(0.85f, 0.83f, 0.78f) * (0.8f + 0.2f * n);
            float rust = Mathf.Clamp01((FBM(u * 3 + 5, v * 3, 4) - 0.6f) * 5);
            c = Color.Lerp(c, new Color(0.35f, 0.18f, 0.08f), rust * 0.7f);
            t.SetPixel(x, y, c);
        }
        t.Apply(); Save(t, "board");
    }
}
