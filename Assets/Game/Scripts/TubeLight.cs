using UnityEngine;
public class TubeLight : MonoBehaviour
{
    public Light lamp;
    public Renderer tube;
    public Color color = new Color(0.86f, 0.95f, 1f);
    public float intensity = 2.2f;
    public bool flickery;
    public AudioSource hum;
    bool on = true;
    float flickerUntil, nextFlicker;
    MaterialPropertyBlock mpb;
    Color curColor; float curMul = 1;
    static readonly int EmId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseId = Shader.PropertyToID("_BaseColor");

    void Awake() { mpb = new MaterialPropertyBlock(); curColor = color; Apply(1); nextFlicker = Time.time + Random.Range(4, 20); }

    public void SetColor(Color c) { curColor = c; Apply(curMul); }
    public void ResetColor() { curColor = color; Apply(curMul); }
    public void SetOn(bool v) { on = v; Apply(v ? 1 : 0); if (hum) hum.mute = !v; }
    public bool IsOn => on;
    public void Flicker(float seconds) { flickerUntil = Time.time + seconds; }

    void Apply(float m)
    {
        curMul = m;
        if (lamp) { lamp.color = curColor; lamp.intensity = intensity * m; lamp.enabled = m > 0.01f; }
        if (tube)
        {
            tube.GetPropertyBlock(mpb);
            mpb.SetColor(EmId, curColor * (2.5f * m));
            mpb.SetColor(BaseId, Color.Lerp(new Color(0.35f, 0.35f, 0.35f), Color.white, m));
            tube.SetPropertyBlock(mpb);
        }
    }

    void Update()
    {
        if (!on) return;
        if (flickery && Time.time > nextFlicker) { Flicker(Random.Range(0.3f, 1.4f)); nextFlicker = Time.time + Random.Range(6, 25); }
        if (Time.time < flickerUntil) Apply(Random.value < 0.45f ? Random.Range(0f, 0.25f) : 1f);
        else if (curMul != 1) Apply(1);
    }
}
