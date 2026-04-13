using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserTrap : MonoBehaviour
{
    // 🟢 ตัวเลือกทิศทางแบบ Dropdown ให้เลือกง่ายๆ ใน Inspector
    public enum LaserDirection { Right, Left, Up, Down, CustomWorld, CustomLocal }

    [Header("Laser Core Settings")]
    public LaserDirection shootDirection = LaserDirection.Right;
    [Tooltip("ใช้เฉพาะตอนเลือกโหมด Custom")]
    public Vector2 customDirection = Vector2.right; 
    public float maxDistance = 20f;
    public LayerMask hitLayers;
    public bool startActive = true; // 🟢 เลือกว่าเริ่มมาเปิด หรือ ปิด

    [Header("Damage & Knockback")]
    public int damage = 1;
    public float damageTickRate = 0.5f;
    public float knockbackForce = 10f; // 🟢 โดนแล้วกระเด็นด้วย!

    [Header("Visual Fixes")]
    public string sortingLayerName = "Default"; 
    public int sortingOrder = 20;               

    [Header("Game Feel (Juice)")]
    public Color laserColor = Color.red;
    public float baseWidth = 0.15f;
    public float pulseAmplitude = 0.05f;
    public float pulseSpeed = 15f;
    public GameObject hitSparkPrefab;

    [Header("Blinking Mode (Optional)")]
    public bool isBlinking = false;
    public float timeOn = 2f;
    public float timeOff = 2f;

    private LineRenderer lr;
    private float nextDamageTime = 0f;
    private bool isLaserActive;
    private float blinkTimer = 0f;
    private GameObject currentSpark;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startColor = laserColor;
        lr.endColor = laserColor;
        lr.useWorldSpace = true;
        
        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;

        if (lr.material == null || lr.material.name == "Default-Material")
        {
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        if (hitSparkPrefab != null)
        {
            currentSpark = Instantiate(hitSparkPrefab, transform.position, Quaternion.identity);
            currentSpark.SetActive(false);
        }

        // 🟢 เซ็ตสถานะตอนเริ่มเกม
        isLaserActive = startActive;
        blinkTimer = timeOn;
    }

    void Update()
    {
        HandleBlinking();

        if (isLaserActive)
        {
            lr.enabled = true;
            UpdateLaserLogic();
            AnimateLaser();
        }
        else
        {
            lr.enabled = false;
            if (currentSpark != null) currentSpark.SetActive(false);
        }
    }

    // 🟢 ฟังก์ชันแปลงคำสั่ง Dropdown ให้กลายเป็นทิศทางจริงๆ
    private Vector2 GetDirection()
    {
        switch (shootDirection)
        {
            case LaserDirection.Right: return Vector2.right;
            case LaserDirection.Left:  return Vector2.left;
            case LaserDirection.Up:    return Vector2.up;
            case LaserDirection.Down:  return Vector2.down;
            
            // CustomWorld = อิงตามแกนโลก (ใส่ 1, 1 คือยิงทะแยงขวาบนเสมอ)
            case LaserDirection.CustomWorld: return customDirection.normalized; 
            
            // CustomLocal = อิงตามการหมุนของ Object
            case LaserDirection.CustomLocal: return transform.TransformDirection(customDirection.normalized); 
            
            default: return Vector2.right;
        }
    }

    private void HandleBlinking()
    {
        if (!isBlinking) return;

        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0)
        {
            isLaserActive = !isLaserActive;
            blinkTimer = isLaserActive ? timeOn : timeOff;
        }
    }

    private void UpdateLaserLogic()
    {
        Vector2 startPos = transform.position;
        lr.SetPosition(0, startPos);

        // 🟢 เรียกใช้ทิศทางที่ตั้งค่าไว้
        Vector2 direction = GetDirection(); 
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, maxDistance, hitLayers);

        if (hit.collider != null)
        {
            lr.SetPosition(1, hit.point);

            if (currentSpark != null)
            {
                currentSpark.transform.position = hit.point;
                currentSpark.SetActive(true);
            }

            if (hit.collider.CompareTag("Player") && Time.time >= nextDamageTime)
            {
                PlayerController pc = hit.collider.GetComponent<PlayerController>();
                if (pc != null && !pc.isInvincible)
                {
                    pc.TakeDamage(damage);
                    nextDamageTime = Time.time + damageTickRate;
                    
                    // 🟢 ระบบผลักกระเด็น (Knockback) ให้เด้งไปตามทิศทางของเลเซอร์
                    Rigidbody2D pRb = pc.GetComponent<Rigidbody2D>();
                    if (pRb != null)
                    {
                        pRb.linearVelocity = Vector2.zero; // หยุดชะงัก
                        pRb.AddForce(direction * knockbackForce, ForceMode2D.Impulse); // ดีดปลิว!
                    }

                    if (CameraShake.Instance != null) 
                        CameraShake.Instance.StartManagedShake(0.15f, 0.1f);
                }
            }
        }
        else
        {
            lr.SetPosition(1, startPos + (direction * maxDistance));
            if (currentSpark != null) currentSpark.SetActive(false);
        }
    }

    private void AnimateLaser()
    {
        float currentWidth = baseWidth + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
        lr.startWidth = currentWidth;
        lr.endWidth = currentWidth * 0.8f;
    }

    // ==========================================
    // 🟢 PUBLIC API (สำหรับให้ปุ่ม/สวิตช์ มาสั่งงาน)
    // ==========================================
    public void TurnOnLaser() { isLaserActive = true; }
    public void TurnOffLaser() { isLaserActive = false; }
    public void ToggleLaser() { isLaserActive = !isLaserActive; }
}