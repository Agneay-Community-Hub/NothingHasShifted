using UnityEngine;
public class Weather : MonoBehaviour
{
    public Light lightning;
    public AudioSource thunder;
    public AudioClip[] thunderClips;
    public float minGap = 18, maxGap = 45;
    public bool frozen;
    float next;
    void Start() { next = Time.time + Random.Range(8, 20); }
    void Update()
    {
        if (frozen) return;
        if (Time.time > next) { next = Time.time + Random.Range(minGap, maxGap); StartCoroutine(Strike()); }
    }
    public void StrikeNow() { StartCoroutine(Strike()); }
    System.Collections.IEnumerator Strike()
    {
        if (!lightning) yield break;
        int n = Random.Range(2, 4);
        for (int i = 0; i < n; i++)
        {
            lightning.intensity = Random.Range(1.5f, 3.5f);
            yield return new WaitForSeconds(Random.Range(0.04f, 0.09f));
            lightning.intensity = 0;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.18f));
        }
        yield return new WaitForSeconds(Random.Range(0.6f, 2.2f));
        if (thunder && thunderClips.Length > 0) thunder.PlayOneShot(thunderClips[Random.Range(0, thunderClips.Length)], Random.Range(0.6f, 1f));
    }
}
