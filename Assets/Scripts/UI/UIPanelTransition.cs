using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class UIPanelTransition : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("ระยะที่หน้าต่างจะซ่อนตัว เช่น X: 0, Y: -800 (มุดลงล่าง)")]
    public Vector2 hideOffset = new Vector2(0, -800f); 
    public float slideSpeed = 12f;

    private RectTransform rect;
    private CanvasGroup cg;
    
    private Vector2 showPosition; // จุดโชว์ (กลางจอ)
    private Vector2 hidePosition; // จุดซ่อน
    
    private Vector2 targetPosition;
    private float targetAlpha;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();

        // จำตำแหน่งกึ่งกลางที่มันตั้งอยู่ตั้งแต่แรกไว้
        showPosition = rect.anchoredPosition; 
        hidePosition = showPosition + hideOffset;

        // 🟢 เริ่มต้นเกมมา สั่งให้มันซ่อนตัวรอไว้เลย (ไม่ต้องใช้ SetActive(false) แล้ว)
        ForceHide(); 
    }

    void Update()
    {
        // ทำการขยับตำแหน่งและปรับความใสอย่างนุ่มนวลทุกเฟรม
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, Time.unscaledDeltaTime * slideSpeed);
        cg.alpha = Mathf.Lerp(cg.alpha, targetAlpha, Time.unscaledDeltaTime * slideSpeed);
    }

    public void ShowPanel()
    {
        targetPosition = showPosition;
        targetAlpha = 1f;
        
        // เปิดให้เมาส์คลิกปุ่มได้
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    public void HidePanel()
    {
        targetPosition = hidePosition;
        targetAlpha = 0f;
        
        // ปิดการคลิก กันผู้เล่นกดโดนปุ่มตอนที่จอมันกำลังใส
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    // เอาไว้วาร์ปซ่อนทันทีแบบไม่มีแอนิเมชัน (ใช้ตอนเริ่มเกม)
    private void ForceHide()
    {
        rect.anchoredPosition = hidePosition;
        targetPosition = hidePosition;
        cg.alpha = 0f;
        targetAlpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }
}