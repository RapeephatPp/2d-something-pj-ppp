using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Replacement Object")]
    [Tooltip("เอา Prefab ของแท่นวางเปล่าๆ หรือของที่จะให้โผล่มาแทนที่ มาใส่ตรงนี้")]
    public GameObject replacementPrefab; 

    [Header("Audio SFX")]
    public AudioClip pickupSound; // 🟢 เสียงเก็บอาวุธ (แกร๊ง!)

    private bool isPlayerNear = false;

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(interactKey))
        {
            // 🟢 เล่นเสียงหยิบอาวุธ
            if (AudioManager.Instance != null && pickupSound != null)
                AudioManager.Instance.PlaySFX(pickupSound, 1.0f);

            CharacterSwitcher.Instance.SwitchToArmed();
            
            if (replacementPrefab != null)
            {
                Instantiate(replacementPrefab, transform.position, transform.rotation);
            }

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = false;
    }
}