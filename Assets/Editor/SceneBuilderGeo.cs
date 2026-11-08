using UnityEngine;
using TMPro;
using V = UnityEngine.Vector3;

public static partial class SceneBuilder
{
    static void WallRun(Transform p, string n, float x0, float x1, float z0, float z1, bool inwardPositiveX)
    {
        // full-height wall slab between z0..z1 + dado/skirting on its corridor face
        Box(n, p, new V(x0, 0, z0), new V(x1, CH, z1), "WallUpper", 1.4f);
        float fx = inwardPositiveX ? x1 : x0, s = inwardPositiveX ? 1 : -1;
        Box(n + "_Dado", p, new V(Mathf.Min(fx, fx + s * 0.012f), 0, z0), new V(Mathf.Max(fx, fx + s * 0.012f), 1.2f, z1), "Dado", 1.2f, false);
        Box(n + "_Line", p, new V(Mathf.Min(fx, fx + s * 0.016f), 1.18f, z0), new V(Mathf.Max(fx, fx + s * 0.016f), 1.23f, z1), "DadoLine", 1f, false);
        Box(n + "_Skirt", p, new V(Mathf.Min(fx, fx + s * 0.02f), 0, z0), new V(Mathf.Max(fx, fx + s * 0.02f), 0.11f, z1), "Skirting", 1f, false);
    }

    static void BuildCorridor()
    {
        var b = GO("Corridor", building, V.zero).transform;
        Box("Floor", b, new V(-1.6f, -0.3f, 0), new V(1.62f, 0, LEN), "Terrazzo", 1.6f);
        Box("Ceiling", b, new V(-1.6f, CH, 0), new V(2.4f, CH + 0.25f, LEN), "Ceiling", 1.6f);
        Box("Chajja", b, new V(1.65f, CH - 0.1f, 0), new V(2.4f, CH, LEN), "Ochre", 1.2f, false);
        Box("Beam", b, new V(1.2f, 2.8f, 0), new V(1.65f, CH, LEN), "Ochre", 1.2f);
        // left wall with door openings
        float z = 0;
        foreach (var dz in DoorZ)
        {
            WallRun(b, "WallL", -1.6f, -1.4f, z, dz - 0.57f, true);
            Box("Lintel", b, new V(-1.6f, 2.22f, dz - 0.57f), new V(-1.4f, CH, dz + 0.57f), "WallUpper", 1.4f);
            z = dz + 0.57f;
        }
        WallRun(b, "WallL", -1.6f, -1.4f, z, LEN, true);
        // balcony side: pillars, parapet, coping, grilles
        for (int i = 0; i <= 8; i++)
        {
            float zp = i * 3.75f;
            Box("Pillar", b, new V(1.2f, 0, zp - 0.2f), new V(1.65f, 2.8f, zp + 0.2f), "Ochre", 1.2f);
            Box("PillarSkirt", b, new V(1.18f, 0, zp - 0.22f), new V(1.2f, 0.11f, zp + 0.22f), "Skirting", 1f, false);
        }
        Box("Parapet", b, new V(1.4f, 0, 0), new V(1.6f, 1.0f, LEN), "Ochre", 1.2f);
        Box("ParapetDado", b, new V(1.388f, 0, 0), new V(1.4f, 1.0f, LEN), "Dado", 1.2f, false);
        Box("Coping", b, new V(1.34f, 1.0f, 0), new V(1.68f, 1.06f, LEN), "Granite", 1.0f);
        var gr = GO("Grilles", b, V.zero).transform;
        for (int i = 0; i < 8; i++)
        {
            float zp = i * 3.75f + 0.2f;
            for (int k = 0; k < 2; k++)
            {
                var g = Art("grille_panel", gr, new V(1.5f, 1.06f, zp + 0.8375f + k * 1.675f), new V(0, 90, 0));
                g.transform.localScale = new V(0.965f, 1, 1);
                foreach (var r in g.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mats["GrilleMetal"];
            }
        }
        var block = GO("GrilleCollider", b, new V(1.5f, 1.9f, LEN / 2)); block.AddComponent<BoxCollider>().size = new V(0.12f, 1.8f, LEN);
        // backing behind closed doors
        foreach (var dz in DoorZ) if (System.Array.IndexOf(DoorZ, dz) != 5) Box("DoorBack", b, new V(-1.8f, 0, dz - 0.6f), new V(-1.62f, 2.25f, dz + 0.6f), "Black", 1f, false);
    }

    static GameObject BuildLanding(string name, V pos, float yaw, bool isA)
    {
        var root = GO(name, building, pos, new V(0, yaw, 0)).transform;
        float L = LAND, sd = 3.92f;
        Box("Floor", root, new V(-1.6f, -0.3f, -L), new V(1.6f, 0, 0), "Terrazzo", 1.6f);
        Box("Ceiling", root, new V(-1.6f, CH, -L - sd - 0.3f), new V(1.6f, CH + 0.25f, 0), "Ceiling", 1.6f);
        WallRun(root, "WallL", -1.6f, -1.4f, -L, 0, true);
        WallRun(root, "WallR", 1.4f, 1.6f, -L, 0, false);
        Box("WallL_Stair", root, new V(-1.6f, -2.8f, -L - sd - 0.3f), new V(-1.4f, CH, -L), "WallUpper", 1.4f);
        Box("WallR_Stair", root, new V(1.4f, -2.8f, -L - sd - 0.3f), new V(1.6f, CH, -L), "WallUpper", 1.4f);
        Box("Divider", root, new V(-0.06f, -2.8f, -L - sd), new V(0.06f, CH, -L), "WallUpper", 1.4f);
        Box("EndWall", root, new V(-1.6f, -2.8f, -L - sd - 0.3f), new V(1.6f, CH, -L - sd), "WallUpper", 1.4f);
        Box("StairPit", root, new V(-1.4f, -3.0f, -L - sd), new V(-0.06f, -2.6f, -L), "Granite", 1f);
        var st = GO("Stairs", root, V.zero).transform;
        for (int i = 0; i < 14; i++)
        {
            float z0 = -L - i * 0.28f, z1 = z0 - 0.28f;
            Box("Down" + i, st, new V(-1.4f, -2.6f, z1), new V(-0.06f, -(i + 1) * 0.17f + 0.0f, z0 + (i == 0 ? 0 : 0.03f)), "Granite", 1f);
            Box("Up" + i, st, new V(0.06f, 0, z1), new V(1.4f, (i + 1) * 0.17f, z0), "Granite", 1f);
        }
        // handrail along divider (down flight)
        float len = Mathf.Sqrt(sd * sd + 2.38f * 2.38f), ang = Mathf.Atan2(2.38f, sd) * Mathf.Rad2Deg;
        var rail = Box("Rail", root, new V(-0.03f, -0.03f, -len / 2), new V(0.03f, 0.03f, len / 2), "Iron", 1f, false);
        rail.transform.localPosition = new V(-0.14f, 0.92f - 1.19f, -L - sd / 2); rail.transform.localEulerAngles = new V(-ang, 0, 0);
        // collapsible gate across the up-flight (terrace)
        var gate = GO("TerraceGate", root, new V(0.73f, 0, -L - 0.05f)).transform;
        for (int i = 0; i < 12; i++)
        {
            float x = -0.65f + i * 0.118f;
            Box("Bar", gate, new V(x - 0.008f, 0.05f, -0.012f), new V(x + 0.008f, 2.4f, 0.012f), "Iron", 1f, false);
        }
        for (int i = 0; i < 11; i++)
            for (int k = 0; k < 5; k++)
            {
                float x = -0.65f + (i + 0.5f) * 0.118f, y = 0.3f + k * 0.45f;
                var d = Box("X", gate, new V(-0.006f, -0.2f, -0.006f), new V(0.006f, 0.2f, 0.006f), "Iron", 1f, false);
                d.transform.localPosition = new V(x, y, 0); d.transform.localEulerAngles = new V(0, 0, (k + i) % 2 == 0 ? 17 : -17);
            }
        Box("GateTop", gate, new V(-0.68f, 2.38f, -0.03f), new V(0.68f, 2.45f, 0.03f), "Iron", 1f, false);
        gate.gameObject.AddComponent<BoxCollider>().size = new V(1.4f, 5f, 0.1f);
        gate.GetComponent<BoxCollider>().center = new V(0, 2.5f, 0);
        var pl = Art("padlock", gate, new V(0.05f, 1.15f, 0.03f), V.zero, 1.4f);
        var tb = Quad("TerraceBoard", root, new V(0.73f, 2.75f, -L + 0.02f), new V(0, 180, 0), new Vector2(1.1f, 0.34f), mats["Board"]);
        var tt = Text("TerraceText", root, new V(0.73f, 2.75f, -L + 0.035f), new V(0, 180, 0), "TERRACE — NO ENTRY", fSign, 1.5f, new Color(0.9f, 0.88f, 0.8f), new Vector2(1.05f, 0.3f));
        tt.textWrappingMode = TextWrappingModes.NoWrap; tt.enableAutoSizing = true; tt.fontSizeMin = 0.5f; tt.fontSizeMax = 1.5f;
        var sl = GO("StairLight", root, new V(-0.7f, -0.6f, -L - 1.6f)).AddComponent<Light>();
        sl.type = LightType.Point; sl.range = 4.5f; sl.intensity = 2.2f; sl.color = new Color(1f, 0.82f, 0.6f); sl.shadows = LightShadows.None;
        // floor sign on the right wall
        Quad("FloorBoard", root, new V(1.385f, 2.15f, -2.2f), new V(0, 90, 0), new Vector2(1.9f, 0.85f), mats["Board"]);
        var fs = Text("FloorSign", root, new V(1.37f, 2.08f, -2.2f), new V(0, 90, 0), "8th FLOOR", fSign, 5f, new Color(0.93f, 0.9f, 0.82f), new Vector2(1.8f, 0.6f));
        Text("Block", root, new V(1.37f, 2.46f, -2.2f), new V(0, 90, 0), "C - BLOCK  ·  GENTS HOSTEL", fSign, 1.3f, new Color(0.86f, 0.62f, 0.2f), new Vector2(1.8f, 0.2f));
        fs.textWrappingMode = TextWrappingModes.NoWrap; fs.enableAutoSizing = true; fs.fontSizeMin = 1f; fs.fontSizeMax = 5f;
        signs.Add(fs);
        if (isA)
        {
            // warden's rules notice
            Quad("RulesPaper", root, new V(-1.385f, 1.55f, -2.0f), new V(0, -90, 0), new Vector2(0.95f, 1.35f), mats["Paper"]);
            Text("RulesText", root, new V(-1.375f, 1.55f, -2.0f), new V(0, -90, 0),
                "<b>NOTICE</b>\n<size=70%>BY ORDER OF THE WARDEN</size>\n\n" +
                "<align=left><size=80%>1. Do not ignore anything unusual in the corridor.\n\n" +
                "2. If <b>anything has shifted</b>, turn back at once and take the stairs <b>behind you</b>.\n\n" +
                "3. If <b>nothing has shifted</b>, walk on and take the stairs at the <b>far end</b>.\n\n" +
                "4. Reach the <b>GROUND FLOOR</b> to leave.\n\n" +
                "<i>Lights out 10:30 PM. Maintain silence.</i></size></align>",
                fType, 0.5f, new Color(0.1f, 0.08f, 0.07f), new Vector2(0.82f, 1.2f));
        }
        return root.gameObject;
    }

    static void BuildOutside()
    {
        var o = GO("Outside", building, V.zero).transform;
        Box("Courtyard", o, new V(1.6f, -12.4f, -30), new V(45, -12f, 60), "Ground", 3f);
        Box("OuterWall", o, new V(1.4f, -12f, -LAND - 4.4f), new V(1.62f, -0.3f, LEN + LAND + 4.4f), "OuterWall", 2f);
        for (int k = 1; k <= 3; k++)
        {
            float y = -3.5f * k;
            Box("LowerParapet", o, new V(1.62f, y, -LAND - 4.4f), new V(1.85f, y + 1.06f, LEN + LAND + 4.4f), "Ochre", 1.2f, false);
            Box("LowerChajja", o, new V(1.62f, y + 3.3f, -LAND - 4.4f), new V(2.4f, y + 3.45f, LEN + LAND + 4.4f), "Ochre", 1.2f, false);
        }
        Box("StairTower", o, new V(1.6f, -12f, -LAND - 4.4f), new V(3.5f, 4.5f, -0.1f), "OuterWall", 2f, false);
        Box("StairTowerB", o, new V(1.6f, -12f, LEN + 0.1f), new V(3.5f, 4.5f, LEN + LAND + 4.4f), "OuterWall", 2f, false);
        Quad("OppositeBlock", o, new V(30f, 6f, 15f), new V(0, 90, 0), new Vector2(72, 36), mats["Facade"]);
        Box("CompoundWall", o, new V(16f, -12f, -30), new V(16.3f, -9.6f, 60), "OuterWall", 2f, false);
        Box("Roof", o, new V(-1.6f, CH + 0.25f, -LAND - 4.4f), new V(2.4f, CH + 0.5f, LEN + LAND + 4.4f), "Ochre", 2f, false);
        PHM("island_tree_01", o, new V(7.5f, -12f, 3f), new V(0, 30, 0), 1.7f);
        PHM("island_tree_01", o, new V(10f, -12f, 21f), new V(0, 140, 0), 2.0f);
        PHM("island_tree_01", o, new V(6f, -12f, 30f), new V(0, 250, 0), 1.6f);
        PHM("island_tree_01", o, new V(14f, -12f, 7f), new V(0, 80, 0), 2.2f);
        foreach (var lp in new[] { new Vector2(8.5f, 11.5f), new Vector2(8.5f, 26f), new Vector2(15.5f, 16.5f) })
        {
            float lz = lp.y, lx = lp.x;
            var l = PHM("street_lamp_01", o, new V(lx - 0.4f, -12f, lz), new V(0, 90, 0), 1.0f);
            var pl = GO("SodiumLight", o, new V(lx, -8.4f, lz)).AddComponent<Light>();
            pl.type = LightType.Point; pl.range = 20; pl.intensity = 28f; pl.color = new Color(1f, 0.58f, 0.22f); pl.shadows = LightShadows.None;
            var gl = Quad("Glow", o, new V(lx - 0.3f, -8.3f, lz), new V(0, 90, 0), new Vector2(2.2f, 2.2f), mats["Glow"]);
        }
    }

    static void BuildRoom(int di)
    {
        float dz = DoorZ[di];
        var r = GO("Room" + di, building, V.zero).transform;
        Box("RFloor", r, new V(-5.4f, -0.3f, dz - 1.85f), new V(-1.6f, 0, dz + 1.85f), "Terrazzo", 1.6f);
        Box("RCeil", r, new V(-5.4f, CH, dz - 1.85f), new V(-1.6f, CH + 0.2f, dz + 1.85f), "Ceiling", 1.6f, false);
        Box("RBack", r, new V(-5.6f, 0, dz - 1.85f), new V(-5.4f, CH, dz + 1.85f), "WallUpper", 1.4f, false);
        Box("RSide1", r, new V(-5.4f, 0, dz - 2.05f), new V(-1.6f, CH, dz - 1.85f), "WallUpper", 1.4f, false);
        Box("RSide2", r, new V(-5.4f, 0, dz + 1.85f), new V(-1.6f, CH, dz + 2.05f), "WallUpper", 1.4f, false);
        PHM("old_bed_frame", r, new V(-4.6f, 0, dz + 0.9f), new V(0, 0, 0), 1f);
        PHM("vintage_suitcase", r, new V(-2.3f, 0, dz + 1.6f), new V(0, 0, 0), 1f);
        PHM("metal_stool_01", r, new V(-3.0f, 0, dz - 1.3f), new V(0, 20, 0), 1f);
    }
}
