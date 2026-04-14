using UnityEngine;

public class CharacterSwitcher : MonoBehaviour
{
    public static CharacterSwitcher Instance;

    [Header("Characters")]
    public GameObject unarmedPlayer; 
    public GameObject armedPlayer;   

    [Header("Animators")]
    public Animator unarmedAnimator; 
    public Animator armedAnimator;   

    [Header("References")]
    public PlayerController playerController; 
    public CameraFollow cameraFollow; 

    public bool isArmed { get; private set; } 
    public Vector3 savedPreTutorialPosition; 
    
    // 🟢 เปลี่ยนตัวแปรจุดเซฟให้เป็น static เพื่อให้จำข้ามการโหลดฉากได้
    public static Vector3 currentCheckpointPosition; 
    public static bool hasCheckpoint = false;
    public static bool savedIsArmed = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // 🟢 ถ้าเคยเซฟไว้แล้ว ให้โหลดข้อมูลมาเกิดใหม่
        if (hasCheckpoint)
        {
            if (savedIsArmed) ForceArmed();
            else ForceUnarmed();

            TeleportActivePlayer(currentCheckpointPosition);
            
            // รีเซ็ตเลือดให้เต็ม
            PlayerController activePC = isArmed ? armedPlayer.GetComponent<PlayerController>() : unarmedPlayer.GetComponent<PlayerController>();
            if (activePC != null) activePC.currentHealth = activePC.maxHealth;
            if (UIManager.Instance != null && activePC != null) UIManager.Instance.UpdateHealth(activePC.maxHealth);
        }
        else
        {
            ForceUnarmed();
            if (unarmedPlayer != null) currentCheckpointPosition = unarmedPlayer.transform.position;
        }
    }

    public void SwitchToArmed()
    {
        if (isArmed) return;
        PlayerController unarmedPC = unarmedPlayer.GetComponent<PlayerController>();
        PlayerController armedPC = armedPlayer.GetComponent<PlayerController>();

        if (unarmedPC != null && armedPC != null)
        {
            armedPC.currentHealth = unarmedPC.currentHealth;
            armedPC.currentGuardGauge = unarmedPC.currentGuardGauge; 
            armedPC.SetDashCooldown(unarmedPC.GetDashCooldown());    
        }

        // 🟢 [เพิ่มกลับเข้ามา] สั่งให้ร่างถือดาบ วาร์ปมาทับร่างมือเปล่าเป๊ะๆ และหันหน้าไปทางเดียวกัน
        armedPlayer.transform.position = unarmedPlayer.transform.position;
        armedPlayer.transform.localScale = unarmedPlayer.transform.localScale;

        ForceArmed();
    }

    public void SwitchToUnarmed()
    {
        if (!isArmed) return;
        PlayerController unarmedPC = unarmedPlayer.GetComponent<PlayerController>();
        PlayerController armedPC = armedPlayer.GetComponent<PlayerController>();

        if (unarmedPC != null && armedPC != null)
        {
            unarmedPC.currentHealth = armedPC.currentHealth;
            unarmedPC.currentGuardGauge = armedPC.currentGuardGauge; 
            unarmedPC.SetDashCooldown(armedPC.GetDashCooldown());    
        }

        // 🟢 [เพิ่มกลับเข้ามา] สั่งให้ร่างมือเปล่า วาร์ปมาทับร่างถือดาบเป๊ะๆ และหันหน้าไปทางเดียวกัน
        unarmedPlayer.transform.position = armedPlayer.transform.position;
        unarmedPlayer.transform.localScale = armedPlayer.transform.localScale;

        ForceUnarmed();
    }

    private void ForceArmed()
    {
        unarmedPlayer.SetActive(false);
        armedPlayer.SetActive(true);
        isArmed = true;
        PlayerController.isArmed = true;
        
        if (playerController != null && armedAnimator != null) playerController.ChangeAnimator(armedAnimator);
        if (cameraFollow != null) cameraFollow.target = armedPlayer.transform;
    }

    private void ForceUnarmed()
    {
        unarmedPlayer.SetActive(true);
        armedPlayer.SetActive(false);
        isArmed = false;
        
        if (playerController != null) 
        {
            PlayerController.isArmed = false;
            if (unarmedAnimator != null) playerController.ChangeAnimator(unarmedAnimator);
        }

        if (cameraFollow != null) cameraFollow.target = unarmedPlayer.transform;
    }

    public void TeleportActivePlayer(Vector3 newPosition)
    {
        if (isArmed) armedPlayer.transform.position = newPosition;
        else unarmedPlayer.transform.position = newPosition;
    }
}