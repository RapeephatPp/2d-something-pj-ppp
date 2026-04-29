using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class LaserTrap : MonoBehaviour
{
    public enum LaserType 
    { 
        Normal,             // ปกติ (ดาบปักได้, คนพุ่งหลบได้) -> 🔴 สีแดง
        SwordPassable,      // กรองแสง (ดาบทะลุได้, คนพุ่งหลบได้) -> 🟠 สีส้ม
        AbsoluteBarrier     // สังหาร (ดาบทะลุได้, คนห้ามผ่านเด็ดขาด) -> 🟣 สีม่วง
    }

    public enum LaserDirection { Right, Left, Up, Down, CustomWorld, CustomLocal }

    [Header("Laser Core Settings")]
    public LaserType laserType = LaserType.Normal; 
    public LaserDirection shootDirection = LaserDirection.Right;
    [Tooltip("ใช้เฉพาะตอนเลือกโหมด Custom")]
    public Vector2 customDirection = Vector2.right; 
    public float maxDistance = 20f;
    public LayerMask hitLayers;
    public bool startActive = true; 

    [Header("Damage & Knockback")]
    public int damage = 1;
    public float damageTickRate = 0.5f;
    public float knockbackForce = 15f; 

    [Header("Visual Fixes")]
    public string sortingLayerName = "Default"; 
    public int sortingOrder = 20;               

    [Header("Game Feel (Juice)")]
    // 🟢 ซ่อนการตั้งค่าสีไว้ เพราะเดี๋ยวโค้ดจะจัดการให้เอง!
    [HideInInspector] public Color laserColor = Color.red;
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

    // 🟢 ระบบเปลี่ยนสีให้ดูทันทีในหน้าจอ Unity (ไม่ต้องกด Play)
    private void OnValidate()
    {
        UpdateLaserColor();
    }

    private void UpdateLaserColor()
    {
        switch (laserType)
        {
            case LaserType.Normal: laserColor = Color.red; break;
            case LaserType.SwordPassable: laserColor = new Color(1f, 0.5f, 0f); break; 
            case LaserType.AbsoluteBarrier: laserColor = new Color(0.7f, 0f, 1f); break; 
        }
        
        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.startColor = laserColor;
            lr.endColor = laserColor;
        }

        // 🟢 เปลี่ยนสีและ "ขนาด" ของ Spark ให้ตรงกับเลเซอร์
        if (currentSpark != null)
        {
            SpriteRenderer sparkSr = currentSpark.GetComponent<SpriteRenderer>();
            if (sparkSr != null) sparkSr.color = laserColor;

            // ส่งค่าไปบอก HitSparkJuice
            HitSparkJuice sparkJuice = currentSpark.GetComponent<HitSparkJuice>();
            if (sparkJuice != null)
            {
                // นำ baseWidth มาหารด้วย 0.15 (ซึ่งเป็นค่าความกว้างมาตรฐาน)
                // ถ้าเลเซอร์หนา 0.30 Spark ก็จะใหญ่ขึ้นเป็น 2 เท่าทันที!
                sparkJuice.externalScaleMultiplier = baseWidth / 0.15f; 
            }
        }
    }

    void Start()
    {
        lr = GetComponent<LineRenderer>();

        // 🟢 1. สร้าง Spark ขึ้นมาก่อน (ต้องทำก่อน UpdateLaserColor)
        if (hitSparkPrefab != null)
        {
            currentSpark = Instantiate(hitSparkPrefab, transform.position, Quaternion.identity);
            currentSpark.SetActive(false);
        }

        // 🟢 2. เรียกใช้การตั้งค่าสี (มันจะไปเปลี่ยนสี Spark ที่เพิ่งเสกมาให้ด้วย)
        UpdateLaserColor(); 
        
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;

        if (lr.material == null || lr.material.name == "Default-Material")
            lr.material = new Material(Shader.Find("Sprites/Default"));

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

    private Vector2 GetDirection()
    {
        switch (shootDirection)
        {
            case LaserDirection.Right: return Vector2.right;
            case LaserDirection.Left:  return Vector2.left;
            case LaserDirection.Up:    return Vector2.up;
            case LaserDirection.Down:  return Vector2.down;
            case LaserDirection.CustomWorld: return customDirection.normalized; 
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

        Vector2 direction = GetDirection(); 
        
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, direction, maxDistance, hitLayers);
        RaycastHit2D actualHit = new RaycastHit2D();

        foreach (RaycastHit2D h in hits)
        {
            if ((laserType == LaserType.SwordPassable || laserType == LaserType.AbsoluteBarrier) 
                && h.collider.GetComponent<ThrownSword>() != null)
            {
                continue; 
            }

            actualHit = h;
            break; 
        }

        if (actualHit.collider != null)
        {
            lr.SetPosition(1, actualHit.point);

            if (currentSpark != null)
            {
                currentSpark.transform.position = actualHit.point;
                currentSpark.SetActive(true);
            }

            if (actualHit.collider.CompareTag("Player") && Time.time >= nextDamageTime)
            {
                PlayerController pc = actualHit.collider.GetComponent<PlayerController>();
                if (pc != null)
                {
                    bool bypass = (laserType == LaserType.AbsoluteBarrier);
                    
                    if ((!pc.isInvincible || bypass) && !pc.isDead) 
                    {
                        // 🟢 1. คำนวณทิศทางผลักออก "ซ้าย หรือ ขวา" จากจุดที่โดนเลเซอร์
                        float pushDirX = Mathf.Sign(pc.transform.position.x - actualHit.point.x);
                        
                        // ถ้าเดินชนตรงกลางเป๊ะๆ (0) ให้เด้งสวนทางกับหน้าที่หันอยู่
                        if (Mathf.Abs(pc.transform.position.x - actualHit.point.x) < 0.05f) 
                        {
                            pushDirX = -Mathf.Sign(pc.transform.localScale.x); 
                        }
                        
                        // สร้างแรงกระเด็นเฉียงขึ้นฟ้าเล็กน้อย (X, Y)
                        Vector2 knockbackDir = new Vector2(pushDirX, 0.7f).normalized; 

                        // 🟢 2. ทำดาเมจ
                        pc.TakeDamage(damage, bypass);
                        nextDamageTime = Time.time + damageTickRate;
                        
                        // 🟢 3. เรียกใช้ระบบกระเด็นใหม่! (กระเด็นเป็นเวลา 0.25 วินาที)
                        if (!pc.isDead) 
                        {
                            pc.ApplyKnockback(knockbackDir * knockbackForce, 0.25f); 
                        }

                        if (CameraShake.Instance != null) 
                            CameraShake.Instance.StartManagedShake(0.15f, 0.1f);
                    }
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

    public void TurnOnLaser() { isLaserActive = true; }
    public void TurnOffLaser() { isLaserActive = false; }
    public void ToggleLaser() { isLaserActive = !isLaserActive; }
}