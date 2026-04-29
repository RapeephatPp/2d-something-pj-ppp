using UnityEngine;

public class RisingLava : MonoBehaviour
{
    public float riseSpeed = 0.5f;
    public bool isRising = false; // Default is false

    void Update()
    {
        // Only move up if triggered
        if (isRising)
        {
            transform.Translate(Vector3.up * riseSpeed * Time.deltaTime);
        }
    }

    // New function to start the lava
    public void StartRising()
    {
        isRising = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerController>().TakeDamage(999); 
        }
    }
}