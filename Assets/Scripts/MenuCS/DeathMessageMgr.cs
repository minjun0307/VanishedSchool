using UnityEngine;

// HudUI의 자식으로 배치되어 사망 문구 데이터(DeathMessageData)를 관리하는 오브젝트.
// DeathPanel 등에서 GetRandomMessage()로 문구를 받아 사용합니다.
public class DeathMessageMgr : MonoBehaviour
{
    public DeathMessageData m_Data;   // 인스펙터에서 연결하는 사망 문구 에셋

    // 사망 문구 중 하나를 랜덤으로 반환
    public string GetRandomMessage()
    {
        if (m_Data == null)
        {
            Debug.LogWarning("DeathMessageMgr: m_Data가 연결되지 않았습니다.");
            return "";
        }
        return m_Data.GetRandom();
    }
}
