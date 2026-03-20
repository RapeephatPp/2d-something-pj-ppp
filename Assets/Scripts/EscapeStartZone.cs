using UnityEngine;

public class EscapeStartZone : MonoBehaviour
{
    [Header("Escape References")]
    public GameTimer gameTimer;
    public RisingLava lava;
    public GameObject gateToBlockBehind; // Wall that closes so player can't go back

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;

            // Trigger all escape mechanics
            if (gameTimer != null) gameTimer.StartTimer();
            if (lava != null) lava.StartRising();
            if (CameraShake.Instance != null) CameraShake.Instance.StartSway();
            
            // Block the path behind the player
            if (gateToBlockBehind != null) gateToBlockBehind.SetActive(true);

            Debug.Log("Escape Sequence Started!");
        }
    }
}