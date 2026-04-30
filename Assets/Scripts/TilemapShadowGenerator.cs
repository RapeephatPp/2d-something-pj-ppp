using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Reflection;
using System.Collections.Generic;

[RequireComponent(typeof(CompositeCollider2D))]
public class TilemapShadowGenerator : MonoBehaviour
{
    [Header("⚙️ การตั้งค่า Optimize เงา (ป้องกัน Crash)")]
    [Tooltip("ถ้าระยะห่างระหว่างขอบน้อยกว่าค่านี้ จะตัดจุดทิ้ง (ยิ่งค่าเยอะ เงายิ่งหยาบ แต่สเปคยิ่งลื่น) แนะนำ: 0.1 ถึง 0.5")]
    public float pointTolerance = 0.2f;
    
    [Tooltip("จำกัดจุดสูงสุดต่อ 1 โซน ถ้าจุดเกินนี้จะข้ามการสร้างเพื่อป้องกัน Unity ค้าง")]
    public int maxVerticesPerZone = 300;

    [Space(10)]
    [Header("🚀 กดติ๊กถูกเพื่อสร้างเงา")]
    public bool generateShadows = false;

    private void OnValidate()
    {
        if (generateShadows)
        {
            generateShadows = false;
            GenerateShadowCasters();
        }
    }

    public void GenerateShadowCasters()
    {
        // 1. เคลียร์ของเก่าทิ้งให้เกลี้ยง
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i).name.StartsWith("ShadowCaster_"))
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        CompositeCollider2D compCollider = GetComponent<CompositeCollider2D>();
        int pathCount = compCollider.pathCount;

        FieldInfo shapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo shapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);

        int successCount = 0;

        for (int i = 0; i < pathCount; i++)
        {
            Vector2[] pathVertices = new Vector2[compCollider.GetPathPointCount(i)];
            compCollider.GetPath(i, pathVertices);

            // --- 2. อัลกอริทึมลดความซับซ้อนของขอบ (Decimation) เพื่อกัน Crash ---
            List<Vector3> simplifiedPath = new List<Vector3>();
            if (pathVertices.Length > 0)
            {
                simplifiedPath.Add(pathVertices[0]); // เก็บจุดแรกไว้เสมอ
            }

            for (int j = 1; j < pathVertices.Length; j++)
            {
                // ถ้าจุดถัดไป อยู่ใกล้กับจุดล่าสุดเกินไป (น้อยกว่า pointTolerance) ให้ข้ามจุดนั้นไปเลย!
                if (Vector2.Distance(simplifiedPath[simplifiedPath.Count - 1], pathVertices[j]) > pointTolerance)
                {
                    simplifiedPath.Add(pathVertices[j]);
                }
            }

            // --- 3. ระบบเซฟตี้ ป้องกันขอบเขตที่ใหญ่ระดับทวีป ---
            if (simplifiedPath.Count > maxVerticesPerZone)
            {
                Debug.LogWarning($"<color=orange>⚠️ โซนที่ {i} ใหญ่และซับซ้อนเกินไป! (มีถึง {simplifiedPath.Count} จุด) ข้ามการสร้างเงาโซนนี้เพื่อป้องกันแครช แนะนำให้หั่น Tilemap ชิ้นนี้แยกออกไปครับ</color>");
                continue; 
            }

            // 4. สร้าง Shadow Caster
            GameObject shadowObj = new GameObject("ShadowCaster_" + i);
            shadowObj.transform.parent = transform;
            shadowObj.transform.localPosition = Vector3.zero;
            shadowObj.transform.localRotation = Quaternion.identity;

            ShadowCaster2D shadowCaster = shadowObj.AddComponent<ShadowCaster2D>();
            shadowCaster.castsShadows = true;
            shadowCaster.selfShadows = false;

            // 5. สลับด้านจุด (Reverse Array) แก้ปัญหาเงาแหว่ง
            Vector3[] finalPath = new Vector3[simplifiedPath.Count];
            for (int j = 0; j < simplifiedPath.Count; j++)
            {
                int reverseIndex = simplifiedPath.Count - 1 - j;
                finalPath[j] = simplifiedPath[reverseIndex];
            }

            // ยัดค่ารูปทรงที่ Optimize แล้วเข้าไป
            shapePathField.SetValue(shadowCaster, finalPath);
            
            if (shapePathHashField != null)
            {
                shapePathHashField.SetValue(shadowCaster, Random.Range(int.MinValue, int.MaxValue));
            }

            shadowObj.SetActive(false);
            shadowObj.SetActive(true);
            
            successCount++;
        }

        Debug.Log($"<color=green>✔️ สร้างเงาสำเร็จ! รอดตายจากการ Crash ล้อมขอบเขตได้ทั้งหมด {successCount} จาก {pathCount} โซน</color>");
    }
}