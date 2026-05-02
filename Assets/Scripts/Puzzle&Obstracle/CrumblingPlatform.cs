using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class CrumblingPlatform : MonoBehaviour
{
    [Header("Settings")]
    public float breakDelay = 0.5f; 
    public float respawnTime = 3f;  

    [Header("Juice / Game Feel")]
    public float shakeIntensity = 0.05f;
    public float shakeSpeed = 50f;
    public ParticleSystem breakParticles; 
    
    [Header("Audio SFX")]
    public AudioClip crumbleWarningSound; // 🟢 เสียงร้าว/กรอบแกรบ ตอนเพิ่งเหยียบ
    public AudioClip breakSound;          // 🟢 เสียงหินถล่ม/แตกกระจาย

    private bool isSteppedOn = false;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D coll;
    private Vector3 originalPosition;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        coll = GetComponent<BoxCollider2D>();
        originalPosition = transform.position;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isSteppedOn)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f) 
                {
                    StartCoroutine(BreakSequence());
                    break;
                }
            }
        }
    }

    IEnumerator BreakSequence()
    {
        isSteppedOn = true;

        // 🟢 เล่นเสียงร้าวเตือนผู้เล่น!
        if (AudioManager.Instance != null && crumbleWarningSound != null)
            AudioManager.Instance.PlaySFX(crumbleWarningSound, 0.6f);

        float timer = 0;
        while (timer < breakDelay)
        {
            timer += Time.deltaTime;
            float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
            float offsetY = Random.Range(-shakeIntensity, shakeIntensity) * 0.3f;
            transform.position = originalPosition + new Vector3(offsetX, offsetY, 0);
            yield return null; 
        }

        transform.position = originalPosition;
        BreakPlatform();
    }

    void BreakPlatform()
    {
        // 🟢 เล่นเสียงถล่ม (เรียกผ่าน AudioManager จะได้ไม่โดนตัดเสียงตอนพื้นโดน Disable)
        if (AudioManager.Instance != null && breakSound != null)
            AudioManager.Instance.PlaySFX(breakSound, 0.9f);

        if (breakParticles != null) 
        {
            Instantiate(breakParticles, transform.position, Quaternion.identity);
        }

        spriteRenderer.enabled = false;
        coll.enabled = false;

        if (respawnTime > 0)
        {
            StartCoroutine(RespawnSequence());
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(respawnTime);
        
        spriteRenderer.enabled = true;
        coll.enabled = true;
        isSteppedOn = false;
    }
}