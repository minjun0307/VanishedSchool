using UnityEngine;

// 맵에 놓인 상호작용 요소 하나 (책상, 사물함, 칠판 …).
// 오브젝트에 이 스크립트를 붙이고 m_Data에 InteractData 에셋을 연결하면,
// 플레이어가 다가갔을 때 안내 문구가 뜨고 F를 누르면 메시지가 출력됩니다.
//
// 플레이어가 범위를 인식하려면 Is Trigger가 켜진 콜라이더가 함께 있어야 합니다.
// (콜라이더를 자식 오브젝트에 두어도 부모의 이 스크립트를 찾아냅니다 — PlayerInventory 참고)
public class Interactable : MonoBehaviour
{
    public InteractData m_Data;   // 인스펙터에서 연결하는 상호작용 데이터 에셋

    public InteractData Data { get { return m_Data; } }
}
