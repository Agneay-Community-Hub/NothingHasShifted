using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Builds the whole game scene from code: hostel corridor, landings, courtyard, lighting, audio, anomalies.
public static partial class SceneBuilder
{
    const string PH = "Assets/ThirdParty/PolyHaven/";
    const string ART = "Assets/Game/Art/Models/";
    const string MAT = "Assets/Game/Materials/";
    const string TEX = "Assets/Game/Textures/";
    const string AUD = "Assets/Game/Audio/";
    public const float CW = 1.4f, CH = 3.2f, LEN = 30f, LAND = 4.4f;
    public static readonly float[] DoorZ = { 1.875f, 5.625f, 9.375f, 13.125f, 16.875f, 20.625f, 24.375f, 28.125f };

    static Transform building, props, lights, anomaliesRoot;
    static Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static TMP_FontAsset fType, fSign, fHand;

    // ------------------------------------------------------------------ helpers
    static T Load<T>(string p) where T : Object { var a = AssetDatabase.LoadAssetAtPath<T>(p); if (a == null) Debug.LogError("Missing " + p); return a; }
    static AudioClip Clip(string n) => Load<AudioClip>(AUD + n + ".wav");

    static GameObject GO(string name, Transform parent, Vector3 pos, Vector3 euler = default)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localEulerAngles = euler; return g;
    }

    static GameObject Model(string path, Transform parent, Vector3 pos, Vector3 euler, float scale = 1f, string name = null)
    {
        var a = Load<GameObject>(path);
        var g = (GameObject)PrefabUtility.InstantiatePrefab(a);
        PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localEulerAngles = euler; g.transform.localScale = Vector3.one * scale;
        if (name != null) g.name = name;
        return g;
    }
    static GameObject PHM(string id, Transform parent, Vector3 pos, Vector3 euler, float scale = 1f) => Model(PH + "Models/" + id + "/" + id + ".gltf", parent, pos, euler, scale, id);
    static GameObject Art(string id, Transform parent, Vector3 pos, Vector3 euler, float scale = 1f) => Model(ART + id + ".glb", parent, pos, euler, scale, id);

    static void KeepOnly(GameObject g, params string[] names)
    {
        var all = g.GetComponentsInChildren<Renderer>(true).ToList();
        var keep = all.Where(r => names.Contains(r.name)).ToList();
        foreach (var r in all)
        {
            if (!r || keep.Contains(r)) continue;
            if (keep.Any(k => k && k.transform.IsChildOf(r.transform)))
            {
                var mf = r.GetComponent<MeshFilter>(); Object.DestroyImmediate(r); if (mf) Object.DestroyImmediate(mf);
            }
            else Object.DestroyImmediate(r.gameObject);
        }
    }
    static void Recentre(GameObject g)
    {
        // move children so that renderer bounds are centred on x/z and sit on y=0
        var rs = g.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return;
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        var off = g.transform.InverseTransformVector(new Vector3(b.center.x - g.transform.position.x, b.min.y - g.transform.position.y, b.center.z - g.transform.position.z));
        foreach (Transform c in g.transform) c.localPosition -= off;
    }

    static Mesh BoxMesh(Vector3 s, float tile)
    {
        var m = new Mesh(); m.name = "Box_" + s;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        Vector3 h = s * 0.5f;
        void Face(Vector3 normal, Vector3 u, Vector3 w, float su, float sw)
        {
            int i = v.Count; Vector3 c = Vector3.Scale(normal, h);
            Vector3 hu = u * su * 0.5f, hw = w * sw * 0.5f;
            v.Add(c - hu - hw); v.Add(c + hu - hw); v.Add(c + hu + hw); v.Add(c - hu + hw);
            for (int k = 0; k < 4; k++) n.Add(normal);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(su / tile, 0)); uv.Add(new Vector2(su / tile, sw / tile)); uv.Add(new Vector2(0, sw / tile));
            t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
        }
        Face(Vector3.up, Vector3.right, Vector3.forward, s.x, s.z);
        Face(Vector3.down, Vector3.right, Vector3.back, s.x, s.z);
        Face(Vector3.forward, Vector3.left, Vector3.up, s.x, s.y);
        Face(Vector3.back, Vector3.right, Vector3.up, s.x, s.y);
        Face(Vector3.right, Vector3.forward, Vector3.up, s.z, s.y);
        Face(Vector3.left, Vector3.back, Vector3.up, s.z, s.y);
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateTangents(); m.RecalculateBounds();
        return m;
    }

    static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();
    static GameObject Box(string name, Transform parent, Vector3 min, Vector3 max, string mat, float tile = 1f, bool collider = true)
    {
        var size = max - min; var center = (min + max) * 0.5f;
        string key = size.ToString("F3") + tile;
        if (!meshCache.TryGetValue(key, out var mesh))
        {
            mesh = BoxMesh(size, tile);
            AssetDatabase.AddObjectToAsset(mesh, meshHolder);
            meshCache[key] = mesh;
        }
        var g = GO(name, parent, center);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        g.AddComponent<MeshRenderer>().sharedMaterial = mats[mat];
        if (collider) { var bc = g.AddComponent<BoxCollider>(); bc.size = size; }
        g.isStatic = true;
        return g;
    }
    static Object meshHolder;

    static GameObject Quad(string name, Transform parent, Vector3 pos, Vector3 euler, Vector2 size, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Quad); g.name = name;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localEulerAngles = euler; g.transform.localScale = new Vector3(size.x, size.y, 1);
        g.GetComponent<MeshRenderer>().sharedMaterial = m;
        return g;
    }

    static TextMeshPro Text(string name, Transform parent, Vector3 pos, Vector3 euler, string s, TMP_FontAsset f, float size, Color c, Vector2 rect, TextAlignmentOptions al = TextAlignmentOptions.Center)
    {
        var g = GO(name, parent, pos, euler);
        var t = g.AddComponent<TextMeshPro>();
        t.font = f; t.text = s; t.fontSize = size; t.color = c; t.alignment = al;
        t.rectTransform.sizeDelta = rect; t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    // ------------------------------------------------------------------ materials
    static void ImportSetup()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PH + "Textures" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var imp = (TextureImporter)AssetImporter.GetAtPath(p);
            bool nrm = p.Contains("_nor_gl");
            bool lin = p.Contains("_Rough") || p.Contains("_arm") || p.Contains("_AO");
            bool changed = false;
            if (nrm && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; changed = true; }
            if (lin && imp.sRGBTexture) { imp.sRGBTexture = false; changed = true; }
            if (imp.maxTextureSize > 1024) { imp.maxTextureSize = 1024; changed = true; }
            if (changed) imp.SaveAndReimport();
        }
    }

    static Material Lit(string name, string phTex, Color tint, float smooth = 0.3f, float metal = 0f, float normal = 1f)
    {
        var path = MAT + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find("Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
        if (phTex != null)
        {
            m.SetTexture("_BaseMap", Load<Texture2D>(PH + "Textures/" + phTex + "/" + phTex + "_Diffuse.jpg"));
            var nm = AssetDatabase.LoadAssetAtPath<Texture2D>(PH + "Textures/" + phTex + "/" + phTex + "_nor_gl.jpg");
            if (nm) { m.SetTexture("_BumpMap", nm); m.SetFloat("_BumpScale", normal); m.EnableKeyword("_NORMALMAP"); }
        }
        EditorUtility.SetDirty(m);
        mats[name] = m;
        return m;
    }
    static Material Simple(string name, Color c, float smooth = 0.3f, float metal = 0, Texture tex = null, Color? emis = null, Texture emisTex = null, bool cutout = false, bool unlit = false)
    {
        var path = MAT + name + ".mat";
        var sh = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh;
        m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
        if (tex) m.SetTexture("_BaseMap", tex);
        if (emis.HasValue)
        {
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emis.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (emisTex) m.SetTexture("_EmissionMap", emisTex);
        }
        if (cutout) { m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", 0.4f); m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450; }
        EditorUtility.SetDirty(m);
        mats[name] = m; return m;
    }
    static Material Transparent(string name, Color c, Texture tex, bool additive)
    {
        var path = MAT + name + ".mat";
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh;
        m.SetColor("_BaseColor", c); m.SetTexture("_BaseMap", tex);
        m.SetFloat("_Surface", 1); m.SetFloat("_Blend", additive ? 2 : 0);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
        EditorUtility.SetDirty(m);
        mats[name] = m; return m;
    }

    static TMP_FontAsset Font(string ttf, string name)
    {
        var path = "Assets/Game/Fonts/" + name + ".asset";
        var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (fa) return fa;
        var font = Load<Font>("Assets/Game/Fonts/" + ttf);
        fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        fa.name = name;
        AssetDatabase.CreateAsset(fa, path);
        fa.atlasTextures[0].name = name + " Atlas"; AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = name + " Material"; AssetDatabase.AddObjectToAsset(fa.material, fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    static void MakeMaterials()
    {
        AssetDatabase.CreateFolder("Assets/Game", "Materials");
        Lit("Terrazzo", "terrazzo_tiles", new Color(0.92f, 0.9f, 0.86f), 0.62f);
        Lit("WallUpper", "white_plaster_rough_01", new Color(0.86f, 0.83f, 0.72f), 0.12f, 0, 0.6f);
        Lit("Dado", "painted_plaster_wall", new Color(0.33f, 0.52f, 0.42f), 0.5f, 0, 0.5f);
        Lit("Ceiling", "white_plaster_rough_01", new Color(0.72f, 0.72f, 0.68f), 0.05f);
        Lit("Ochre", "plastered_wall_03", new Color(0.95f, 0.72f, 0.4f), 0.12f, 0, 0.8f);
        Lit("Granite", "granite_tile", new Color(0.55f, 0.52f, 0.5f), 0.55f);
        Lit("DoorWood", "wood_peeling_paint_weathered", new Color(0.75f, 0.62f, 0.5f), 0.35f);
        Lit("GrilleMetal", "green_metal_rust", new Color(0.55f, 0.75f, 0.62f), 0.4f, 0.5f);
        Lit("Ground", "red_laterite_soil_stones", new Color(0.7f, 0.6f, 0.55f), 0.35f);
        Lit("OuterWall", "plastered_wall_03", new Color(0.72f, 0.66f, 0.55f), 0.1f);
        Lit("RustMetal", "rusty_painted_metal", new Color(0.6f, 0.6f, 0.6f), 0.35f, 0.4f);
        Simple("Skirting", new Color(0.22f, 0.07f, 0.05f), 0.5f);
        Simple("DadoLine", new Color(0.08f, 0.2f, 0.14f), 0.5f);
        Simple("Black", new Color(0.005f, 0.005f, 0.006f), 0f);
        Simple("Paper", Color.white, 0.05f, 0, Load<Texture2D>(TEX + "paper.png"));
        Simple("Board", Color.white, 0.4f, 0.2f, Load<Texture2D>(TEX + "board.png"));
        Simple("Cork", new Color(0.12f, 0.22f, 0.14f), 0.05f);
        Simple("NoticeFrame", new Color(0.28f, 0.16f, 0.08f), 0.4f);
        Simple("Rangoli", Color.white, 0.1f, 0, Load<Texture2D>(TEX + "rangoli.png"), null, null, true);
        Simple("Kolam", Color.white, 0.1f, 0, Load<Texture2D>(TEX + "kolam.png"), null, null, true);
        Simple("Hand", Color.white, 0.7f, 0, Load<Texture2D>(TEX + "hand.png"), null, null, true);
        Simple("Blood", Color.white, 0.85f, 0, Load<Texture2D>(TEX + "blood.png"), null, null, true);
        Simple("Facade", Color.white, 0.05f, 0, Load<Texture2D>(TEX + "facade.png"), Color.white * 1.6f, Load<Texture2D>(TEX + "facade_emit.png"));
        Simple("Lungi", Color.white, 0.05f, 0, Load<Texture2D>(TEX + "cloth_lungi.png"));
        Simple("Towel", Color.white, 0.05f, 0, Load<Texture2D>(TEX + "cloth_towel.png"));
        Simple("Gamcha", Color.white, 0.05f, 0, Load<Texture2D>(TEX + "cloth_gamcha.png"));
        Simple("SareeCloth", Color.white, 0.05f, 0, Load<Texture2D>(TEX + "cloth_saree.png"));
        Simple("Line", new Color(0.7f, 0.15f, 0.1f), 0.3f);
        Simple("Iron", new Color(0.09f, 0.09f, 0.09f), 0.45f, 0.8f);
        Simple("Eyes", Color.black, 0.9f, 0, null, new Color(4f, 3.2f, 2.6f));
        Simple("TubeGlow", Color.white, 0.5f, 0, null, new Color(2.2f, 2.4f, 2.6f));
        Simple("ExitGreen", new Color(0.02f, 0.25f, 0.06f), 0.3f, 0, null, new Color(0.02f, 0.45f, 0.1f));
        Simple("Sodium", new Color(1f, 0.6f, 0.2f), 0.3f, 0, null, new Color(4f, 2f, 0.6f));
        Simple("MoonDisc", Color.white, 0f, 0, Load<Texture2D>(TEX + "moon.png"), null, null, false, true);
        var md = mats["MoonDisc"]; md.SetFloat("_Surface", 1); md.SetFloat("_Blend", 0); md.SetOverrideTag("RenderType", "Transparent");
        md.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); md.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); md.SetFloat("_ZWrite", 0); md.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); md.renderQueue = 3000;
        Transparent("RainStreak", new Color(0.55f, 0.6f, 0.7f, 0.7f), Load<Texture2D>(TEX + "streak.png"), true);
        Transparent("Glow", new Color(1f, 0.6f, 0.25f, 0.5f), Load<Texture2D>(TEX + "glow.png"), true);
        Transparent("BloodDrip", new Color(0.35f, 0.0f, 0.0f, 1f), Load<Texture2D>(TEX + "glow.png"), false);
    }
}
