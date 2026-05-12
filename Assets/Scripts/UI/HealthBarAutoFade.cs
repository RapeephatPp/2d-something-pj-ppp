using UnityEngine;
using UnityEngine.EventSystems; // 🟢 ขาดไม่ได้! ต้องใช้เพื่อตรวจจับเมาส์ชี้

[RequireComponent(typeof(CanvasGroup))] // บังคับว่าต้องมี CanvasGroup เพื่อปรับความสว่าง
public class HealthBarAutoFade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Fade Settings")]
    public float showDuration = 3f; // 🟢 โชว์ค้างไว้กี่วิหลังจากโดนตี
    public float fadeSpeed = 5f;    // 🟢 ความเร็วตอนจางหาย/สว่างขึ้น

    private CanvasGroup cg;
    private float showTimer = 0f;
    private bool isHovered = false;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        // เริ่มเกมมา สั่งให้โชว์อวดผู้เล่นก่อนแป๊บนึง
        TriggerShow();
    }

    void Update()
    {
        // นับเวลาถอยหลัง
        if (showTimer > 0)
        {
            showTimer -= Time.deltaTime;
        }

        // 🟢 เงื่อนไขโชว์หลอด: เมาส์ชี้อยู่ หรือ เวลายังไม่หมด
        float targetAlpha = (isHovered || showTimer > 0f) ? 1f : 0f;
        
        // 🟢 ค่อยๆ เฟดความสว่างให้สมูท
        cg.alpha = Mathf.Lerp(cg.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }

    // 🟢 ฟังก์ชันนี้ UIManager จะเป็นคนเรียกใช้ตอนเลือดลด
    public void TriggerShow()
    {
        showTimer = showDuration;
    }

    // 🟢 ทำงานอัตโนมัติเมื่อ "เอาเมาส์มาวางทับ"
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    // 🟢 ทำงานอัตโนมัติเมื่อ "เอาเมาส์ออก"
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}