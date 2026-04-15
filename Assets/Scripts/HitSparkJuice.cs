using UnityEngine;

public class HitSparkJuice : MonoBehaviour
{
    [Header("Scale Pulsing (ขยาย-หด)")]
    public float minScale = 0.8f;   // ขนาดเล็กสุด
    public float maxScale = 1.5f;   // ขนาดใหญ่สุด
    public float pulseSpeed = 30f;  // ความเร็วในการเต้นตุบๆ

    [Header("Rotation (หมุนควงสว่าน)")]
    public float spinSpeed = 1000f; // ความเร็วในการหมุน (องศาต่อวินาที) หมุนไวๆ จะดูสะใจมาก

    [Header("Auto Destroy (ใช้กับตอนฟันดาบ)")]
    [Tooltip("ติ๊กถูกถ้าใช้กับฟันดาบ (ให้มันหายไปเอง) / เอาออกถ้าใช้กับเลเซอร์")]
    public bool autoDestroy = false; 
    public float lifeTime = 0.15f;  // อายุไขถ้าเปิด Auto Destroy

    private Vector3 baseScale;

    void Awake()
    {
        // จำขนาดดั้งเดิมของภาพไว้ก่อน
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        // 🟢 ทริคเล็กๆ: สุ่มองศาเริ่มต้นทุกครั้งที่โผล่มา เอฟเฟกต์จะได้ดูไม่ซ้ำซาก
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        if (autoDestroy)
        {
            // สั่งทำลายตัวเองล่วงหน้า (เหมาะสำหรับใช้เป็นเอฟเฟกต์ฟันดาบ)
            Destroy(gameObject, lifeTime);
        }
    }

    void Update()
    {
        // 1. สั่งให้หมุนติ้วๆ ตลอดเวลา
        transform.Rotate(0, 0, spinSpeed * Time.deltaTime);

        // 2. ปรับขนาดใหญ่-เล็ก สลับไปมาด้วยสมการคลื่น Sine 
        // (Mathf.Sin จะได้ค่า -1 ถึง 1 เราเอามาแปลงให้เป็นช่วง 0 ถึง 1 แล้วจับไปหาขนาด Scale)
        float lerpValue = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        float currentScaleMultiplier = Mathf.Lerp(minScale, maxScale, lerpValue);

        transform.localScale = baseScale * currentScaleMultiplier;
    }
}