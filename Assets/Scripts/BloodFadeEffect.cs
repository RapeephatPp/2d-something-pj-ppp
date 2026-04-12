using UnityEngine;
using System.Collections;

public class BloodFadeEffect : MonoBehaviour
{
    public enum BloodBehavior 
    { 
        Decal,          // รอยเลือดแปะติดฉาก (อยู่นิ่งๆ)
        PhysicsBounce   // หยดเลือดกระเด็นเด้งพื้น (ใช้ฟิสิกส์)
    }

    [Header("Behavior Settings")]
    public BloodBehavior behavior = BloodBehavior.Decal; // 🟢 เลือกโหมดได้ใน Inspector!

    [Header("Visual Settings")]
    public Sprite[] bloodSprites; 

    [Header("Fade Settings")]
    public float fadeDelay = 2f; 
    public float fadeDuration = 3f; 

    [Header("Physics Bounce Settings (For PhysicsBounce Mode)")]
    public float minPopForce = 3f;      // แรงกระเด็นต่ำสุด
    public float maxPopForce = 7f;      // แรงกระเด็นสูงสุด
    public float spinForce = 150f;      // แรงหมุนติ้วๆ กลางอากาศ
    
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // สุ่มรูปภาพเลือด
        if (bloodSprites != null && bloodSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, bloodSprites.Length);
            sr.sprite = bloodSprites[randomIndex]; 
        }

        // 🟢 ตรวจสอบโหมดการทำงาน
        if (behavior == BloodBehavior.Decal)
        {
            // ถ้าเป็นรอยเลือดติดฉาก ให้สุ่มหมุนองศาแบบเดิม และอยู่นิ่งๆ
            transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
        }
        else if (behavior == BloodBehavior.PhysicsBounce)
        {
            // ถ้าเป็นโหมดกระเด้ง ให้ยิงแรงระเบิด (Impulse) ใส่
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // สุ่มทิศทางพุ่งขึ้นด้านบน (Y) และกระจายออกซ้ายขวา (X)
                Vector2 popDir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1.5f)).normalized;
                float popForce = Random.Range(minPopForce, maxPopForce);
                
                rb.AddForce(popDir * popForce, ForceMode2D.Impulse);
                rb.AddTorque(Random.Range(-spinForce, spinForce)); // ใส่แรงหมุนให้ดูเป็นธรรมชาติ
            }
            else 
            {
                Debug.LogWarning("ลืมใส่ Rigidbody2D ให้กับ Prefab เลือด หรือเปล่าวัยรุ่น!?");
            }
        }

        // เริ่มต้นจับเวลาจางหายเหมือนเดิม
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(fadeDelay);
        
        float currentTime = 0f;
        Color originalColor = sr.color;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, currentTime / fadeDuration);
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        Destroy(gameObject); 
    }
}