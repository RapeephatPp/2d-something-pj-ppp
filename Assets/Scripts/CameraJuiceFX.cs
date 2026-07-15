using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class CameraJuiceFX : MonoBehaviour
{
    public static CameraJuiceFX Instance;

    private Volume globalVolume;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;

    [Header("Dash Effect Settings")] public float dashAberrationIntensity = 1f;
    public float dashEffectDuration = 0.2f;

    [Header("Combat Focus (ตอนเจอศัตรู)")] public float normalAberration = 0f; // ค่าปกติ
    public float combatAberration = 0.25f; // ค่าตอนเจอศัตรู
    public float transitionSpeed = 3f; // ความเร็วตอนเปลี่ยนค่าสมูทๆ

    private float targetAberration = 0f; // เป้าหมายของค่าสีที่ยืด
    private bool isDashing = false; // เช็คว่ากำลัง Dash อยู่ไหม (Dash จะ Override ทุกอย่าง)
    
    private int enemiesInCombat = 0;

    [Header("Low Health Vignette")] public Color normalVignetteColor = Color.black;
    public Color dangerVignetteColor = Color.red;
    public float normalVignetteIntensity = 0.25f;
    public float dangerVignetteIntensity = 0.45f;
    public float pulseSpeed = 5f;
    private bool isLowHealth = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        globalVolume = GetComponent<Volume>();

        if (globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out vignette);
            globalVolume.profile.TryGet(out chromaticAberration);
        }

        targetAberration = normalAberration; // เริ่มเกมมาให้เป็น 0
    }

    void Update()
    {
        if (vignette != null)
        {
            if (isLowHealth)
            {
                // เลือดน้อย: แดงเต้นตุบๆ (โค้ดเดิมของคุณ)
                float pingPong = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
                vignette.color.value = Color.Lerp(dangerVignetteColor, normalVignetteColor, pingPong);
                vignette.intensity.value = Mathf.Lerp(dangerVignetteIntensity, normalVignetteIntensity, pingPong);
            }
            else
            {
                // 🟢 เลือดปกติ: เฟดสีขอบจอกลับมาเป็นสีดำแบบสมูทๆ!
                vignette.color.value = Color.Lerp(vignette.color.value, normalVignetteColor, Time.deltaTime * transitionSpeed);
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, normalVignetteIntensity, Time.deltaTime * transitionSpeed);
            }
        }

        // 🟢 ระบบปรับความเบลอสมูทๆ (ถ้าไม่ได้ Dash อยู่ ให้มันขยับไปหาค่าเป้าหมาย)
        if (chromaticAberration != null && !isDashing)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(
                chromaticAberration.intensity.value,
                targetAberration,
                Time.deltaTime * transitionSpeed
            );
        }
    }
    
    // 🟢 ฟังก์ชันสำหรับสั่งเปิด/ปิด โหมดตึงเครียด!
    public void SetCombatMode(bool inCombat)
    {
        if (inCombat)
        {
            enemiesInCombat++; // มีคนเห็นเราเพิ่มขึ้น
        }
        else
        {
            enemiesInCombat--; // มีคนตายหรือเลิกตาม
            if (enemiesInCombat < 0) enemiesInCombat = 0; // กันค่าติดลบ
        }

        // 🟢 ตัดสินใจเปลี่ยนเอฟเฟกต์จากจำนวนศัตรู
        if (enemiesInCombat > 0)
        {
            targetAberration = combatAberration; // สั่งยืดขอบจอ[cite: 2]
        }
        else
        {
            targetAberration = normalAberration; // สั่งคืนค่ากลับเป็น 0[cite: 2]
        }
    }

    public void TriggerDashJuice()
    {
        if (chromaticAberration != null)
        {
            StopAllCoroutines();
            StartCoroutine(DashEffectRoutine());
        }
    }

    private IEnumerator DashEffectRoutine()
    {
        isDashing = true; // ล็อคไว้ไม่ให้ Update() มาแย่งปรับค่า
        chromaticAberration.intensity.value = dashAberrationIntensity;

        float elapsed = 0f;
        while (elapsed < dashEffectDuration)
        {
            elapsed += Time.deltaTime;
            // ตอน Dash จบ ให้มันถอยกลับไปหาค่า target ปัจจุบัน (เผื่อว่าตอน Dash เสร็จยังสู้กับศัตรูอยู่)
            chromaticAberration.intensity.value =
                Mathf.Lerp(dashAberrationIntensity, targetAberration, elapsed / dashEffectDuration);
            yield return null;
        }

        chromaticAberration.intensity.value = targetAberration;
        isDashing = false; // ปลดล็อค
    }

    public void SetDangerState(bool state)
    {
        isLowHealth = state;
    }
}