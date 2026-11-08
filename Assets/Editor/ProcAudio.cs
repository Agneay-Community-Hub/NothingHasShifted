using System; using System.IO; using UnityEditor; using UnityEngine;
// Procedurally synthesises every sound in the game into WAV files (no external audio needed).
public static class ProcAudio
{
    const int SR = 44100;
    const string Dir = "Assets/Game/Audio";
    static System.Random rng = new System.Random(1234);
    static float R() => (float)rng.NextDouble() * 2f - 1f;
    static float U() => (float)rng.NextDouble();

    class LP { float y; float a; public LP(float hz) { Set(hz); } public void Set(float hz) { a = 1f - Mathf.Exp(-2f * Mathf.PI * hz / SR); } public float P(float x) { y += a * (x - y); return y; } }
    class HP { LP l; public HP(float hz) { l = new LP(hz); } public float P(float x) => x - l.P(x); }
    class BP { float b0, b1, b2, a1, a2, x1, x2, y1, y2; public BP(float f, float q) { Set(f, q); }
        public void Set(float f, float q) { float w = 2 * Mathf.PI * f / SR, al = Mathf.Sin(w) / (2 * q), a0 = 1 + al; b0 = al / a0; b1 = 0; b2 = -al / a0; a1 = -2 * Mathf.Cos(w) / a0; a2 = (1 - al) / a0; }
        public float P(float x) { float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2; x2 = x1; x1 = x; y2 = y1; y1 = y; return y; } }

    static void Save(string name, float[] L, float[] Rt = null, float gain = 0.9f)
    {
        Directory.CreateDirectory(Dir);
        int ch = Rt == null ? 1 : 2; int n = L.Length;
        float peak = 1e-6f; for (int i = 0; i < n; i++) { peak = Mathf.Max(peak, Mathf.Abs(L[i])); if (Rt != null) peak = Mathf.Max(peak, Mathf.Abs(Rt[i])); }
        float g = gain / peak;
        using (var bw = new BinaryWriter(File.Create(Path.Combine(Dir, name + ".wav"))))
        {
            int bytes = n * ch * 2;
            bw.Write(new[] { 'R', 'I', 'F', 'F' }); bw.Write(36 + bytes); bw.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            bw.Write(16); bw.Write((short)1); bw.Write((short)ch); bw.Write(SR); bw.Write(SR * ch * 2); bw.Write((short)(ch * 2)); bw.Write((short)16);
            bw.Write(new[] { 'd', 'a', 't', 'a' }); bw.Write(bytes);
            for (int i = 0; i < n; i++) { bw.Write((short)(Mathf.Clamp(L[i] * g, -1, 1) * 32767)); if (Rt != null) bw.Write((short)(Mathf.Clamp(Rt[i] * g, -1, 1) * 32767)); }
        }
    }
    static float[] Buf(float sec) => new float[(int)(sec * SR)];
    static void LoopXfade(float[] a, float xf)
    {
        int x = (int)(xf * SR), n = a.Length - x;
        for (int i = 0; i < x; i++) { float t = (float)i / x; a[i] = a[i] * t + a[n + i] * (1 - t); }
        Array.Resize(ref a, n);
    }
    static float[] Trim(float[] a, float xf) { int x = (int)(xf * SR); var b = new float[a.Length - x]; for (int i = 0; i < b.Length; i++) { float v = a[i]; if (i < x) { float t = (float)i / x; v = a[i] * t + a[a.Length - x + i] * (1 - t); } b[i] = v; } return b; }
    static void Reverb(float[] a, float mix, float decay = 0.45f)
    {
        int[] d = { 1557, 1617, 1491, 1422, 1277, 1356 };
        var outp = new float[a.Length];
        foreach (var dl in d) { var buf = new float[dl]; int p = 0; for (int i = 0; i < a.Length; i++) { float y = buf[p]; buf[p] = a[i] + y * (decay + 0.3f); p = (p + 1) % dl; outp[i] += y / d.Length; } }
        for (int i = 0; i < a.Length; i++) a[i] = a[i] * (1 - mix) + outp[i] * mix * 2.2f;
    }

    [MenuItem("Game/Generate Audio")]
    public static void All()
    {
        Rain(); Frogs(); Hum(); Fan(); Steps(); Thunder(); Knock(); Creak(); Scream(); Wrong(); Right(); Stairs(); Clunk(); Drip(); Squeak(); Slap(); Whisper(); Drone();
        AssetDatabase.Refresh();
    }

    static void Rain()
    {
        float sec = 14f; var L = Buf(sec); var Rr = Buf(sec);
        var lp1 = new LP(900); var lp2 = new LP(900); var hp1 = new HP(250); var hp2 = new HP(250); var hs1 = new LP(5000); var hs2 = new LP(5000);
        float br1 = 0, br2 = 0;
        for (int i = 0; i < L.Length; i++)
        {
            br1 = br1 * 0.995f + R() * 0.05f; br2 = br2 * 0.995f + R() * 0.05f;
            float wl = hp1.P(lp1.P(R())) * 0.6f + br1 * 0.7f + hs1.P(R()) * 0.25f;
            float wr = hp2.P(lp2.P(R())) * 0.6f + br2 * 0.7f + hs2.P(R()) * 0.25f;
            L[i] = wl; Rr[i] = wr;
        }
        // droplets / splashes on the parapet
        for (int k = 0; k < 900; k++)
        {
            int s = rng.Next(L.Length); float f = 1500 + U() * 4000; float amp = 0.05f + U() * 0.25f; float pan = U();
            var bp = new BP(f, 6);
            for (int j = 0; j < 1200 && s + j < L.Length; j++) { float v = bp.P(R() * Mathf.Exp(-j / 120f)) * amp; L[s + j] += v * (1 - pan); Rr[s + j] += v * pan; }
        }
        Save("rain_loop", Trim(L, 1.5f), Trim(Rr, 1.5f), 0.7f);
    }

    static void Frogs()
    {
        float sec = 12f; var L = Buf(sec); var Rr = Buf(sec);
        for (int frog = 0; frog < 5; frog++)
        {
            float pitch = 220 + U() * 420, pan = U(), period = 0.7f + U() * 1.4f, rate = 22 + U() * 30, amp = 0.3f + U() * 0.7f;
            float t0 = U() * period;
            for (float t = t0; t < sec; t += period * (0.85f + U() * 0.3f))
            {
                int calls = 1 + rng.Next(3);
                for (int c = 0; c < calls; c++)
                {
                    float st = t + c * 0.32f; float dur = 0.18f + U() * 0.12f;
                    int s0 = (int)(st * SR), n = (int)(dur * SR);
                    for (int j = 0; j < n; j++)
                    {
                        int idx = (s0 + j) % L.Length; float tt = (float)j / SR;
                        float env = Mathf.Sin(Mathf.PI * j / n);
                        float pulse = Mathf.Pow(Mathf.Max(0, Mathf.Sin(2 * Mathf.PI * rate * tt)), 3);
                        float v = (Mathf.Sin(2 * Mathf.PI * pitch * tt) + 0.5f * Mathf.Sin(2 * Mathf.PI * pitch * 2.02f * tt)) * pulse * env * amp;
                        L[idx] += v * (1 - pan); Rr[idx] += v * pan;
                    }
                }
            }
        }
        Save("frogs_loop", L, Rr, 0.6f);
    }

    static void Hum()
    {
        var a = Buf(2f);
        for (int i = 0; i < a.Length; i++)
        {
            float t = (float)i / SR;
            float s = Mathf.Sin(2 * Mathf.PI * 100 * t) * 0.6f + Mathf.Sin(2 * Mathf.PI * 200 * t) * 0.25f + Mathf.Sin(2 * Mathf.PI * 300 * t) * 0.12f;
            float buzz = Mathf.Clamp(Mathf.Sin(2 * Mathf.PI * 100 * t) * 6f, -1, 1) * 0.08f;
            a[i] = s + buzz + R() * 0.01f;
        }
        Save("tube_hum", a, null, 0.5f);
    }

    static void Fan()
    {
        var a = Buf(4.0f); var lp = new LP(600); var bp = new BP(180, 1.2f);
        for (int i = 0; i < a.Length; i++)
        {
            float t = (float)i / SR;
            float mod = 0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * 4.5f * t);
            a[i] = lp.P(R()) * 1.6f * mod + bp.P(R()) * 0.4f + Mathf.Sin(2 * Mathf.PI * 50 * t) * 0.05f + Mathf.Sin(2 * Mathf.PI * 4.5f * t * 30) * 0.004f;
        }
        Save("fan_loop", Trim(a, 0.5f), null, 0.5f);
    }

    static void StepInto(float[] a, int at, float vol, float tone)
    {
        // chappal: heel tap + rubber slap against the sole ~70ms later
        var lp = new LP(1800 * tone); var bp = new BP(260 * tone, 1.5f);
        for (int j = 0; j < 4000 && at + j < a.Length; j++) a[at + j] += (lp.P(R()) * 1.2f + bp.P(R()) * 2f) * Mathf.Exp(-j / 260f) * vol;
        int s2 = at + (int)((0.06f + U() * 0.03f) * SR); var bp2 = new BP(1100 * tone, 1.2f); var lp2 = new LP(3500);
        for (int j = 0; j < 3000 && s2 + j < a.Length; j++) a[s2 + j] += (bp2.P(R()) * 2.2f + lp2.P(R()) * 0.5f) * Mathf.Exp(-j / 160f) * vol * 0.8f;
    }
    static void Steps()
    {
        for (int k = 0; k < 4; k++) { var a = Buf(0.35f); StepInto(a, 10, 1, 0.9f + U() * 0.25f); Save("step" + k, a, null, 0.8f); }
    }

    static void Thunder()
    {
        for (int k = 0; k < 2; k++)
        {
            float sec = 8f; var L = Buf(sec); var lp = new LP(140); var lp2 = new LP(500); float br = 0;
            var env = new float[L.Length];
            float[] swells = { 0.2f + U() * 0.3f, 1.2f + U(), 2.5f + U() * 1.5f, 4f + U() * 1.5f };
            for (int i = 0; i < L.Length; i++)
            {
                float t = (float)i / SR; float e = 0;
                foreach (var s in swells) e += Mathf.Exp(-Mathf.Pow((t - s) / 0.7f, 2)) * (1.2f - s * 0.15f);
                e *= Mathf.Clamp01(t * 3); e *= Mathf.Clamp01((sec - t) / 2.5f);
                br = br * 0.998f + R() * 0.06f;
                L[i] = (lp.P(R()) * 3f + br * 1.5f + lp2.P(R()) * 0.4f) * e;
                if (k == 0 && t < 0.25f) L[i] += R() * Mathf.Exp(-t * 20) * 0.6f;
            }
            Save("thunder" + k, L, null, 0.95f);
        }
    }

    static void Knock()
    {
        var a = Buf(1.6f);
        float[] times = { 0.05f, 0.27f, 0.47f, 0.95f, 1.12f };
        foreach (var tt in times)
        {
            int s = (int)(tt * SR); var lp = new LP(900);
            float f = 95 + U() * 20;
            for (int j = 0; j < 9000 && s + j < a.Length; j++) { float t = (float)j / SR; a[s + j] += (Mathf.Sin(2 * Mathf.PI * f * t) * 1.2f + Mathf.Sin(2 * Mathf.PI * f * 2.7f * t) * 0.4f) * Mathf.Exp(-t * 28) + lp.P(R()) * Mathf.Exp(-t * 90) * 1.5f; }
        }
        Reverb(a, 0.25f);
        Save("knock", a);
    }

    static void Creak()
    {
        var a = Buf(2.4f); var bp = new BP(850, 6); var bp2 = new BP(1700, 8);
        float next = 0; float t = 0;
        for (int i = 0; i < a.Length; i++)
        {
            t = (float)i / SR;
            float f = 120 + 90 * Mathf.Sin(t * 1.7f) + 30 * Mathf.Sin(t * 7.3f);
            float x = 0;
            if (t >= next) { x = 1 + R() * 0.3f; next = t + (1f / f) * (1 + R() * 0.25f); }
            float env = Mathf.Clamp01(t * 4) * Mathf.Clamp01((2.4f - t) * 2);
            a[i] = (bp.P(x) * 2 + bp2.P(x)) * env;
        }
        Reverb(a, 0.3f);
        Save("creak", a);
    }

    static void Scream()
    {
        float sec = 1.8f; var a = Buf(sec); float[] ph = new float[6]; var hp = new HP(300);
        for (int i = 0; i < a.Length; i++)
        {
            float t = (float)i / SR; float env = Mathf.Clamp01(t * 30) * Mathf.Exp(-Mathf.Max(0, t - 0.6f) * 3);
            float baseF = Mathf.Lerp(700, 1300, Mathf.Clamp01(t * 2)) * (1 + 0.05f * Mathf.Sin(2 * Mathf.PI * 9 * t));
            float s = 0;
            for (int k = 0; k < 6; k++) { ph[k] += baseF * (1 + (k - 2.5f) * 0.013f) * (k % 2 == 0 ? 1 : 1.5f) / SR; s += (ph[k] % 1f) * 2 - 1; }
            s = (float)Math.Tanh(s * 1.4f) + hp.P(R()) * 0.7f;
            float boom = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(90, 35, t) * t) * Mathf.Exp(-t * 3) * 1.4f;
            a[i] = s * env * 0.7f + boom;
        }
        Reverb(a, 0.3f);
        Save("scream", a, null, 0.98f);
    }

    static void Wrong()
    {
        float sec = 4f; var a = Buf(sec);
        float[] fr = { 110f, 116.5f, 155.6f, 164.8f, 233f };
        for (int i = 0; i < a.Length; i++)
        {
            float t = (float)i / SR; float s = 0;
            foreach (var f in fr) s += Mathf.Sin(2 * Mathf.PI * f * t + Mathf.Sin(t * 3) * 0.5f);
            float env = Mathf.Clamp01(t * 8) * Mathf.Exp(-t * 0.9f);
            float boom = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(70, 28, Mathf.Clamp01(t)) * t) * Mathf.Exp(-t * 1.5f) * 3;
            a[i] = s * env * 0.4f + boom + R() * Mathf.Exp(-t * 8) * 0.3f;
        }
        Reverb(a, 0.35f, 0.55f);
        Save("wrong_sting", a);
    }

    static void Right()
    {
        var a = Buf(3f); float[] p = { 1f, 2.76f, 5.4f, 8.93f };
        for (int i = 0; i < a.Length; i++) { float t = (float)i / SR; float s = 0; for (int k = 0; k < p.Length; k++) s += Mathf.Sin(2 * Mathf.PI * 196 * p[k] * t) * Mathf.Exp(-t * (1.2f + k * 1.4f)) / (k + 1); a[i] = s * Mathf.Clamp01(t * 200); }
        Reverb(a, 0.3f);
        Save("right_tone", a, null, 0.5f);
    }

    static void Stairs()
    {
        var a = Buf(3.2f); float t = 0.05f;
        for (int k = 0; k < 7; k++) { StepInto(a, (int)(t * SR), 1f - k * 0.1f, 0.85f); t += 0.42f + k * 0.02f; }
        Reverb(a, 0.4f, 0.5f);
        Save("stairs", a);
    }

    static void Clunk()
    {
        var a = Buf(1.2f); var lp = new LP(2500);
        for (int i = 0; i < a.Length; i++)
        {
            float t = (float)i / SR;
            float click = lp.P(R()) * Mathf.Exp(-t * 120) * 2;
            float thunk = Mathf.Sin(2 * Mathf.PI * 70 * t) * Mathf.Exp(-t * 14);
            float hum = Mathf.Sin(2 * Mathf.PI * 100 * t) * Mathf.Exp(-t * 6) * 0.3f;
            a[i] = click + thunk + hum;
        }
        Reverb(a, 0.3f);
        Save("clunk", a);
    }

    static void Drip()
    {
        var a = Buf(0.6f);
        for (int i = 0; i < a.Length; i++) { float t = (float)i / SR; float f = 900 + 1400 * Mathf.Exp(-t * 40); a[i] = Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 18); }
        Reverb(a, 0.3f);
        Save("drip", a, null, 0.6f);
    }

    static void Squeak()
    {
        var a = Buf(1.6f); float ph = 0;
        for (int i = 0; i < a.Length; i++)
        {
            float t = (float)i / SR; float cyc = (t * 1.25f) % 1f;
            float env = cyc < 0.35f ? Mathf.Sin(Mathf.PI * cyc / 0.35f) : 0;
            float f = 1700 + 400 * Mathf.Sin(t * 25) + 200 * cyc;
            ph += f / SR; a[i] = (Mathf.Sin(2 * Mathf.PI * ph) + 0.3f * Mathf.Sin(4 * Mathf.PI * ph)) * env * 0.5f + R() * 0.02f;
        }
        Save("squeak", a, null, 0.4f);
    }

    static void Slap()
    {
        var a = Buf(0.8f); var lp = new LP(1200); var bp = new BP(400, 2);
        for (int i = 0; i < a.Length; i++) { float t = (float)i / SR; a[i] = (lp.P(R()) * 1.5f + bp.P(R()) * 2) * Mathf.Exp(-t * 30); }
        Reverb(a, 0.35f);
        Save("slap", a, null, 0.7f);
    }

    static void Whisper()
    {
        var a = Buf(2.5f); var b1 = new BP(700, 4); var b2 = new BP(1300, 5); var b3 = new BP(2600, 6); var hp = new HP(400);
        for (int i = 0; i < a.Length; i++)
        {
            float t = (float)i / SR;
            b1.Set(500 + 300 * Mathf.Sin(t * 5.1f), 4); b2.Set(1200 + 500 * Mathf.Sin(t * 3.7f + 1), 5);
            float n = hp.P(R());
            float env = Mathf.Clamp01(t * 2) * Mathf.Clamp01((2.5f - t) * 1.5f) * (0.6f + 0.4f * Mathf.Sin(t * 11));
            a[i] = (b1.P(n) + b2.P(n) + b3.P(n) * 0.5f) * env;
        }
        Reverb(a, 0.45f, 0.55f);
        Save("whisper", a, null, 0.6f);
    }

    static void Drone()
    {
        float sec = 16f; var L = Buf(sec); var Rr = Buf(sec); var lp = new LP(200); var lp2 = new LP(200);
        for (int i = 0; i < L.Length; i++)
        {
            float t = (float)i / SR;
            float s = Mathf.Sin(2 * Mathf.PI * 55 * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 82.5f * t + Mathf.Sin(t * 0.25f * Mathf.PI * 2) * 2) * 0.25f;
            L[i] = s + lp.P(R()) * 0.8f; Rr[i] = s + lp2.P(R()) * 0.8f;
        }
        Save("drone_loop", Trim(L, 1f), Trim(Rr, 1f), 0.5f);
    }
}
