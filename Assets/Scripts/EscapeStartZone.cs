using UnityEngine;

public class EscapeStartZone : MonoBehaviour
{
    [Header("Escape References")]
    public GameTimer gameTimer;
    public RisingLava lava;
    public GameObject gateToBlockBehind; 

    // 🟢 [เพิ่มใหม่] อาเรย์สำหรับใส่ศัตรูที่คุณอยากให้มันตื่นตอนเหยียบเส้นนี้
    [Header("Enemies To Trigger")]
    public EnemyBehavior[] chaserEnemies; 

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;

            // เริ่มระบบหนีตายทั้งหมด
            if (gameTimer != null) gameTimer.StartTimer();
            if (lava != null) lava.StartRising();
            if (CameraShake.Instance != null) CameraShake.Instance.StartSway();
            
            if (gateToBlockBehind != null) gateToBlockBehind.SetActive(true);

            // 🟢 [เพิ่มใหม่] สั่งให้ศัตรูทุกตัวในลิสต์เริ่มวิ่ง!
            if (chaserEnemies != null)
            {
                foreach (EnemyBehavior enemy in chaserEnemies)
                {
                    if (enemy != null) enemy.TriggerChase();
                }
            }

            Debug.Log("Escape Sequence Started! RUN!");
        }
    }
}