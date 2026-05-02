using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class PlayerThoughtTrigger : MonoBehaviour
{
    [Header("Thought Settings")]
    [TextArea(2, 4)]
    [Tooltip("ข้อความที่อยากให้ตัวละครคิด")]
    public string thoughtText = "อืม... ทางนี้ดูมืดจังแฮะ?";
    
    [Tooltip("ระยะเวลาที่โชว์ข้อความ (วินาที)")]
    public float displayTime = 3f;
    
    [Header("Visuals")]
    [Tooltip("ลากลูกที่เป็น TextMeshPro มาใส่")]
    public TextMeshPro textMesh; 
    
    [Tooltip("ลากภาพพื้นหลังกรอบคำพูด (ถ้ามี)")]
    public SpriteRenderer backgroundBubble; 
    
    [Header("Game Feel Settings")]
    [Tooltip("ความสูงของข้อความจากจุดกึ่งกลางผู้เล่น")]
    public float floatOffset = 1.5f; 
    [Tooltip("ความสมูทตอนที่กรอบข้อความวิ่งตามผู้เล่น")]
    public float followSpeed = 10f;

    private bool hasTriggered = false;
    private Transform playerTransform;

    void Start()
    {
        // บังคับให้กรอบชนเป็น Trigger เสมอ
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // เริ่มเกมมาให้โปร่งใส 100% รอไว้เลย
        SetAlpha(0f);
    }

    void Update()
    {
        // 🟢 ทำให้กรอบคำพูดลอยตามหัวผู้เล่นแบบสมูทๆ ตลอดเวลาที่มันกำลังโชว์อยู่
        if (playerTransform != null && textMesh != null && textMesh.color.a > 0.01f)
        {
            Vector3 targetPos = playerTransform.position + new Vector3(0, floatOffset, 0);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // เช็คว่าชนผู้เล่น และยังไม่เคยทำงานมาก่อน
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true; // ล็อคไว้เลยว่าทำงานแล้ว
            playerTransform = collision.transform; // จำตัวผู้เล่นไว้เพื่อวิ่งตาม
            
            StartCoroutine(ShowThoughtRoutine());
        }
    }

    private IEnumerator ShowThoughtRoutine()
    {
        // 1. เซ็ตข้อความ
        if (textMesh != null) textMesh.text = thoughtText;

        // 2. วาร์ปกรอบคำพูดไปอยู่บนหัวผู้เล่นทันทีก่อนเริ่มเฟด (จะได้ไม่ลอยมาจากที่ไกลๆ)
        transform.position = playerTransform.position + new Vector3(0, floatOffset, 0);

        // 3. Fade In (ค่อยๆ สว่างขึ้นใน 0.5 วินาที)
        yield return StartCoroutine(FadeRoutine(1f, 0.5f));

        // 4. โชว์ข้อความค้างไว้ตามเวลาที่ตั้ง
        yield return new WaitForSeconds(displayTime);

        // 5. Fade Out (ค่อยๆ จางหายไปใน 0.5 วินาที)
        yield return StartCoroutine(FadeRoutine(0f, 0.5f));

        // 6. ทำลายทิ้งไปเลย คืน Memory ให้ระบบ เพราะมันโชว์แค่ครั้งเดียวอยู่แล้ว
        Destroy(gameObject);
    }

    // ฟังก์ชันช่วยสำหรับการทำ Fade แบบนุ่มนวล
    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = textMesh != null ? textMesh.color.a : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            SetAlpha(newAlpha);
            yield return null;
        }
        SetAlpha(targetAlpha);
    }

    // ปรับความสว่างทั้งตัวหนังสือและกรอบ
    private void SetAlpha(float alpha)
    {
        if (textMesh != null)
            textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, alpha);
            
        if (backgroundBubble != null)
            backgroundBubble.color = new Color(backgroundBubble.color.r, backgroundBubble.color.g, backgroundBubble.color.b, alpha);
    }
}