using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Camera cam;
    public float walkSpeed = 2.1f, runSpeed = 3.6f;
    public static float Sensitivity = 2.0f;
    public AudioSource feet;
    public AudioClip[] steps;
    public bool locked = true;
    public System.Action OnStep;

    CharacterController cc;
    float pitch, yaw, vy, stepDist, bobT;
    Vector3 camBase;
    public float Speed01 { get; private set; }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        camBase = cam.transform.localPosition;
        yaw = transform.eulerAngles.y;
    }

    public void Teleport(Vector3 pos, float yawDeg)
    {
        cc.enabled = false;
        transform.position = pos;
        yaw = yawDeg; pitch = 0;
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        cam.transform.localRotation = Quaternion.identity;
        cc.enabled = true;
        vy = 0;
    }

    void Update()
    {
        if (locked) { Speed01 = 0; return; }
        float mx = Input.GetAxis("Mouse X"), my = Input.GetAxis("Mouse Y");
        yaw += mx * Sensitivity;
        pitch = Mathf.Clamp(pitch - my * Sensitivity, -80f, 80f);
        transform.rotation = Quaternion.Euler(0, yaw, 0);

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1) input.Normalize();
        bool running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float speed = running ? runSpeed : walkSpeed;
        Vector3 move = transform.TransformDirection(input) * speed;
        vy = cc.isGrounded ? -1f : vy - 9.81f * Time.deltaTime;
        move.y = vy;
        Vector3 before = transform.position;
        cc.Move(move * Time.deltaTime);
        Vector3 delta = transform.position - before; delta.y = 0;
        float d = delta.magnitude;
        Speed01 = Mathf.Clamp01(d / Mathf.Max(0.0001f, Time.deltaTime) / runSpeed);

        // head bob + footsteps
        if (d > 0.0005f)
        {
            bobT += d * (running ? 2.6f : 3.1f);
            stepDist += d;
            float stride = running ? 0.95f : 0.72f;
            if (stepDist > stride)
            {
                stepDist = 0;
                if (feet && steps != null && steps.Length > 0)
                {
                    feet.pitch = Random.Range(0.9f, 1.08f);
                    feet.PlayOneShot(steps[Random.Range(0, steps.Length)], running ? 0.9f : 0.6f);
                }
                OnStep?.Invoke();
            }
        }
        float bob = Mathf.Sin(bobT * Mathf.PI) * 0.035f * Mathf.Clamp01(Speed01 * 3);
        float sway = Mathf.Cos(bobT * Mathf.PI * 0.5f) * 0.02f * Mathf.Clamp01(Speed01 * 3);
        cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, camBase + new Vector3(sway, bob, 0), 12 * Time.deltaTime);
        cam.transform.localRotation = Quaternion.Euler(pitch, 0, -sway * 20f);
    }
}
