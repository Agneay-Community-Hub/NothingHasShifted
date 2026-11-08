using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using V = UnityEngine.Vector3;

public static partial class SceneBuilder
{
    static List<TMP_Text> signs = new List<TMP_Text>();
    static List<Door> doors = new List<Door>();
    static List<TubeLight> tubes = new List<TubeLight>();
    static List<Spinner> fans = new List<Spinner>();
    static List<Transform> chappals = new List<Transform>();
    static List<TMP_Text> notices = new List<TMP_Text>();
    static List<Transform> floatables = new List<Transform>();
    static Transform clockH, clockM, chair, wheelchair, ghost, ghostEyes, towel2;
    static GameObject coolerBlood, chappalCrowd, handprints, sareeCloth, roomEyes;
    static AudioSource rainSrc, frogSrc;
    static ParticleSystem rainPS;
    static Light moon;
    static Renderer moonDisc;

    [MenuItem("Game/Build Scene")]
    public static void Build()
    {
        mats.Clear(); meshCache.Clear(); signs.Clear(); doors.Clear(); tubes.Clear(); fans.Clear(); chappals.Clear(); notices.Clear(); floatables.Clear();
        ImportSetup();
        MakeMaterials();
        fType = Font("SpecialElite-Regular.ttf", "SpecialElite SDF");
        fSign = Font("Oswald.ttf", "Oswald SDF");
        fHand = Font("Kalam-Regular.ttf", "Kalam SDF");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (!AssetDatabase.IsValidFolder("Assets/Game/Scenes")) AssetDatabase.CreateFolder("Assets/Game", "Scenes");
        string holderPath = "Assets/Game/Art/GeneratedMeshes.asset";
        AssetDatabase.DeleteAsset(holderPath);
        meshHolder = ScriptableObject.CreateInstance<MeshHolder>();
        AssetDatabase.CreateAsset(meshHolder, holderPath);

        building = GO("Building", null, V.zero).transform;
        props = GO("Props", building, V.zero).transform;
        lights = GO("Lights", building, V.zero).transform;

        BuildCorridor();
        BuildLanding("LandingA", new V(0, 0, 0), 0, true);
        var landB = BuildLanding("LandingB", new V(0, 0, LEN), 180, false);
        BuildOutside();
        BuildRoom(5);
        BuildDoors();
        BuildProps();
        BuildLights();
        var game = BuildGame(landB.transform);
        BuildAnomalies(game);
        BuildPost();

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, "Assets/Game/Scenes/Main.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Game/Scenes/Main.unity", true) };
        Debug.Log("Scene built.");
    }

    // ------------------------------------------------------------------ doors
    static void BuildDoors()
    {
        var root = GO("Doors", building, V.zero).transform;
        int[] locked = { 1, 4, 7 };
        for (int i = 0; i < DoorZ.Length; i++)
        {
            var d = GO("Door" + i, root, new V(-1.5f, 0, DoorZ[i]), new V(0, 90, 0));
            var frame = Art("door_frame", d.transform, V.zero, V.zero);
            foreach (var r in frame.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mats["DoorWood"];
            var hinge = GO("Hinge", d.transform, new V(0.49f, 0.01f, 0));
            var leaf = Art("door_leaf", hinge.transform, V.zero, V.zero);
            foreach (var r in leaf.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials;
                for (int k = 0; k < ms.Length; k++) if (ms[k] && ms[k].name.Contains("DoorPaint")) ms[k] = mats["DoorWood"];
                r.sharedMaterials = ms;
            }
            var col = hinge.AddComponent<BoxCollider>(); col.center = new V(-0.49f, 1.07f, 0); col.size = new V(0.98f, 2.13f, 0.06f);
            var num = Text("Number", hinge.transform, new V(-0.49f, 1.78f, 0.05f), new V(0, 180, 0), "80" + (i + 1), fSign, 2.4f, new Color(0.95f, 0.92f, 0.85f), new Vector2(0.5f, 0.2f));
            var lockGo = Art("padlock", hinge.transform, new V(-0.93f, 1.06f, 0.05f), V.zero, 1.3f);
            lockGo.SetActive(locked.Contains(i));
            var door = d.AddComponent<Door>(); door.hinge = hinge.transform; door.padlock = lockGo; door.number = num;
            doors.Add(door);
        }
    }

    // ------------------------------------------------------------------ props
    static void BuildProps()
    {
        var p = props;
        // notice board
        Box("NoticeFrame", p, new V(-1.4f, 1.05f, 3.1f), new V(-1.36f, 1.95f, 4.4f), "NoticeFrame", 1f);
        Box("Cork", p, new V(-1.36f, 1.1f, 3.15f), new V(-1.355f, 1.9f, 4.35f), "Cork", 1f, false);
        string[] txt =
        {
            "<b>LOST</b>\nSteel water bottle\n(Milton, blue cap)\n\nIf found return to\nRoom 706 — Arjun",
            "<b>MESS MENU — THU</b>\nB: Idli, Sambar\nL: Rice, Rasam, Poriyal\nD: Chapati, Dal\n\n<i>No outside food!</i>",
            "<b>WATER CAN</b>\ndelivery 9 AM\nRs. 40 / can\n\n— Murugan Agencies",
            "<b>LIGHTS OUT\n10:30 PM</b>\n\n— Warden",
        };
        Vector3[] np = { new V(0, 1.68f, 3.45f), new V(0, 1.66f, 4.0f), new V(0, 1.3f, 3.45f), new V(0, 1.3f, 4.02f) };
        Vector2[] ns = { new Vector2(0.4f, 0.34f), new Vector2(0.44f, 0.38f), new Vector2(0.36f, 0.3f), new Vector2(0.34f, 0.28f) };
        for (int i = 0; i < 4; i++)
        {
            float rz = (i % 2 == 0 ? 1 : -1) * (1.5f + i);
            var q = GO("Notice" + i, p, new V(-1.352f, np[i].y, np[i].z), new V(0, -90, rz));
            Quad("Paper", q.transform, V.zero, V.zero, ns[i], mats["Paper"]);
            var t = Text("Text", q.transform, new V(0, 0, -0.003f), V.zero, txt[i], i == 0 ? fHand : fType, 0.3f, new Color(0.08f, 0.07f, 0.12f), ns[i] * 0.9f);
            notices.Add(t);
        }
        // SILENCE sign + clock
        Quad("SilenceBoard", p, new V(-1.385f, 2.55f, 11.25f), new V(0, -90, 0), new Vector2(1.2f, 0.3f), mats["Board"]);
        Text("Silence", p, new V(-1.375f, 2.55f, 11.25f), new V(0, -90, 0), "SILENCE PLEASE", fSign, 1.6f, new Color(0.92f, 0.9f, 0.84f), new Vector2(1.15f, 0.28f));
        var clock = PHM("wall_clock", p, new V(-1.38f, 2.0f, 11.25f), new V(0, 90, 0), 1.4f);
        foreach (var r in clock.GetComponentsInChildren<Renderer>()) if (r.name.ToLower().Contains("hand") || r.name.ToLower().Contains("arrow")) r.gameObject.SetActive(false);
        var pivot = GO("ClockPivot", p, new V(-1.32f, 2.0f, 11.25f), new V(0, 90, 0)).transform;
        clockH = GO("Hour", pivot, V.zero, new V(0, 0, -83.5f)).transform;
        clockM = GO("Minute", pivot, V.zero, new V(0, 0, -282f)).transform;
        Box("HourHand", clockH, new V(-0.008f, -0.01f, -0.003f), new V(0.008f, 0.08f, 0.003f), "Black", 1, false);
        Box("MinHand", clockM, new V(-0.005f, -0.012f, -0.005f), new V(0.005f, 0.13f, 0.001f), "Black", 1, false);
        // fire extinguisher, power box, switches
        PHM("korean_fire_extinguisher_01", p, new V(-1.27f, 0.9f, 7.5f), new V(0, 90, 0), 1.0f);
        PHM("power_box_01", p, new V(-1.35f, 2.0f, 7.5f), new V(0, 90, 0), 1.0f);
        // water cooler + cans
        var cooler = Art("water_cooler", p, new V(-1.07f, 0, 15f), new V(0, 90, 0));
        PHM("plastic_bottle_gallon", p, new V(-1.15f, 0, 15.75f), new V(0, 20, 0), 2.4f);
        floatables.Add(PHM("plastic_bottle_gallon", p, new V(-1.12f, 0, 16.15f), new V(0, -35, 0), 2.4f).transform);
        // chair
        chair = PHM("plastic_monobloc_chair_01", p, new V(0.85f, 0, 11.2f), new V(0, -90, 0), 1f).transform;
        chair.gameObject.AddComponent<BoxCollider>().center = new V(0, 0.45f, 0);
        chair.GetComponent<BoxCollider>().size = new V(0.6f, 0.9f, 0.6f);
        // bucket + mug, bat
        floatables.Add(Art("bucket_mug", p, new V(-1.12f, 0, DoorZ[2] + 0.78f), new V(0, 90, 0)).transform);
        floatables.Add(Art("cricket_bat", p, new V(-1.25f, 0.02f, DoorZ[1] + 0.75f), new V(0, 90, 12)).transform);
        // chappals at doors
        int[] withChappals = { 0, 2, 3, 4, 6 };
        foreach (var i in withChappals)
        {
            var c = Art("chappal_pair", p, new V(-1.0f, 0.002f, DoorZ[i] + 0.25f * (i % 2 == 0 ? 1 : -1)), new V(0, 90 + Random.Range(-25, 25), 0));
            chappals.Add(c.transform);
        }
        floatables.Add(chappals[1]);
        // toran + kolam + diya at door 3
        Art("marigold_toran", p, new V(-1.36f, 2.36f, DoorZ[3]), new V(0, 90, 0));
        Quad("Kolam", p, new V(-0.85f, 0.003f, DoorZ[3]), new V(90, 0, 0), new Vector2(0.9f, 0.9f), mats["Kolam"]);
        PHM("brass_diya_lantern", p, new V(-1.28f, 0, DoorZ[3] + 0.68f), V.zero, 1.2f);
        PHM("brass_pot_01", p, new V(-1.26f, 0, DoorZ[3] - 0.7f), V.zero, 0.9f);
        // gumboots at door 7
        var boots = PHM("rubber_boots", p, new V(-1.05f, 0, DoorZ[7] + 0.28f), new V(0, 90, 0), 1f);
        KeepOnly(boots, "rubber_boots_dirty_l", "rubber_boots_dirt_r"); Recentre(boots);
        // suitcase + boxes outside door 6
        var sc = PHM("vintage_suitcase", p, new V(-1.18f, 0, DoorZ[6] + 0.85f), new V(0, 90, 0), 1f);
        KeepOnly(sc, "vintage_suitcase_01_bottom", "vintage_suitcase_01_clasp", "vintage_suitcase_01_handle", "vintage_suitcase_01_top"); Recentre(sc);
        PHM("cardboard_box_01", p, new V(-1.15f, 0, DoorZ[6] - 0.85f), new V(0, 12, 0), 1.2f);
        floatables.Add(PHM("cardboard_box_01", p, new V(-1.15f, 0.41f, DoorZ[6] - 0.82f), new V(0, -8, 0), 1.0f).transform);
        // landing B corner: dustbin, broom, trash, rangoli
        var bin = PHM("metal_trash_can", p, new V(-1.05f, 0, LEN + 3.6f), V.zero, 1f);
        KeepOnly(bin, "metal_trash_can_rust", "metal_trash_can_rust_handle_left", "metal_trash_can_rust_handle_right", "metal_trash_can_rust_lid"); Recentre(bin);
        PHM("plastic_broom", p, new V(-1.25f, 0, LEN + 2.8f), new V(0, 90, 0), 1f);
        PHM("dustpan", p, new V(-1.1f, 0, LEN + 2.4f), new V(0, 70, 0), 1f);
        floatables.Add(PHM("trashbag", p, new V(-1.1f, 0, 29.3f), new V(0, 40, 0), 0.9f).transform);
        Quad("Rangoli", p, new V(0, 0.003f, LEN + 2.2f), new V(90, 0, 0), new Vector2(1.7f, 1.7f), mats["Rangoli"]);
        PHM("metal_stool_01", p, new V(0.9f, 0, 27f), new V(0, 15, 0), 1f);
        // clothes line on the balcony
        Box("ClothLine", p, new V(1.17f, 2.245f, 18.95f), new V(1.185f, 2.26f, 26.05f), "Line", 1, false);
        (string mat, float z, float w, float l)[] cl = { ("Towel", 19.6f, 1f, 1f), ("Lungi", 20.6f, 1.7f, 1.1f), ("Gamcha", 21.7f, 1.1f, 0.9f), ("Towel", 23.3f, 1f, 1f), ("Lungi", 24.5f, 1.6f, 1.15f) };
        foreach (var c in cl)
        {
            var g = Art("cloth_hanging", p, new V(1.178f, 2.25f, c.z), new V(0, 90, 0));
            g.transform.localScale = new V(c.w, c.l, 1);
            foreach (var r in g.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mats[c.mat];
            if (Mathf.Approximately(c.z, 23.3f)) towel2 = g.transform;
        }
        sareeCloth = Art("cloth_hanging", p, new V(1.178f, 2.25f, 23.3f), new V(0, 90, 0));
        sareeCloth.transform.localScale = new V(2.2f, 2.3f, 1);
        foreach (var r in sareeCloth.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mats["SareeCloth"];
        sareeCloth.SetActive(false);
        // wheelchair (hidden until its anomaly)
        wheelchair = PHM("wheelchair_01", p, new V(0.2f, 0, 23.5f), new V(0, 180, 0), 1f).transform;
        wheelchair.gameObject.SetActive(false);
        // ghost
        var gh = Art("ghost", null, new V(0, -50, 0), V.zero);
        gh.name = "Ghost";
        ghost = gh.transform;
        var eyes = GO("Eyes", ghost, new V(0, 1.735f, 0.128f)).transform;
        foreach (var ex in new[] { -0.035f, 0.035f })
        {
            var e = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.DestroyImmediate(e.GetComponent<Collider>());
            e.transform.SetParent(eyes, false); e.transform.localPosition = new V(ex, 0, 0); e.transform.localScale = new V(0.026f, 0.013f, 0.01f);
            e.GetComponent<Renderer>().sharedMaterial = mats["Eyes"];
        }
        ghostEyes = eyes;
        var gl = GO("GhostLight", ghost, new V(0, 1.5f, 0.6f)).AddComponent<Light>();
        gl.type = LightType.Point; gl.range = 2.6f; gl.intensity = 0.7f; gl.color = new Color(0.6f, 0.7f, 1f); gl.shadows = LightShadows.None;
        gh.SetActive(false);
        // room 5 eyes
        roomEyes = GO("RoomEyes", building, new V(-4.9f, 1.45f, DoorZ[5] - 1.2f), new V(0, 60, 0));
        foreach (var ex in new[] { -0.04f, 0.04f })
        {
            var e = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.DestroyImmediate(e.GetComponent<Collider>());
            e.transform.SetParent(roomEyes.transform, false); e.transform.localPosition = new V(ex, 0, 0); e.transform.localScale = new V(0.03f, 0.016f, 0.016f);
            e.GetComponent<Renderer>().sharedMaterial = mats["Eyes"];
        }
        roomEyes.SetActive(false);
        // anomaly-only props: blood under the cooler, crowd of chappals, handprints
        coolerBlood = GO("CoolerBlood", p, V.zero);
        Quad("Puddle", coolerBlood.transform, new V(-0.62f, 0.004f, 15f), new V(90, 0, 0), new Vector2(1.1f, 1.1f), mats["Blood"]);
        Quad("Puddle2", coolerBlood.transform, new V(-0.85f, 0.61f, 15f), new V(90, 0, 0), new Vector2(0.45f, 0.18f), mats["Blood"]);
        var drip = GO("Drip", coolerBlood.transform, new V(-0.78f, 0.74f, 15f)).AddComponent<ParticleSystem>();
        var dm = drip.main; dm.startLifetime = 0.45f; dm.startSpeed = 0; dm.startSize = 0.025f; dm.gravityModifier = 1f; dm.startColor = new Color(0.4f, 0, 0); dm.simulationSpace = ParticleSystemSimulationSpace.World;
        var de = drip.emission; de.rateOverTime = 6;
        var ds = drip.shape; ds.shapeType = ParticleSystemShapeType.Box; ds.scale = new V(0.01f, 0.01f, 0.3f);
        drip.GetComponent<ParticleSystemRenderer>().sharedMaterial = mats["BloodDrip"];
        coolerBlood.SetActive(false);
        chappalCrowd = GO("ChappalCrowd", p, V.zero);
        var rnd = new System.Random(5);
        for (int k = 0; k < 26; k++)
        {
            float x = -1.25f + (float)rnd.NextDouble() * 1.3f, z = DoorZ[1] - 0.9f + (float)rnd.NextDouble() * 1.8f;
            Art("chappal_pair", chappalCrowd.transform, new V(x, 0.002f, z), new V(0, 90 + (float)rnd.NextDouble() * 60 - 30, 0), 0.9f + (float)rnd.NextDouble() * 0.2f);
        }
        chappalCrowd.SetActive(false);
        handprints = GO("Handprints", p, V.zero);
        float[] hz = { 17.7f, 18.3f, 18.95f, 19.6f, 21.55f, 22.25f, 22.95f, 23.55f };
        for (int k = 0; k < hz.Length; k++)
        {
            float y = 0.9f + (float)rnd.NextDouble() * 1.0f;
            Quad("Hand" + k, handprints.transform, new V(-1.383f, y, hz[k]), new V(0, -90, (float)rnd.NextDouble() * 40 - 20), new Vector2(0.24f, 0.24f), mats["Hand"]);
        }
        handprints.SetActive(false);
    }

    // ------------------------------------------------------------------ lights
    static void BuildLights()
    {
        var root = lights;
        var hum = Clip("tube_hum"); var fanClip = Clip("fan_loop");
        var zs = DoorZ.ToList(); zs.Add(-2.2f); zs.Add(LEN + 2.2f);
        for (int i = 0; i < zs.Count; i++)
        {
            float z = zs[i];
            var t = GO("Tube" + i, root, new V(0, CH, z));
            var m = PHM("mounted_fluorescent_lights", t.transform, V.zero, new V(0, 90, 0), 1.25f);
            KeepOnly(m, "mounted_fluorescent_lights_a", "Cylinder.025_1");
            Recentre(m); m.transform.localPosition = new V(0, -0.05f, 0);
            Renderer tubeR = m.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name.StartsWith("Cylinder"));
            if (tubeR) tubeR.sharedMaterial = mats["TubeGlow"];
            var l = GO("Light", t.transform, new V(0, -0.25f, 0)).AddComponent<Light>();
            l.type = LightType.Point; l.range = 9.5f; l.intensity = 6.5f; l.color = new Color(0.86f, 0.95f, 1f); l.shadows = LightShadows.None;
            var tl = t.AddComponent<TubeLight>(); tl.lamp = l; tl.tube = tubeR; tl.intensity = 6.5f;
            tl.flickery = (i == 6 || i == zs.Count - 1);
            var a = t.AddComponent<AudioSource>(); a.clip = hum; a.loop = true; a.playOnAwake = true; a.volume = 0.07f; a.spatialBlend = 1; a.minDistance = 0.6f; a.maxDistance = 5f; a.rolloffMode = AudioRolloffMode.Linear;
            tl.hum = a;
            tubes.Add(tl);
        }
        foreach (var z in new[] { 7.5f, 15f, 22.5f })
        {
            var f = PHM("ceiling_fan", root, new V(0, CH, z), V.zero, 1f);
            var blades = f.GetComponentsInChildren<Renderer>().First(r => r.name == "ceiling_fan_blades").transform;
            var sp = blades.gameObject.AddComponent<Spinner>(); sp.speed = 290f;
            fans.Add(sp);
            var a = f.AddComponent<AudioSource>(); a.clip = fanClip; a.loop = true; a.playOnAwake = true; a.volume = 0.22f; a.spatialBlend = 1; a.minDistance = 1f; a.maxDistance = 7f; a.rolloffMode = AudioRolloffMode.Linear;
        }
        moon = GO("Moonlight", null, new V(0, 10, 0), new V(32, -72, 0)).AddComponent<Light>();
        moon.type = LightType.Directional; moon.intensity = 0.42f; moon.color = new Color(0.55f, 0.66f, 0.95f);
        moon.shadows = LightShadows.Soft; moon.shadowStrength = 0.9f;
        var md = Quad("MoonDisc", null, new V(60, 38, 30), new V(0, 90, 0), new Vector2(7, 7), mats["MoonDisc"]);
        moonDisc = md.GetComponent<Renderer>();
        var lt = GO("Lightning", null, new V(0, 10, 0), new V(25, -80, 0)).AddComponent<Light>();
        lt.type = LightType.Directional; lt.intensity = 0; lt.color = new Color(0.75f, 0.8f, 1f); lt.shadows = LightShadows.None;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.1f, 0.11f, 0.14f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.025f, 0.03f, 0.045f); RenderSettings.fogDensity = 0.034f;
        RenderSettings.skybox = null;

        var probe = GO("ReflectionProbe", null, new V(0, 1.6f, LEN / 2)).AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.size = new V(3.4f, 3.6f, LEN + 2 * LAND); probe.boxProjection = true; probe.resolution = 128; probe.intensity = 0.8f;

        // rain
        var rainGo = GO("Rain", null, new V(6.2f, 6f, LEN / 2), new V(90, 0, 0));
        rainPS = rainGo.AddComponent<ParticleSystem>();
        var mn = rainPS.main; mn.startLifetime = 1.6f; mn.startSpeed = 13f; mn.startSize = 0.05f; mn.maxParticles = 14000; mn.simulationSpace = ParticleSystemSimulationSpace.World;
        mn.startColor = new Color(0.8f, 0.85f, 0.95f, 0.9f); mn.prewarm = true;
        var em = rainPS.emission; em.rateOverTime = 8500;
        var sh = rainPS.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new V(8.5f, LEN + 14, 0.1f);
        var vo = rainPS.velocityOverLifetime; vo.enabled = true; vo.space = ParticleSystemSimulationSpace.World; vo.x = new ParticleSystem.MinMaxCurve(-1.2f, -0.4f); vo.y = new ParticleSystem.MinMaxCurve(0f, 0f); vo.z = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        // drips falling off the chajja edge, right in front of the player
        var dripGo = GO("ChajjaDrips", null, new V(2.33f, CH - 0.12f, LEN / 2), new V(90, 0, 0));
        var dps = dripGo.AddComponent<ParticleSystem>();
        var dmn = dps.main; dmn.startLifetime = 1.4f; dmn.startSpeed = 0.5f; dmn.startSize = 0.03f; dmn.gravityModifier = 1.2f; dmn.maxParticles = 3000; dmn.simulationSpace = ParticleSystemSimulationSpace.World; dmn.prewarm = true;
        dmn.startColor = new Color(0.8f, 0.85f, 0.95f, 0.9f);
        var dem = dps.emission; dem.rateOverTime = 900;
        var dsh = dps.shape; dsh.shapeType = ParticleSystemShapeType.Box; dsh.scale = new V(0.04f, LEN + 8, 0.01f);
        var dpr = dripGo.GetComponent<ParticleSystemRenderer>(); dpr.renderMode = ParticleSystemRenderMode.Stretch; dpr.velocityScale = 0.05f; dpr.lengthScale = 2f; dpr.sharedMaterial = mats["RainStreak"];
        dripGo.transform.SetParent(rainGo.transform, true);
        var pr = rainGo.GetComponent<ParticleSystemRenderer>(); pr.renderMode = ParticleSystemRenderMode.Stretch; pr.velocityScale = 0.06f; pr.lengthScale = 1f; pr.sharedMaterial = mats["RainStreak"];
    }

    // ------------------------------------------------------------------ player + managers
    static GameObject BuildGame(Transform landB)
    {
        var player = new GameObject("Player");
        player.transform.position = new V(0, 0.05f, -2.2f);
        var cc = player.AddComponent<CharacterController>(); cc.height = 1.75f; cc.radius = 0.28f; cc.center = new V(0, 0.875f, 0); cc.stepOffset = 0.3f; cc.slopeLimit = 50;
        var camGo = GO("Camera", player.transform, new V(0, 1.62f, 0));
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>(); cam.nearClipPlane = 0.04f; cam.farClipPlane = 120; cam.fieldOfView = 68;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.025f, 0.04f);
        camGo.AddComponent<AudioListener>();
        var cd = camGo.AddComponent<UniversalAdditionalCameraData>(); cd.renderPostProcessing = true; cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var pc = player.AddComponent<PlayerController>(); pc.cam = cam;
        var feet = player.AddComponent<AudioSource>(); feet.spatialBlend = 0; feet.volume = 0.8f; pc.feet = feet;
        pc.steps = new[] { Clip("step0"), Clip("step1"), Clip("step2"), Clip("step3") };

        var game = new GameObject("Game");
        var w = game.AddComponent<World>();
        var gm = game.AddComponent<GameManager>();
        var ui = game.AddComponent<UIController>(); ui.typeFont = fType; ui.signFont = fSign;
        var we = game.AddComponent<Weather>();
        we.lightning = GameObject.Find("Lightning").GetComponent<Light>();
        we.thunder = game.AddComponent<AudioSource>(); we.thunder.volume = 0.9f;
        we.thunderClips = new[] { Clip("thunder0"), Clip("thunder1") };
        gm.player = pc; gm.ui = ui;
        gm.sfx = game.AddComponent<AudioSource>();
        gm.scream = Clip("scream"); gm.wrongSting = Clip("wrong_sting"); gm.rightTone = Clip("right_tone"); gm.stairSteps = Clip("stairs");
        rainSrc = Loop(game, "rain_loop", 0.5f); frogSrc = Loop(game, "frogs_loop", 0.22f); Loop(game, "drone_loop", 0.16f);
        var rz = new GameObject("Reverb").AddComponent<AudioReverbZone>(); rz.transform.position = new V(0, 1.5f, LEN / 2); rz.minDistance = 22; rz.maxDistance = 30; rz.reverbPreset = AudioReverbPreset.Hallway;

        // ground floor exit (landing B local space) + void blocker for normal floors
        var exit = GO("ExitGround", landB, V.zero);
        Quad("ExitBoard", exit.transform, new V(-0.73f, 2.72f, -LAND + 0.02f), new V(0, 180, 0), new Vector2(0.75f, 0.28f), mats["ExitGreen"]);
        Text("ExitText", exit.transform, new V(-0.73f, 2.72f, -LAND + 0.035f), new V(0, 180, 0), "EXIT", fSign, 1.8f, Color.white, new Vector2(0.7f, 0.26f));
        var el = GO("ExitLight", exit.transform, new V(-0.73f, -1.6f, -LAND - 2.6f)).AddComponent<Light>();
        el.type = LightType.Point; el.range = 6; el.intensity = 3f; el.color = new Color(0.7f, 0.8f, 1f);
        var exitRain = GO("ExitRain", exit.transform, V.zero).AddComponent<AudioSource>(); exitRain.clip = Clip("rain_loop"); exitRain.loop = true; exitRain.playOnAwake = true; exitRain.spatialBlend = 1; exitRain.minDistance = 2; exitRain.maxDistance = 12; exitRain.volume = 1f;
        exitRain.transform.localPosition = new V(-0.73f, -1.6f, -LAND - 2.6f);
        exit.SetActive(false);
        var blocker = GO("StairsDownNormal", landB, V.zero);
        Quad("Void", blocker.transform, new V(-0.73f, -1.1f, -LAND - 3.6f), V.zero, new Vector2(1.4f, 3.2f), mats["Black"]);

        w.building = building; w.fans = fans.ToArray(); w.tubes = tubes.ToArray(); w.doors = doors.ToArray(); w.chappals = chappals.ToArray();
        w.clockHour = clockH; w.clockMinute = clockM; w.notices = notices.ToArray(); w.floorSigns = signs.ToArray();
        w.rain = rainPS; w.rainAudio = rainSrc; w.frogAudio = frogSrc; w.ghost = ghost; w.ghostEyes = ghostEyes;
        w.moon = moon; w.moonDisc = moonDisc; w.floatables = floatables.ToArray(); w.chair = chair; w.wheelchair = wheelchair;
        w.exitDoorGround = exit; w.stairsDownB = blocker;
        return game;
    }
    static AudioSource Loop(GameObject g, string clip, float vol)
    {
        var a = g.AddComponent<AudioSource>(); a.clip = Clip(clip); a.loop = true; a.playOnAwake = true; a.volume = vol; a.spatialBlend = 0; return a;
    }

    // ------------------------------------------------------------------ anomalies
    static Anomaly A(Transform root, AnomalyKind k, string hint, int index = 0, string clip = null, bool src = false)
    {
        var g = GO(k.ToString() + "_" + index, root, V.zero);
        var a = g.AddComponent<Anomaly>(); a.kind = k; a.hint = hint; a.index = index; a.show = new GameObject[0]; a.hide = new GameObject[0];
        if (clip != null) a.clip = Clip(clip);
        if (src || clip != null) { var s = g.AddComponent<AudioSource>(); s.spatialBlend = 1; s.minDistance = 1.5f; s.maxDistance = 18; s.playOnAwake = false; a.src = s; }
        return a;
    }
    static void BuildAnomalies(GameObject game)
    {
        var r = GO("Anomalies", null, V.zero).transform;
        var list = new List<Anomaly>();
        list.Add(A(r, AnomalyKind.FanReverse, "A ceiling fan was spinning the wrong way.", 1));
        list.Add(A(r, AnomalyKind.FanWild, "A ceiling fan was spinning far too fast.", 2));
        list.Add(A(r, AnomalyKind.RedLights, "The tube lights were red."));
        list.Add(A(r, AnomalyKind.LightsDie, "The lights switched off ahead of you.", 0, "clunk"));
        var dn = A(r, AnomalyKind.DoorNumber, "Two rooms had the same number.", 4); list.Add(dn);
        var dop = A(r, AnomalyKind.DoorOpen, "A door opened by itself.", 5, "creak"); dop.show = new[] { roomEyes }; list.Add(dop);
        var cc = A(r, AnomalyKind.Toggle, "There were far too many chappals outside one room."); cc.show = new[] { chappalCrowd }; list.Add(cc);
        list.Add(A(r, AnomalyKind.ChappalsWatch, "The chappals turned to face you."));
        list.Add(A(r, AnomalyKind.Knocking, "Someone was knocking from inside a locked room.", 7, "knock"));
        var fe = A(r, AnomalyKind.FigureEnd, "She was standing at the end of the corridor.", 0, "whisper"); fe.pos = new V(0.35f, 0, LEN - 0.6f); fe.yaw = 180; list.Add(fe);
        var fc = A(r, AnomalyKind.FigureCourtyard, "Someone was standing outside the grille, in the rain."); fc.pos = new V(2.0f, -0.05f, 18.2f); fc.yaw = -90; list.Add(fc);
        list.Add(A(r, AnomalyKind.ClockBackwards, "The clock was running backwards."));
        var n0 = A(r, AnomalyKind.Notice, "The LOST notice had changed.", 0); n0.altText = "<b>LOST</b>\nMy way back.\n\nIf found, do NOT\nreturn to Room 706"; list.Add(n0);
        var n1 = A(r, AnomalyKind.Notice, "The mess menu had changed.", 1); n1.altText = "<b>MESS MENU — THU</b>\nB: Idli, Sambar\nL: Rice, Rasam, Poriyal\nD: <color=#7a0000>YOU</color>\n\n<i>No outside food!</i>"; list.Add(n1);
        var cb = A(r, AnomalyKind.Toggle, "The water cooler was dripping blood."); cb.show = new[] { coolerBlood }; list.Add(cb);
        var ch = A(r, AnomalyKind.ChairCreep, "The plastic chair moved whenever you looked away.", 0, "squeak"); ch.pos = new V(0, 0, 21f); ch.yaw = 180; list.Add(ch);
        list.Add(A(r, AnomalyKind.Tilt, "The whole corridor was tilted."));
        var hp = A(r, AnomalyKind.Handprints, "Bloody handprints appeared on the wall.", 0, "slap"); hp.show = new[] { handprints }; list.Add(hp);
        var wc = A(r, AnomalyKind.Wheelchair, "An empty wheelchair was rolling towards you.", 0, "squeak"); wc.pos = new V(0.1f, 0, 24f); wc.yaw = 180; list.Add(wc);
        list.Add(A(r, AnomalyKind.Footsteps, "Someone was walking right behind you.", 0, "step2"));
        var fr = A(r, AnomalyKind.FrozenRain, "The rain had frozen in mid-air."); fr.scareOnMiss = true; list.Add(fr);
        list.Add(A(r, AnomalyKind.Floating, "Things had started to float."));
        list.Add(A(r, AnomalyKind.Padlocks, "Every single door was padlocked."));
        list.Add(A(r, AnomalyKind.MoonRed, "The moon was red."));
        var sa = A(r, AnomalyKind.Toggle, "A white saree was drying on the line."); sa.show = new[] { sareeCloth }; sa.hide = new[] { towel2.gameObject }; list.Add(sa);
        game.GetComponent<GameManager>().anomalies = list.ToArray();
    }

    // ------------------------------------------------------------------ post processing + pipeline
    static void BuildPost()
    {
        var urp = (UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline;
        urp.shadowDistance = 38; urp.supportsHDR = true; urp.msaaSampleCount = 1; urp.maxAdditionalLightsCount = 8;
        var so = new SerializedObject(urp);
        so.FindProperty("m_SoftShadowsSupported").boolValue = true;
        so.FindProperty("m_MainLightShadowmapResolution").intValue = 2048;
        so.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = 8;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(urp);

        var gp = Profile("PP_Main", prof =>
        {
            var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var bl = prof.Add<Bloom>(true); bl.intensity.Override(0.9f); bl.threshold.Override(0.9f); bl.scatter.Override(0.72f);
            var vg = prof.Add<Vignette>(true); vg.intensity.Override(0.4f); vg.smoothness.Override(0.5f);
            var fg = prof.Add<FilmGrain>(true); fg.type.Override(FilmGrainLookup.Medium3); fg.intensity.Override(0.45f); fg.response.Override(0.7f);
            var ca = prof.Add<ColorAdjustments>(true); ca.postExposure.Override(0.25f); ca.contrast.Override(16f); ca.saturation.Override(-14f);
            var cab = prof.Add<ChromaticAberration>(true); cab.intensity.Override(0.12f);
            var smh = prof.Add<ShadowsMidtonesHighlights>(true); smh.shadows.Override(new Vector4(0.88f, 0.96f, 1.12f, 0f)); smh.highlights.Override(new Vector4(1.06f, 1.0f, 0.92f, 0f));
        });
        var sp = Profile("PP_Scare", prof =>
        {
            var ca = prof.Add<ColorAdjustments>(true); ca.colorFilter.Override(new Color(1f, 0.35f, 0.3f)); ca.saturation.Override(-60f); ca.contrast.Override(40f);
            var cab = prof.Add<ChromaticAberration>(true); cab.intensity.Override(1f);
            var ld = prof.Add<LensDistortion>(true); ld.intensity.Override(-0.45f);
            var vg = prof.Add<Vignette>(true); vg.intensity.Override(0.6f); vg.color.Override(new Color(0.3f, 0, 0));
        });
        var v = new GameObject("PostFX").AddComponent<Volume>(); v.isGlobal = true; v.sharedProfile = gp; v.priority = 0;
        var s = new GameObject("PostFX_Scare").AddComponent<Volume>(); s.isGlobal = true; s.sharedProfile = sp; s.priority = 1; s.weight = 0;
        Object.FindFirstObjectByType<GameManager>().scareVolume = s;
    }
    static VolumeProfile Profile(string name, System.Action<VolumeProfile> fill)
    {
        string path = "Assets/Game/Settings/" + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        var prof = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(prof, path);
        fill(prof);
        foreach (var c in prof.components) { c.name = c.GetType().Name; AssetDatabase.AddObjectToAsset(c, prof); }
        EditorUtility.SetDirty(prof);
        return prof;
    }
}
