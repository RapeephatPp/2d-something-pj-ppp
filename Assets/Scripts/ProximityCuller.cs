using UnityEngine;

public class ProximityCuller : MonoBehaviour
{
    [Header("เป้าหมายที่ใช้เช็คระยะ (ถ้าว่างเดี๋ยวหาเอง)")]
    public Transform targetPlayer;

    [Header("ชิ้นส่วนที่จะเปิด/ปิด (ใส่ได้หลายชิ้น)")]
    public GameObject[] contentsToToggle; 

    [Header("ระยะห่างที่จะให้เริ่มทำงาน")]
    public float activationDistance = 20f;

    [Header("ความถี่ในการเช็ค (วินาที)")]
    public float checkInterval = 0.5f;

    [Space(10)]
    [Header("💥 โหมดล้างบาง NPC เมื่อเดินออกจากโซน")]
    public bool destroyNpcsOnExit = true;
    
    [Tooltip("ใส่ชื่อ Tag ของศัตรูที่คุณต้องการล้างทิ้ง (เช่น Enemy)")]
    public string targetTagToClear = "Enemy";

    private float _activationDistanceSqr;
    
    // 🌟 ตัวแปรใหม่: เอาไว้จำว่ารอบที่แล้วผู้เล่นอยู่ในโซนหรือเปล่า
    private bool _wasNear = false; 

    private void Start()
    {
        _activationDistanceSqr = activationDistance * activationDistance;

        foreach (var obj in contentsToToggle)
        {
            if (obj == this.gameObject)
            {
                Debug.LogError($"<color=red>🚨 ผิดพลาดที่ {gameObject.name}: ห้ามเอาตัวเองมาใส่ในช่องปิดเปิดเด็ดขาด!</color>");
                return;
            }
        }

        InvokeRepeating(nameof(CheckDistance), Random.Range(0f, checkInterval), checkInterval);
    }

    private void CheckDistance()
    {
        if (targetPlayer == null || !targetPlayer.gameObject.activeInHierarchy)
        {
            FindActivePlayer();
        }

        if (targetPlayer == null || contentsToToggle.Length == 0) return;

        Vector2 cullerPos = transform.position;
        Vector2 playerPos = targetPlayer.position;

        float sqrDistance = (cullerPos - playerPos).sqrMagnitude;
        bool isNear = sqrDistance <= _activationDistanceSqr;

        // ----------------------------------------------------
        // 💥 จุดสำคัญ: ถ้า "เคยอยู่ใกล้" แล้วตอนนี้ "เดินออกไปแล้ว"
        // ----------------------------------------------------
        if (!isNear && _wasNear)
        {
            if (destroyNpcsOnExit)
            {
                ClearWanderingNPCs();
            }
        }

        // สั่งเปิด-ปิด ตามระยะ (ทำงานต่อจากเดิม)
        foreach (var obj in contentsToToggle)
        {
            if (obj != null && obj.activeSelf != isNear)
            {
                obj.SetActive(isNear);
            }
        }

        // อัปเดตความจำไว้ใช้ในรอบต่อไป
        _wasNear = isNear; 
    }

    private void ClearWanderingNPCs()
    {
        // เข้าไปค้นหาลูกๆ (NPC) ที่อยู่ภายใต้ GameObject ในลิสต์ของเรา
        foreach (var obj in contentsToToggle)
        {
            if (obj == null) continue;

            // ⚠️ ทริคโปร: เวลาจะลบของในลูป ต้องลากถอยหลัง (จากล่างขึ้นบน) ไม่งั้น Index จะเลื่อนและ Error!
            for (int i = obj.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = obj.transform.GetChild(i);
                
                // ตรวจสอบว่าเป็น NPC ที่เดินเพ่นพ่านจริงไหม ผ่าน Tag
                if (child.CompareTag(targetTagToClear))
                {
                    // ลบทิ้งเลย! (หรือถ้าทำ Object Pooling ไว้ ให้เปลี่ยนเป็น child.gameObject.SetActive(false) เพื่อส่งกลับคลัง)
                    Destroy(child.gameObject);
                }
            }
        }
        Debug.Log($"<color=orange>Successfully Cleared '{targetTagToClear}' In zone {gameObject.name} !</color>");
    }

    private void FindActivePlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            if (p.activeInHierarchy)
            {
                targetPlayer = p.transform;
                return;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}