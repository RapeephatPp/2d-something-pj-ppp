using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class CameraJuiceFX : MonoBehaviour
{
    public static CameraJuiceFX Instance;

    private Volume globalVolume;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration; // ยืดสีขอบจอเวลาพุ่ง (Dash)
    private LensDistortion lensDistortion; // บิดเลนส์เวลาโดนตีแรงๆ

    [Header("Dash Effect Settings")]
    public float dashAberrationIntensity = 1f;
    public float dashEffectDuration = 0.2f;

    [Header("Low Health Vignette")]
    public Color normalVignetteColor = Color.black;
    public Color dangerVignetteColor = Color.red;
    public float normalVignetteIntensity = 0.25f;
    public float dangerVignetteIntensity = 0.45f;
    public float pulseSpeed = 5f;

    private bool isLowHealth = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        
        globalVolume = GetComponent<Volume>();
        
        // ดึง Effect ที่อยู่ใน Volume มาเก็บไว้ใช้งาน
        globalVolume.profile.TryGet(out vignette);
        globalVolume.profile.TryGet(out chromaticAberration);
        globalVolume.profile.TryGet(out lensDistortion);
    }

    void Update()
    {
        // ถ้าเลือดน้อย ให้ขอบจอสีแดงเต้นตุบๆ
        if (isLowHealth && vignette != null)
        {
            float pingPong = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            vignette.color.value = Color.Lerp(dangerVignetteColor, normalVignetteColor, pingPong);
            vignette.intensity.value = Mathf.Lerp(dangerVignetteIntensity, normalVignetteIntensity, pingPong);
        }
    }

    // 🟢 เรียกใช้ฟังก์ชันนี้ตอนกดพุ่ง (Dash)
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
        chromaticAberration.intensity.value = dashAberrationIntensity;
        
        float elapsed = 0.25f;
        while (elapsed < dashEffectDuration)
        {
            elapsed += Time.deltaTime;
            // ค่อยๆ ลดความเบลอ/ยืดสี ลงจนกลับเป็น 0
            chromaticAberration.intensity.value = Mathf.Lerp(dashAberrationIntensity, 0.25f, elapsed / dashEffectDuration);
            yield return null;
        }
        chromaticAberration.intensity.value = 0.25f;
    }

    // 🟢 เรียกใช้ฟังก์ชันนี้จาก UIManager เพื่อบอกว่าเลือดกำลังจะหมด
    public void SetDangerState(bool state)
    {
        isLowHealth = state;
        if (!state && vignette != null)
        {
            // คืนค่าขอบจอกลับเป็นปกติ
            vignette.color.value = normalVignetteColor;
            vignette.intensity.value = normalVignetteIntensity;
        }
    }
}