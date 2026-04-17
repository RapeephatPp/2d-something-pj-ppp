using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class TutorialWorldText : MonoBehaviour
{
    [Header("Tutorial Text Settings")]
    [TextArea(3, 5)]
    [Tooltip("พิมพ์ข้อความสอนเล่นลงไปได้เลย")]
    public string message = "ข้อความสอนเล่น...";
    
    [Header("World Space UI")]
    [Tooltip("ลากลูกที่เป็น TextMeshPro มาใส่")]
    public TextMeshPro textMesh; 
    
    [Tooltip("ลากภาพพื้นหลัง (ถ้ามี) มาใส่ ไม่ใส่ก็ได้")]
    public SpriteRenderer backgroundBubble; 
    
    [Header("Animation Settings")]
    public float fadeSpeed = 5f;       // ความเร็วตอนค่อยๆ สว่าง
    public float floatSpeed = 2f;      // ความเร็วตอนเด้งดึ๋ง
    public float floatAmount = 0.15f;  // ระยะความสูงที่เด้ง

    private Vector3 startPos;
    private float targetAlpha = 0f;    // เป้าหมายความใส (0 = มองไม่เห็น, 1 = ชัดเจน)

    void Start()
    {
        // บังคับให้เป็น Trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        if (textMesh != null) 
        {
            textMesh.text = message; // ดึงข้อความไปใส่ให้
            startPos = textMesh.transform.localPosition;
            
            // เริ่มเกมมาให้ล่องหนรอไว้เลย
            SetAlpha(0f);
        }
    }

    void Update()
    {
        if (textMesh == null) return;

        // 1. ทำแอนิเมชัน Fade-in / Fade-out อย่างนุ่มนวล
        float currentAlpha = textMesh.color.a;
        float newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
        SetAlpha(newAlpha);

        // 2. ทำให้ข้อความลอยเด้งดึ๋งๆ (ทำงานเฉพาะตอนที่มันเริ่มสว่าง จะได้ไม่กินสเปค)
        if (newAlpha > 0.01f) 
        {
            float newY = startPos.y + (Mathf.Sin(Time.time * floatSpeed) * floatAmount);
            textMesh.transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
        }
    }

    // ฟังก์ชันสำหรับปรับความสว่าง (Alpha) ทั้งตัวหนังสือและกรอบ
    private void SetAlpha(float alpha)
    {
        if (textMesh != null)
            textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, alpha);
            
        if (backgroundBubble != null)
            backgroundBubble.color = new Color(backgroundBubble.color.r, backgroundBubble.color.g, backgroundBubble.color.b, alpha);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            targetAlpha = 1f; // ผู้เล่นมาแล้ว สั่งให้สว่าง!
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            targetAlpha = 0f; // ผู้เล่นไปแล้ว สั่งให้จางหาย!
        }
    }
}