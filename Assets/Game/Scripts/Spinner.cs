using UnityEngine;
public class Spinner : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float speed = 360f;
    public float wobble = 0f;
    [HideInInspector] public float baseSpeed;
    float cur;
    Quaternion baseRot;
    void Awake() { baseSpeed = speed; cur = speed; baseRot = transform.localRotation; }
    float angle;
    void Update()
    {
        cur = Mathf.Lerp(cur, speed, Time.deltaTime * 0.8f);
        angle += cur * Time.deltaTime;
        var w = wobble > 0 ? Quaternion.Euler(Mathf.Sin(Time.time * 7f) * wobble, 0, Mathf.Cos(Time.time * 6f) * wobble) : Quaternion.identity;
        transform.localRotation = baseRot * w * Quaternion.AngleAxis(angle, axis);
    }
    public void SnapSpeed(float s) { speed = s; cur = s; }
}
