using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("ลาก Image โลโก้ที่มี CanvasGroup มาใส่ตรงนี้")]
    public CanvasGroup logoGroup;
    
    [Header("Timings Settings")]
    public float delayBeforeShow = 0.5f; // รอกี่วิก่อนโลโก้เริ่มโผล่
    public float fadeInTime = 1.5f;      // เวลาตอนเฟดสว่างขึ้น
    public float showTime = 2.0f;        // เวลาที่โชว์โลโก้ค้างไว้
    public float fadeOutTime = 1.5f;     // เวลาตอนเฟดจางลง

    [Header("Scene Transition")]
    [Tooltip("เลข Build Index ของหน้า Main Menu")]
    public int mainMenuSceneIndex = 1; 

    private bool isSkipping = false;

    void Start()
    {
        // เริ่มต้นให้โลโก้โปร่งใส (มองไม่เห็น) รอไว้ก่อน
        if (logoGroup != null) logoGroup.alpha = 0f;
        
        // เริ่มกระบวนการโชว์โลโก้
        StartCoroutine(IntroSequence());
    }

    void Update()
    {
        // 🟢 ถ้าผู้เล่นกดปุ่มอะไรก็ตามบนคีย์บอร์ดหรือเมาส์ ให้ข้ามโลโก้ทันที
        if (Input.anyKeyDown && !isSkipping)
        {
            isSkipping = true;
            StopAllCoroutines(); // หยุดแอนิเมชันเดิม
            StartCoroutine(SkipIntroRoutine());
        }
    }

    private IEnumerator IntroSequence()
    {
        // 1. รอแป๊บนึงก่อนเริ่ม
        yield return new WaitForSeconds(delayBeforeShow);

        // 2. Fade In (ค่อยๆ สว่าง)
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            if (logoGroup != null) logoGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInTime);
            yield return null;
        }
        if (logoGroup != null) logoGroup.alpha = 1f;

        // 3. Show (ค้างไว้ให้คนอ่าน)
        yield return new WaitForSeconds(showTime);

        // 4. Fade Out (ค่อยๆ จางหาย)
        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            if (logoGroup != null) logoGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutTime);
            yield return null;
        }
        if (logoGroup != null) logoGroup.alpha = 0f;

        // 5. โหลดเข้า Main Menu
        GoToMainMenu();
    }

    private IEnumerator SkipIntroRoutine()
    {
        // 🟢 เร่งให้โลโก้จางหายอย่างรวดเร็ว (0.3 วินาที) ตอนกดข้าม
        float elapsed = 0f;
        float startAlpha = logoGroup != null ? logoGroup.alpha : 0f;
        float quickFadeTime = 0.3f;

        while (elapsed < quickFadeTime)
        {
            elapsed += Time.deltaTime;
            if (logoGroup != null) logoGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / quickFadeTime);
            yield return null;
        }
        
        GoToMainMenu();
    }

    private void GoToMainMenu()
    {
        // 🟢 ดึงระบบ ScreenFader ของเรามาใช้งานตอนสลับฉากให้จอมืดสมูทๆ
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToScene(mainMenuSceneIndex);
        }
        else
        {
            // ถ้าเผลอลืมใส่ ScreenFader ไว้ ก็ให้มันโหลดดื้อๆ ไปเลย จะได้ไม่ค้าง
            SceneManager.LoadScene(mainMenuSceneIndex);
        }
    }
}