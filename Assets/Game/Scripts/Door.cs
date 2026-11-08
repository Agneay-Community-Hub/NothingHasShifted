using UnityEngine;
public class Door : MonoBehaviour
{
    public Transform hinge;     // rotates around local Y
    public float openAngle;     // current target
    public float speed = 25f;
    public GameObject padlock;
    public TMPro.TMP_Text number;
    float cur, shakeT;
    Quaternion baseRot;
    void Awake() { if (hinge) baseRot = hinge.localRotation; }
    public void SetOpen(float a, bool instant = false) { openAngle = a; if (instant) { cur = a; ApplyRot(0); } }
    public void Shake(float t) { shakeT = t; }
    void ApplyRot(float jitter) { if (hinge) hinge.localRotation = baseRot * Quaternion.Euler(0, cur + jitter, 0); }
    void Update()
    {
        cur = Mathf.MoveTowards(cur, openAngle, speed * Time.deltaTime);
        float j = 0;
        if (shakeT > 0) { shakeT -= Time.deltaTime; j = Mathf.Sin(Time.time * 60f) * 1.2f * Mathf.Clamp01(shakeT * 2); }
        ApplyRot(j);
    }
}
