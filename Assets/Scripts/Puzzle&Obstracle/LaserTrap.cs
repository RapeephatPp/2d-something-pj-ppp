using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal; 

// 🟢 เพิ่ม typeof(AudioSource) เข้าไป เพื่อให้ Unity สร้างตัวเล่นเสียงให้อัตโนมัติ
[RequireComponent(typeof(LineRenderer), typeof(AudioSource))]
public class LaserTrap : MonoBehaviour
{
    public enum LaserType 
    { 
        Normal,             
        SwordPassable,      
        AbsoluteBarrier     
    }

    public enum LaserDirection { Right, Left, Up, Down, CustomWorld, CustomLocal }

    [Header("Laser Core Settings")]
    public LaserType laserType = LaserType.Normal; 
    public LaserDirection shootDirection = LaserDirection.Right;
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
    [HideInInspector] public Color laserColor = Color.red;
    public float glowIntensity = 3.5f;
    public float baseWidth = 0.15f;
    public float pulseAmplitude = 0.05f;
    public float pulseSpeed = 15f;
    public GameObject hitSparkPrefab;

    [Header("Blinking Mode (Optional)")]
    public bool isBlinking = false;
    public float timeOn = 2f;
    public float timeOff = 2f;
    
    [Header("Audio SFX")]
    public AudioClip laserHumSound; // 🟢 ไฟล์เสียงเลเซอร์ครางสั้นๆ ที่เอามาวนลูป

    private LineRenderer lr;
    private float nextDamageTime = 0f;
    private bool isLaserActive;
    private float blinkTimer = 0f;
    private GameObject currentSpark;
    
    private AudioSource audioSource;    // 🟢 ลำโพงส่วนตัวของเลเซอร์
    private bool wasLaserActive;        // 🟢 เอาไว้จำว่าเฟรมที่แล้วเลเซอร์ติดอยู่ไหม

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
        
        Color hdrColor = new Color(laserColor.r * glowIntensity, laserColor.g * glowIntensity, laserColor.b * glowIntensity, laserColor.a);

        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.startColor = hdrColor;
            lr.endColor = hdrColor;
        }

        if (currentSpark != null)
        {
            SpriteRenderer sparkSr = currentSpark.GetComponent<SpriteRenderer>();
            if (sparkSr != null) sparkSr.color = hdrColor; 

            UnityEngine.Rendering.Universal.Light2D sparkLight = currentSpark.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
            if (sparkLight != null) 
            {
                sparkLight.color = laserColor; 
                sparkLight.intensity = glowIntensity; 
            }

            HitSparkJuice sparkJuice = currentSpark.GetComponent<HitSparkJuice>();
            if (sparkJuice != null)
            {
                sparkJuice.externalScaleMultiplier = baseWidth / 0.15f; 
            }
        }
    }

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        
        // 🟢 ตั้งค่าเครื่องเล่นเสียงให้วนลูป
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;          
        audioSource.playOnAwake = false;  

        if (hitSparkPrefab != null)
        {
            currentSpark = Instantiate(hitSparkPrefab, transform.position, Quaternion.identity);
            currentSpark.transform.SetParent(this.transform);
            currentSpark.SetActive(false);
        }

        UpdateLaserColor(); 
        
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;

        if (lr.material == null || lr.material.name == "Default-Material")
            lr.material = new Material(Shader.Find("Sprites/Default"));

        isLaserActive = startActive;
        wasLaserActive = !isLaserActive; // หลอกสคริปต์ให้มันเช็คอัปเดตเสียงตั้งแต่เฟรมแรก
        blinkTimer = timeOn;
    }

    void Update()
    {
        HandleBlinking();

        // 🟢 เช็คว่ามีการ "เปลี่ยนสถานะ" เลเซอร์หรือเปล่า จะได้เปิด/ปิดเสียงถูกจังหวะ
        if (isLaserActive && !wasLaserActive)
        {
            if (laserHumSound != null)
            {
                audioSource.clip = laserHumSound;
                audioSource.Play();
            }
        }
        else if (!isLaserActive && wasLaserActive)
        {
            audioSource.Stop(); // เลเซอร์ดับ สั่งตัดเสียงฉับเลย!
        }
        
        wasLaserActive = isLaserActive; // อัปเดตความจำไว้ใช้เฟรมต่อไป

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

    private void OnDisable()
    {
        if (currentSpark != null)
        {
            currentSpark.SetActive(false);
        }
        
        // 🟢 ถ้าโดน Culler สั่งปิด Object ไป ต้องรีบดับเสียงด้วย ไม่งั้นเสียงจะค้าง!
        if (audioSource != null)
        {
            audioSource.Stop();
            wasLaserActive = false;
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
                        float pushDirX = Mathf.Sign(pc.transform.position.x - actualHit.point.x);
                        if (Mathf.Abs(pc.transform.position.x - actualHit.point.x) < 0.05f) 
                        {
                            pushDirX = -Mathf.Sign(pc.transform.localScale.x); 
                        }
                        
                        Vector2 knockbackDir = new Vector2(pushDirX, 0.7f).normalized; 

                        pc.TakeDamage(damage, bypass);
                        nextDamageTime = Time.time + damageTickRate;
                        
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
    public void TurnOffLaser() 
    { 
        isLaserActive = false; 
        isBlinking = false; 
        if (currentSpark != null) currentSpark.SetActive(false);
    }
    public void ToggleLaser() { isLaserActive = !isLaserActive; }
}