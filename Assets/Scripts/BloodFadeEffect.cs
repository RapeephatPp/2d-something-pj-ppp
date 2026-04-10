using UnityEngine;
using System.Collections;

public class BloodFadeEffect : MonoBehaviour
{
    [Header("Visual Settings")]
    public Sprite[] bloodSprites; 

    [Header("Fade Settings")]
    public float fadeDelay = 2f; 
    public float fadeDuration = 3f; 
    
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // สุ่มรูปภาพเลือด
        if (bloodSprites != null && bloodSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, bloodSprites.Length);
            sr.sprite = bloodSprites[randomIndex]; 
        }

        // สุ่มองศาการหมุน (ยังเก็บไว้เพื่อให้เลือดไม่หันไปทางเดียวกันหมด)
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
        
        // 🟢 เอาบรรทัดสุ่ม Scale ออกไปแล้ว! ขนาดจะคงที่ตาม Prefab ต้นฉบับเลย

        // เริ่มต้นจับเวลาจางหาย
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