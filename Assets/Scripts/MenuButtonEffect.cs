using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // จำเป็นต้องมีเพื่อดักจับเมาส์

public class MenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Animation Settings")]
    public float moveDistance = 30f;     // ระยะที่ปุ่มจะยื่นออกมาทางขวา
    public float scaleMultiplier = 1.1f; // ขนาดที่จะขยายขึ้น (1.1 = 110%)
    public float animationSpeed = 10f;   // ความเร็วในการเปลี่ยนค่า

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private Vector3 originalScale;
    private Vector3 targetScale;

    private bool isHovered = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.localPosition;
        originalScale = rectTransform.localScale;
        
        targetPosition = originalPosition;
        targetScale = originalScale;
    }

    void Update()
    {
        // 🟢 ทำให้ปุ่มขยับและขยายอย่างนุ่มนวลตลอดเวลา
        rectTransform.localPosition = Vector3.Lerp(rectTransform.localPosition, targetPosition, Time.unscaledDeltaTime * animationSpeed);
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
    }

    // 🟢 เมื่อเมาส์วางบนปุ่ม
    public void OnPointerEnter(PointerEventData eventData) { DoHover(true); }
    // 🟢 เมื่อเมาส์ออกจากปุ่ม
    public void OnPointerExit(PointerEventData eventData) { DoHover(false); }
    // 🟢 สำหรับการใช้คีย์บอร์ดหรือจอยเลื่อนมาที่ปุ่ม
    public void OnSelect(BaseEventData eventData) { DoHover(true); }
    public void OnDeselect(BaseEventData eventData) { DoHover(false); }

    private void DoHover(bool isEntering)
    {
        if (isEntering)
        {
            // ตั้งเป้าหมายให้ยื่นออกไปทางขวา และขยายใหญ่
            targetPosition = originalPosition + new Vector3(moveDistance, 0, 0);
            targetScale = originalScale * scaleMultiplier;
            
            // (ใส่เสียง Sound Effect ตอน Hover ตรงนี้ได้เลยครับ)
        }
        else
        {
            // กลับคืนค่าเดิม
            targetPosition = originalPosition;
            targetScale = originalScale;
        }
    }

    // กันเหนียว: ถ้าปุ่มโดนปิดการใช้งาน ให้มันกลับไปค่าเดิมทันที
    void OnDisable()
    {
        rectTransform.localPosition = originalPosition;
        rectTransform.localScale = originalScale;
        targetPosition = originalPosition;
        targetScale = originalScale;
    }
}