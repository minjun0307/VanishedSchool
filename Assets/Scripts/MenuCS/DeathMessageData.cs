using UnityEngine;

// 사망 패널(DeathPanel)에 출력될 문구들을 보관하는 스크립터블 오브젝트.
// 에셋을 선택하면 인스펙터에서 문구를 추가/수정/삭제할 수 있습니다.
[CreateAssetMenu(fileName = "DeathMessageData", menuName = "Game/Death Message Data")]
public class DeathMessageData : ScriptableObject
{
    [TextArea(2, 5)]
    public string[] m_Messages;   // 사망 시 출력될 문구 목록 (에디터에서 관리)

    // 문구 중 하나를 랜덤으로 반환 (목록이 비어 있으면 빈 문자열)
    public string GetRandom()
    {
        if (m_Messages == null || m_Messages.Length == 0)
        {
            Debug.LogWarning("DeathMessageData: 등록된 사망 문구가 없습니다.");
            return "";
        }
        return m_Messages[Random.Range(0, m_Messages.Length)];
    }
}
