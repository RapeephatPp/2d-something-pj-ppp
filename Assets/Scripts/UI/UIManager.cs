using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; 

    [Header("Health Bar UI (Image Filled)")]
    public Image healthFill;      
    public Image easeHealthFill;  
    public float lerpSpeed = 5f;  
    
    [Header("Health Bar Fader (🌟 ของใหม่!)")]
    public HealthBarAutoFade healthBarFader; // 🟢 ลากก้อนแม่หลอดเลือดที่มีสคริปต์เฟดมาใส่

    private float targetHealthPercent = 1f; 

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
        
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.GetComponent<PlayerController>();
    }

    void Update()
    {
        if (easeHealthFill != null && healthFill != null)
        {
            if (easeHealthFill.fillAmount != targetHealthPercent)
            {
                easeHealthFill.fillAmount = Mathf.Lerp(easeHealthFill.fillAmount, targetHealthPercent, lerpSpeed * Time.deltaTime);
            }
        }

        if (swordCooldownFill != null && player != null)
        {
            swordCooldownFill.fillAmount = PlayerController.isArmed ? 1f : player.GetSwordCooldownPercentage();
            swordCooldownFill.color = swordCooldownFill.fillAmount < 1f ? new Color(1, 1, 1, 0.5f) : Color.white;
        }
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        targetHealthPercent = (float)currentHealth / maxHealth;
        
        if (CameraJuiceFX.Instance != null)
        {
            bool inDanger = targetHealthPercent <= 0.3f;
            CameraJuiceFX.Instance.SetDangerState(inDanger);
        }
        
        if (healthFill != null)
        {
            healthFill.fillAmount = targetHealthPercent;
        }

        // 🌟 สั่งให้หลอดเลือดเด้งสว่างขึ้นมา ทุกครั้งที่เลือดมีการเปลี่ยนแปลง!
        if (healthBarFader != null)
        {
            healthBarFader.TriggerShow();
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