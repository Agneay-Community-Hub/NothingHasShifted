using UnityEngine;
using TMPro;
// Registry of everything anomalies may touch. Filled by the scene builder.
public class World : MonoBehaviour
{
    public static World I;
    public Transform building;          // root rotated by the Tilt anomaly
    public Spinner[] fans;
    public TubeLight[] tubes;
    public Door[] doors;
    public Transform[] chappals;        // the normal chappal pairs outside doors
    public Transform clockHour, clockMinute;
    public TMP_Text[] notices;          // notice board texts
    public TMP_Text[] floorSigns;
    public ParticleSystem rain;
    public AudioSource rainAudio, frogAudio;
    public Transform ghost;             // reusable ghost figure
    public Transform ghostEyes;
    public Light moon;
    public Renderer moonDisc;
    public Transform[] floatables;      // bucket, mug, bat...
    public Transform chair;
    public Transform wheelchair;
    public GameObject exitDoorGround, stairsDownB, exitTrigger;
    public AudioSource oneShot3D;
    void Awake() { I = this; }

    public static string FloorName(int f)
    {
        if (f <= 0) return "GROUND FLOOR";
        string suf = (f % 10 == 1 && f != 11) ? "st" : (f % 10 == 2 && f != 12) ? "nd" : (f % 10 == 3 && f != 13) ? "rd" : "th";
        return f + suf + " FLOOR";
    }
}
