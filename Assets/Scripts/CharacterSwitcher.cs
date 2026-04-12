using UnityEngine;

public class CharacterSwitcher : MonoBehaviour
{
    public static CharacterSwitcher Instance;

    [Header("Characters")]
    public GameObject unarmedPlayer; 
    public GameObject armedPlayer;   

    [Header("Animators")]
    public Animator unarmedAnimator; // 🟢 ลาก Animator ตัวมือเปล่ามาใส่
    public Animator armedAnimator;   // 🟢 ลาก Animator ตัวถือดาบมาใส่

    [Header("References")]
    public PlayerController playerController; // 🟢 ลากสคริปต์ PlayerController มาใส่
    public CameraFollow cameraFollow; 

    public bool isArmed { get; private set; } 
    public Vector3 savedPreTutorialPosition; 

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        ForceUnarmed();
    }

    public void SwitchToArmed()
    {
        if (isArmed) return;

        // 🟢 ก๊อปปี้เลือดจากตัวมือเปล่า ไปให้ตัวถือดาบ
        PlayerController unarmedPC = unarmedPlayer.GetComponent<PlayerController>();
        PlayerController armedPC = armedPlayer.GetComponent<PlayerController>();
        if (unarmedPC != null && armedPC != null) armedPC.currentHealth = unarmedPC.currentHealth;

        armedPlayer.transform.position = unarmedPlayer.transform.position;
        armedPlayer.transform.localScale = unarmedPlayer.transform.localScale;

        unarmedPlayer.SetActive(false);
        armedPlayer.SetActive(true);

        if (playerController != null && armedAnimator != null) 
            playerController.ChangeAnimator(armedAnimator);

        PlayerController.isArmed = true;

        if (cameraFollow != null) cameraFollow.target = armedPlayer.transform;
        isArmed = true;
    }

    public void SwitchToUnarmed()
    {
        if (!isArmed) return;

        // 🟢 ก๊อปปี้เลือดจากตัวถือดาบ ไปให้ตัวมือเปล่า
        PlayerController unarmedPC = unarmedPlayer.GetComponent<PlayerController>();
        PlayerController armedPC = armedPlayer.GetComponent<PlayerController>();
        if (unarmedPC != null && armedPC != null) unarmedPC.currentHealth = armedPC.currentHealth;

        unarmedPlayer.transform.position = armedPlayer.transform.position;
        unarmedPlayer.transform.localScale = armedPlayer.transform.localScale;

        armedPlayer.SetActive(false);
        unarmedPlayer.SetActive(true);

        if (playerController != null && unarmedAnimator != null) 
            playerController.ChangeAnimator(unarmedAnimator);

        PlayerController.isArmed = false;

        if (cameraFollow != null) cameraFollow.target = unarmedPlayer.transform;
        isArmed = false;
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