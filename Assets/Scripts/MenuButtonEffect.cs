using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Animation Settings")]
    public float moveDistance = 30f;     
    public float scaleMultiplier = 1.1f; 
    public float animationSpeed = 15f;   

    [Header("Overlay/Border Settings")]
    [Tooltip("ลาก Object ที่เป็นขอบ หรือ กรอบเรืองแสง (ที่มี CanvasGroup) มาใส่ตรงนี้")]
    public CanvasGroup hoverOverlay; // 🟢 ตัวจัดการเฟดความใสของขอบ

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private Vector3 originalScale;
    private Vector3 targetScale;
    
    private float targetAlpha = 0f; // เป้าหมายความใสของกรอบ (0 = ซ่อน, 1 = โชว์)

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.localPosition;
        originalScale = rectTransform.localScale;
        
        targetPosition = originalPosition;
        targetScale = originalScale;

        // เริ่มเกมมาให้ซ่อนขอบไว้ก่อน
        if (hoverOverlay != null) hoverOverlay.alpha = 0f;
    }

    void Update()
    {
        // 1. ขยับปุ่มและขยายขนาด
        rectTransform.localPosition = Vector3.Lerp(rectTransform.localPosition, targetPosition, Time.unscaledDeltaTime * animationSpeed);
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);

        // 🟢 2. เฟดขอบ Overlay ให้ค่อยๆ โผล่หรือจางหายอย่างนุ่มนวล
        if (hoverOverlay != null)
        {
            hoverOverlay.alpha = Mathf.Lerp(hoverOverlay.alpha, targetAlpha, Time.unscaledDeltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) { DoHover(true); }
    public void OnPointerExit(PointerEventData eventData) { DoHover(false); }
    public void OnSelect(BaseEventData eventData) { DoHover(true); }
    public void OnDeselect(BaseEventData eventData) { DoHover(false); }

    private void DoHover(bool isEntering)
    {
        if (isEntering)
        {
            targetPosition = originalPosition + new Vector3(moveDistance, 0, 0);
            targetScale = originalScale * scaleMultiplier;
            targetAlpha = 1f; // 🟢 สั่งโชว์ขอบ (Fade In)
        }
        else
        {
            targetPosition = originalPosition;
            targetScale = originalScale;
            targetAlpha = 0f; // 🟢 สั่งซ่อนขอบ (Fade Out)
        }
    }

    void OnDisable()
    {
        // รีเซ็ตค่าทั้งหมดตอนโดนปิดหน้าต่าง กันบั๊กค้าง
        rectTransform.localPosition = originalPosition;
        rectTransform.localScale = originalScale;
        targetPosition = originalPosition;
        targetScale = originalScale;
        
        if (hoverOverlay != null) hoverOverlay.alpha = 0f;
        targetAlpha = 0f;
    }
}