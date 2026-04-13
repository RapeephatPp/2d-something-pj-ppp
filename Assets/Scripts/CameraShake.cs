using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 basePos;
    private Quaternion baseRot;
    private Coroutine activeShake;
    
    [Header("Escape Sway")]
    public bool isSwaying = false;   
    public float swayAmount = 0.3f; 
    public float swaySpeed = 15f;   
    
    void Awake()
    {
        Instance = this;
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
    }

    // 🟢 เพิ่ม Update เพื่อให้กล้องส่ายตลอดเวลาที่หนี
    void Update()
    {
        // จะส่ายก็ต่อเมื่อ ไม่ได้โดนสั่นจากการตี (activeShake == null)
        if (activeShake == null)
        {
            if (isSwaying)
            {
                // ใช้คลื่น Sine ส่ายแกน X ไปมา
                float offsetX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
                transform.localPosition = basePos + new Vector3(offsetX, 0, 0);
            }
            else
            {
                // พอหลุดโซนแล้ว ให้กล้องค่อยๆ เลื่อนกลับมาตรงกลางแบบนุ่มๆ
                transform.localPosition = Vector3.Lerp(transform.localPosition, basePos, Time.deltaTime * 5f);
            }
        }
    }

   
    public void StartSway()
    {
        isSwaying = true;
    }
    
    public void StopSway()
    {
        isSwaying = false;
    }

    public void StartManagedShake(float duration, float magnitude)
    {
        if (activeShake != null) StopCoroutine(activeShake);
        activeShake = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        Vector3 kickDir = Random.insideUnitCircle.normalized;
        float randomDrift = Random.Range(-300f, 300f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float percent = elapsed / duration;
            
            float damping = 1.0f - percent;
            damping = damping * damping; 

            float bounces = 2.5f; 
            float wave = Mathf.Cos(percent * Mathf.PI * (bounces * 2f)); 
            
            kickDir = Quaternion.Euler(0, 0, randomDrift * Time.unscaledDeltaTime) * kickDir;
            Vector3 offset = kickDir * (magnitude * damping * wave);

            transform.localPosition = basePos + offset;
            float roll = offset.x * 25f; 
            transform.localRotation = baseRot * Quaternion.Euler(0, 0, roll);

            yield return null;
        }

        transform.localPosition = basePos;
        transform.localRotation = baseRot;
        activeShake = null; 
    }
}