using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public PlayerController player;
    public UIController ui;
    public Vector3 spawnPos = new Vector3(0, 0.05f, -2.2f);
    public float spawnYaw = 0f;
    public float backZ = -5.4f, forwardZ = 35.4f;
    public int startFloor = 8;
    [Range(0, 1)] public float anomalyChance = 0.62f;
    public Anomaly[] anomalies;
    public AudioSource sfx;
    public AudioClip scream, wrongSting, rightTone, stairSteps;
    public UnityEngine.Rendering.Volume scareVolume;

    int floor;
    Anomaly current;
    bool busy, paused;
    public bool Playing { get; private set; }
    readonly List<int> bag = new List<int>();
    int lastIndex = -1, mistakes, laps;
    float startTime;

    void Awake()
    {
        I = this;
        PlayerController.Sensitivity = PlayerPrefs.GetFloat("sens", 2f);
        AudioListener.volume = PlayerPrefs.GetFloat("vol", 0.9f);
    }

    void Start()
    {
        foreach (var a in anomalies) a.Deactivate();
        floor = startFloor;
        Prepare(true);
        player.locked = true;
        ui.ShowMenu(true);
        StartCoroutine(ui.FadeTo(0, 2f));
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    public void StartGame() { if (!Playing && !busy) StartCoroutine(Intro()); }

    IEnumerator Intro()
    {
        busy = true;
        ui.ShowMenu(false);
        LockCursor(true);
        yield return ui.FadeTo(1, 0.8f);
        floor = startFloor; mistakes = 0; laps = 0; startTime = Time.time;
        Prepare(true);
        Respawn();
        yield return ui.Story(new[]
        {
            "C-BLOCK GENTS HOSTEL\n<size=70%>2:47 AM · the monsoon has not stopped for three days</size>",
            "The power came back an hour ago.\nYou only stepped out to fill your water bottle.",
            "But the corridor outside your room\nkeeps\nrepeating.",
        });
        yield return ui.FadeTo(0, 1.6f);
        Playing = true; busy = false;
        player.locked = false;
        ui.Toast(World.FloorName(floor));
        ui.Hint("Read the warden's notice on the wall to your left.", 6f);
    }

    void LockCursor(bool v) { Cursor.lockState = v ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !v; }

    public void SetPaused(bool v)
    {
        if (!Playing) return;
        paused = v;
        Time.timeScale = v ? 0 : 1;
        AudioListener.pause = v;
        ui.ShowPause(v);
        LockCursor(!v);
        player.locked = v || busy;
    }

    public void Restart()
    {
        Time.timeScale = 1; AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Update()
    {
        if (!Playing) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            // On WebGL Esc also releases pointer lock; only toggle pause when not busy.
            if (!busy) SetPaused(!paused);
        }
        if (paused || busy) return;
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);
        float z = player.transform.position.z;
        if (z < backZ) StartCoroutine(Resolve(false));
        else if (z > forwardZ) StartCoroutine(Resolve(true));
    }

    void Respawn() { player.Teleport(spawnPos, spawnYaw); }

    void Prepare(bool forceNormal)
    {
        if (current) current.Deactivate();
        current = null;
        var W = World.I;
        bool ground = floor <= 0;
        if (W.exitDoorGround) W.exitDoorGround.SetActive(ground);
        if (W.stairsDownB) W.stairsDownB.SetActive(!ground);
        for (int i = 0; i < W.doors.Length; i++)
            if (W.doors[i].number) W.doors[i].number.text = (ground ? "G" : floor.ToString()) + "0" + (i + 1);
        foreach (var s in W.floorSigns) if (s) s.text = World.FloorName(floor);
        if (!forceNormal && !ground && anomalies.Length > 0 && Random.value < anomalyChance)
        {
            if (bag.Count == 0) { for (int i = 0; i < anomalies.Length; i++) if (i != lastIndex) bag.Add(i); }
            int k = Random.Range(0, bag.Count);
            lastIndex = bag[k]; bag.RemoveAt(k);
            current = anomalies[lastIndex];
            current.Activate();
        }
    }

    IEnumerator Resolve(bool forward)
    {
        busy = true;
        laps++;
        if (floor <= 0 && forward) { yield return Ending(); yield break; }
        bool had = current != null;
        bool correct = had ? !forward : forward;
        string msg = null;
        if (!correct)
        {
            mistakes++;
            if (had && forward && current.scareOnMiss) yield return Jumpscare();
            msg = had ? "SOMETHING HAD SHIFTED.\n<size=55%><color=#c9b48a>" + current.hint + "</color></size>"
                      : "NOTHING HAD SHIFTED.\n<size=55%><color=#c9b48a>You turned back for nothing.</color></size>";
        }
        player.locked = true;
        if (correct && stairSteps) sfx.PlayOneShot(stairSteps, 0.7f);
        yield return ui.FadeTo(1, correct ? 0.9f : 0.25f);
        if (!correct && wrongSting) sfx.PlayOneShot(wrongSting, 0.9f);
        floor = correct ? floor - 1 : startFloor;
        Prepare(floor == startFloor);
        Respawn();
        if (msg != null)
        {
            ui.Center(msg);
            yield return new WaitForSeconds(3.6f);
            ui.Center("");
        }
        else yield return new WaitForSeconds(0.7f);
        yield return ui.FadeTo(0, 1.1f);
        if (correct && rightTone) sfx.PlayOneShot(rightTone, 0.5f);
        ui.Toast(World.FloorName(floor));
        if (floor <= 0) ui.Hint("The main door is open. Walk out.", 6f);
        player.locked = false;
        busy = false;
    }

    IEnumerator Jumpscare()
    {
        player.locked = true;
        var W = World.I;
        var g = W.ghost;
        g.gameObject.SetActive(true);
        if (W.ghostEyes) W.ghostEyes.gameObject.SetActive(true);
        foreach (var t in W.tubes) t.Flicker(1.2f);
        if (scream) sfx.PlayOneShot(scream, 1f);
        var cam = player.cam.transform;
        Vector3 fwd = cam.forward; fwd.y = 0; fwd.Normalize();
        float e = 0;
        while (e < 0.85f)
        {
            e += Time.deltaTime;
            float d = Mathf.Lerp(2.6f, 0.58f, Mathf.SmoothStep(0, 1, e / 0.45f));
            g.position = cam.position + fwd * d - Vector3.up * 1.7f;
            g.rotation = Quaternion.LookRotation(-fwd);
            if (scareVolume) scareVolume.weight = Mathf.Clamp01(e * 4);
            cam.localPosition += Random.insideUnitSphere * 0.02f;
            yield return null;
        }
        if (scareVolume) scareVolume.weight = 0;
        g.gameObject.SetActive(false);
    }

    IEnumerator Ending()
    {
        player.locked = true;
        if (stairSteps) sfx.PlayOneShot(stairSteps, 0.5f);
        yield return ui.FadeTo(1, 2.2f);
        Playing = false;
        float t = Time.time - startTime;
        yield return ui.Story(new[]
        {
            "You step out into the rain.",
            "Under the gate, the old watchman looks up from his radio.\n<i>\"Paani mila, beta?\"</i>",
            "Behind you, on the 8th floor,\na tube light flickers once.",
        });
        LockCursor(false);
        ui.ShowCredits(
            "<size=140%>YOU MADE IT OUT</size>\n\n" +
            string.Format("Time  {0}:{1:00}     ·     Floors walked  {2}     ·     Mistakes  {3}\n\n", (int)(t / 60), (int)(t % 60), laps, mistakes) +
            "<size=70%><color=#c9b48a>NOTHING HAS SHIFTED — made for GitHub Game Off 2026 (theme: SHIFT)\n" +
            "Design, code & procedural audio: Chetan Kumar G\n" +
            "3D props & textures: Poly Haven (CC0)  ·  Fonts: Special Elite, Oswald (Google Fonts)</color></size>");
    }
}
