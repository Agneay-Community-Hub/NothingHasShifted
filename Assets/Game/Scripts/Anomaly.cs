using System.Collections.Generic;
using UnityEngine;

public enum AnomalyKind
{
    Toggle, FanReverse, FanWild, RedLights, LightsDie, DoorNumber, DoorOpen, ChappalsWatch, Knocking,
    FigureEnd, FigureCourtyard, ClockBackwards, Notice, ChairCreep, Tilt, Handprints, Wheelchair,
    Footsteps, FrozenRain, Floating, Padlocks, MoonRed
}

public class Anomaly : MonoBehaviour
{
    public AnomalyKind kind;
    [TextArea] public string hint = "Something was different.";
    public bool scareOnMiss = true;
    public int index;
    public string altText;
    public GameObject[] show;
    public GameObject[] hide;
    public AudioSource src;
    public AudioClip clip;
    public Vector3 pos;
    public float yaw;

    public bool Active { get; private set; }
    World W => World.I;
    Transform Player => GameManager.I.player.transform;
    Camera Cam => GameManager.I.player.cam;

    // saved state
    string savedText;
    readonly List<Quaternion> savedRots = new List<Quaternion>();
    readonly List<Vector3> savedPos = new List<Vector3>();
    readonly List<bool> savedBools = new List<bool>();
    Vector3 p0; Quaternion r0;
    bool fired; float timer; int stage;
    Color savedColor; Color savedDisc;

    public void Activate()
    {
        Active = true; fired = false; timer = 0; stage = 0;
        foreach (var g in show) if (g) g.SetActive(true);
        foreach (var g in hide) if (g) g.SetActive(false);
        switch (kind)
        {
            case AnomalyKind.FanReverse: W.fans[index].SnapSpeed(-Mathf.Abs(W.fans[index].baseSpeed)); break;
            case AnomalyKind.FanWild: W.fans[index].speed = W.fans[index].baseSpeed * 3.2f; W.fans[index].wobble = 5f; break;
            case AnomalyKind.RedLights: foreach (var t in W.tubes) t.SetColor(new Color(1f, 0.07f, 0.04f)); break;
            case AnomalyKind.DoorNumber: savedText = W.doors[index].number.text; W.doors[index].number.text = string.IsNullOrEmpty(altText) ? W.doors[index - 1].number.text : altText; break;
            case AnomalyKind.Notice: savedText = W.notices[index].text; W.notices[index].text = altText; break;
            case AnomalyKind.ChappalsWatch:
                savedRots.Clear(); foreach (var c in W.chappals) savedRots.Add(c.rotation); break;
            case AnomalyKind.FigureEnd:
            case AnomalyKind.FigureCourtyard:
                W.ghost.gameObject.SetActive(true); W.ghost.position = pos; W.ghost.rotation = Quaternion.Euler(0, yaw, 0);
                if (W.ghostEyes) W.ghostEyes.gameObject.SetActive(kind == AnomalyKind.FigureEnd);
                break;
            case AnomalyKind.ClockBackwards: r0 = W.clockMinute.localRotation; savedRots.Clear(); savedRots.Add(W.clockHour.localRotation); break;
            case AnomalyKind.ChairCreep:
            case AnomalyKind.Wheelchair:
                {
                    var t = kind == AnomalyKind.ChairCreep ? W.chair : W.wheelchair;
                    p0 = t.position; r0 = t.rotation; t.gameObject.SetActive(true);
                    t.position = pos; t.rotation = Quaternion.Euler(0, yaw, 0);
                    break;
                }
            case AnomalyKind.Tilt: W.building.localRotation = Quaternion.Euler(0, 0, 3.2f); break;
            case AnomalyKind.Handprints:
                foreach (var g in show) if (g) foreach (Transform c in g.transform) c.gameObject.SetActive(false);
                break;
            case AnomalyKind.Footsteps: GameManager.I.player.OnStep += EchoStep; break;
            case AnomalyKind.FrozenRain:
                if (W.rain) W.rain.Pause();
                if (W.rainAudio) W.rainAudio.mute = true; if (W.frogAudio) W.frogAudio.mute = true;
                var we = FindFirstObjectByType<Weather>(); if (we) we.frozen = true;
                break;
            case AnomalyKind.Floating:
                savedPos.Clear(); savedRots.Clear();
                foreach (var f in W.floatables) { savedPos.Add(f.position); savedRots.Add(f.rotation); }
                break;
            case AnomalyKind.Padlocks:
                savedBools.Clear();
                foreach (var d in W.doors) { savedBools.Add(d.padlock.activeSelf); d.padlock.SetActive(true); }
                break;
            case AnomalyKind.MoonRed:
                savedColor = W.moon.color; W.moon.color = new Color(1f, 0.15f, 0.1f);
                if (W.moonDisc) { savedDisc = W.moonDisc.material.GetColor("_BaseColor"); W.moonDisc.material.SetColor("_BaseColor", new Color(1f, 0.12f, 0.06f)); }
                break;
        }
    }

    public void Deactivate()
    {
        if (!Active) return;
        Active = false;
        foreach (var g in show) if (g) g.SetActive(false);
        foreach (var g in hide) if (g) g.SetActive(true);
        if (src) src.Stop();
        switch (kind)
        {
            case AnomalyKind.FanReverse: W.fans[index].SnapSpeed(W.fans[index].baseSpeed); break;
            case AnomalyKind.FanWild: W.fans[index].SnapSpeed(W.fans[index].baseSpeed); W.fans[index].wobble = 0; break;
            case AnomalyKind.RedLights: foreach (var t in W.tubes) t.ResetColor(); break;
            case AnomalyKind.LightsDie: foreach (var t in W.tubes) t.SetOn(true); break;
            case AnomalyKind.DoorNumber: W.doors[index].number.text = savedText; break;
            case AnomalyKind.Notice: W.notices[index].text = savedText; break;
            case AnomalyKind.DoorOpen: W.doors[index].SetOpen(0, true); break;
            case AnomalyKind.ChappalsWatch: for (int i = 0; i < W.chappals.Length; i++) W.chappals[i].rotation = savedRots[i]; break;
            case AnomalyKind.FigureEnd:
            case AnomalyKind.FigureCourtyard: W.ghost.gameObject.SetActive(false); break;
            case AnomalyKind.ClockBackwards: W.clockMinute.localRotation = r0; W.clockHour.localRotation = savedRots[0]; break;
            case AnomalyKind.ChairCreep: W.chair.position = p0; W.chair.rotation = r0; break;
            case AnomalyKind.Wheelchair: W.wheelchair.position = p0; W.wheelchair.rotation = r0; W.wheelchair.gameObject.SetActive(false); break;
            case AnomalyKind.Tilt: W.building.localRotation = Quaternion.identity; break;
            case AnomalyKind.Footsteps: GameManager.I.player.OnStep -= EchoStep; break;
            case AnomalyKind.FrozenRain:
                if (W.rain) W.rain.Play();
                if (W.rainAudio) W.rainAudio.mute = false; if (W.frogAudio) W.frogAudio.mute = false;
                var we = FindFirstObjectByType<Weather>(); if (we) we.frozen = false;
                break;
            case AnomalyKind.Floating:
                for (int i = 0; i < W.floatables.Length; i++) { W.floatables[i].position = savedPos[i]; W.floatables[i].rotation = savedRots[i]; }
                break;
            case AnomalyKind.Padlocks: for (int i = 0; i < W.doors.Length; i++) W.doors[i].padlock.SetActive(savedBools[i]); break;
            case AnomalyKind.MoonRed:
                W.moon.color = savedColor; if (W.moonDisc) W.moonDisc.material.SetColor("_BaseColor", savedDisc); break;
        }
    }

    bool PlayerLooksAt(Vector3 p, float deg)
    {
        var dir = p - Cam.transform.position;
        return Vector3.Angle(Cam.transform.forward, dir) < deg;
    }

    void PlayAt(Vector3 p, float vol = 1f)
    {
        if (!clip) return;
        if (src) { src.transform.position = p; src.PlayOneShot(clip, vol); }
        else AudioSource.PlayClipAtPoint(clip, p, vol);
    }

    void EchoStep() { StartCoroutine(EchoRoutine()); }
    System.Collections.IEnumerator EchoRoutine()
    {
        yield return new WaitForSeconds(0.32f);
        if (!Active) yield break;
        var p = Player.position - Player.forward * 3.2f;
        PlayAt(p + Vector3.up * 0.1f, 0.85f);
    }

    void Update()
    {
        if (!Active || GameManager.I == null) return;
        var pz = Player.position.z;
        timer += Time.deltaTime;
        switch (kind)
        {
            case AnomalyKind.LightsDie:
                foreach (var t in W.tubes)
                {
                    float tz = t.transform.position.z;
                    if (t.IsOn && tz > pz + 1.5f && tz < pz + 6.5f) { t.SetOn(false); PlayAt(t.transform.position, 0.8f); }
                }
                break;
            case AnomalyKind.DoorOpen:
                if (!fired && pz > W.doors[index].transform.position.z - 4.5f)
                {
                    fired = true; W.doors[index].speed = 22f; W.doors[index].SetOpen(-75f); PlayAt(W.doors[index].transform.position);
                }
                break;
            case AnomalyKind.ChappalsWatch:
                foreach (var c in W.chappals)
                {
                    var d = Player.position - c.position; d.y = 0;
                    if (d.sqrMagnitude > 0.01f) c.rotation = Quaternion.Slerp(c.rotation, Quaternion.LookRotation(-d), Time.deltaTime * 3f);
                }
                break;
            case AnomalyKind.Knocking:
                {
                    var dz = W.doors[index].transform.position.z;
                    if (stage == 0 && Mathf.Abs(pz - dz) < 2.6f) { stage = 1; timer = 0; PlayAt(W.doors[index].transform.position + Vector3.up); W.doors[index].Shake(1.4f); }
                    else if (stage == 1 && timer > 3.2f && Mathf.Abs(pz - dz) < 6f) { stage = 2; PlayAt(W.doors[index].transform.position + Vector3.up, 1f); W.doors[index].Shake(1.6f); }
                    break;
                }
            case AnomalyKind.FigureEnd:
                if (W.ghost.gameObject.activeSelf && Vector3.Distance(Player.position, W.ghost.position) < 10f)
                {
                    foreach (var t in W.tubes) t.Flicker(0.8f);
                    PlayAt(W.ghost.position, 0.6f);
                    W.ghost.gameObject.SetActive(false);
                }
                break;
            case AnomalyKind.FigureCourtyard:
                {
                    var d = Player.position - W.ghost.position; d.y = 0;
                    W.ghost.rotation = Quaternion.Slerp(W.ghost.rotation, Quaternion.LookRotation(d), Time.deltaTime * 1.5f);
                    break;
                }
            case AnomalyKind.ClockBackwards:
                W.clockMinute.localRotation *= Quaternion.Euler(0, 0, 220f * Time.deltaTime);
                W.clockHour.localRotation *= Quaternion.Euler(0, 0, 18f * Time.deltaTime);
                break;
            case AnomalyKind.ChairCreep:
            case AnomalyKind.Wheelchair:
                {
                    var t = kind == AnomalyKind.ChairCreep ? W.chair : W.wheelchair;
                    var to = Player.position - t.position; to.y = 0;
                    if (to.magnitude > 1.6f && !PlayerLooksAt(t.position + Vector3.up * 0.5f, 62f))
                    {
                        t.position += to.normalized * (kind == AnomalyKind.Wheelchair ? 1.1f : 1.6f) * Time.deltaTime;
                        t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(to), Time.deltaTime * 4f);
                        if (src && clip && !src.isPlaying) { src.transform.position = t.position; src.clip = clip; src.loop = true; src.Play(); }
                    }
                    else if (src && src.isPlaying) src.Stop();
                    if (src) src.transform.position = t.position;
                    break;
                }
            case AnomalyKind.Handprints:
                foreach (var g in show)
                {
                    if (!g) continue;
                    foreach (Transform c in g.transform)
                        if (!c.gameObject.activeSelf && pz > c.position.z - 3.5f) { c.gameObject.SetActive(true); PlayAt(c.position, 0.7f); break; }
                }
                break;
            case AnomalyKind.Floating:
                for (int i = 0; i < W.floatables.Length; i++)
                {
                    var f = W.floatables[i];
                    float h = Mathf.Min(timer * 0.25f, 0.9f) + Mathf.Sin(Time.time * 1.3f + i) * 0.05f;
                    f.position = savedPos[i] + Vector3.up * h;
                    f.rotation = savedRots[i] * Quaternion.Euler(Mathf.Sin(Time.time * 0.7f + i) * 18f, Time.time * 12f * (i % 2 == 0 ? 1 : -1), 0);
                }
                break;
        }
    }
}
