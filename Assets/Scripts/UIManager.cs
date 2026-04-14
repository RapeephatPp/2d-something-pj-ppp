using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; 

    [Header("Health Bar UI (อัปเกรดใหม่)")]
    public Slider healthSlider;      // หลอดเลือดจริง (สีแดง)
    public Slider easeHealthSlider;  // หลอดเลือดตามหลัง (สีขาว/เหลือง)
    public float lerpSpeed = 5f;     // ความเร็วในการไหลของหลอดเลือดตามหลัง

    [Header("Skill Cooldown UI")]
    public Image swordCooldownFill;  // 🟢 ลาก Image ที่ปรับ Image Type เป็น Filled มาใส่

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
        // 🟢 1. ทำแอนิเมชันหลอดเลือดค่อยๆ ลดตาม (Ease Health)
        if (healthSlider != null && easeHealthSlider != null)
        {
            if (healthSlider.value != easeHealthSlider.value)
            {
                easeHealthSlider.value = Mathf.Lerp(easeHealthSlider.value, healthSlider.value, lerpSpeed * Time.deltaTime);
            }
        }

        // 🟢 2. อัปเดตคูลดาวน์ดาบแบบเรียลไทม์
        if (swordCooldownFill != null && player != null)
        {
            // ถ้าถือดาบอยู่ ให้หลอดสว่างและเต็ม / ถ้าปาไปแล้ว ให้หลอดค่อยๆ ชาร์จ
            swordCooldownFill.fillAmount = PlayerController.isArmed ? 1f : player.GetSwordCooldownPercentage();
            
            // ทำให้สีซีดลงตอนที่ยังคูลดาวน์ไม่เสร็จ
            swordCooldownFill.color = swordCooldownFill.fillAmount < 1f ? new Color(1, 1, 1, 0.5f) : Color.white;
        }
    }

    // 🟢 อัปเกรด: รับค่า maxHealth มาด้วยเพื่อตั้งขนาดหลอด
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
            
            if (easeHealthSlider != null) easeHealthSlider.maxValue = maxHealth;
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