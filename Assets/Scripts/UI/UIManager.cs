using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; 

    [Header("Health Bar UI (Image Filled)")]
    public Image healthFill;      // 🟢 ลาก Image เลือดสีแดงมาใส่
    public Image easeHealthFill;  // 🟢 ลาก Image เลือดสีขาว (ที่วิ่งตาม) มาใส่
    public float lerpSpeed = 5f;  // ความเร็วในการไหลของหลอดเลือดตามหลัง

    private float targetHealthPercent = 1f; // เก็บค่า % เลือดเป้าหมาย (0.0 ถึง 1.0)

    [Header("Skill Cooldown UI")]
    public Image swordCooldownFill;  

    [Header("Game Over UI")]
    public UIPanelTransition gameOverPanel; 

    private PlayerController player;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.HidePanel(); 
        
        // หาตัว Player เพื่อเอามาดึงค่าคูลดาวน์
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.GetComponent<PlayerController>();
    }

    void Update()
    {
        // 🟢 1. ทำแอนิเมชันหลอดเลือดสีขาวค่อยๆ ลดตาม (Ease Health)
        if (easeHealthFill != null && healthFill != null)
        {
            // ถ้าหลอดสียังไม่เท่ากัน ให้มันค่อยๆ ไหลไปหาเป้าหมาย
            if (easeHealthFill.fillAmount != targetHealthPercent)
            {
                easeHealthFill.fillAmount = Mathf.Lerp(easeHealthFill.fillAmount, targetHealthPercent, lerpSpeed * Time.deltaTime);
            }
        }

        // 🟢 2. อัปเดตคูลดาวน์ดาบแบบเรียลไทม์
        if (swordCooldownFill != null && player != null)
        {
            swordCooldownFill.fillAmount = PlayerController.isArmed ? 1f : player.GetSwordCooldownPercentage();
            swordCooldownFill.color = swordCooldownFill.fillAmount < 1f ? new Color(1, 1, 1, 0.5f) : Color.white;
        }
    }

    // 🟢 อัปเกรด: รับค่า maxHealth มาคำนวณเปอร์เซ็นต์
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        // คำนวณเลือดเป็นเปอร์เซ็นต์ (ต้องใส่ float ไม่งั้นหารกันจะได้ 0)
        targetHealthPercent = (float)currentHealth / maxHealth;
        
        if (healthFill != null)
        {
            // หลอดแดง ลดฮวบทันที
            healthFill.fillAmount = targetHealthPercent;
        }
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.ShowPanel();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Button_Respawn()
    {
        if (gameOverPanel != null) gameOverPanel.HidePanel();
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        if (ScreenFader.Instance != null) ScreenFader.Instance.FadeToScene(currentScene);
        else SceneManager.LoadScene(currentScene);
    }

    public void Button_MainMenu()
    {
        if (gameOverPanel != null) gameOverPanel.HidePanel();
        Time.timeScale = 1f; 
        if (ScreenFader.Instance != null) ScreenFader.Instance.FadeToScene(0); 
        else SceneManager.LoadScene(0);
    }
}