using UnityEngine;

// 상호작용 요소 한 종류의 정보를 담는 스크립터블 오브젝트 (ItemData와 같은 방식).
// Create > Game > Interact Data 로 에셋을 만들고,
// 씬 오브젝트에 붙인 Interactable 컴포넌트에 연결해서 씁니다.
//
// 책상, 사물함, 칠판처럼 "F를 누르면 문구만 뜨는" 요소들을
// 스크립트를 새로 만들지 않고 에셋만 추가해서 늘릴 수 있습니다.
[CreateAssetMenu(fileName = "InteractData", menuName = "Game/Interact Data")]
public class InteractData : ScriptableObject
{
    public string m_Name;                        // 요소 이름 (책상, 사물함 … 로그·구분용)

    [Header("가까이 갔을 때 뜨는 안내")]
    public string m_Prompt = "[F] 조사하기";      // 문구 전체를 그대로 씁니다 (매 프레임 문자열을 이어붙이지 않기 위함)

    [Header("F를 눌렀을 때 뜨는 메시지")]
    [TextArea(2, 4)] public string m_Message;    // 상호작용 결과로 화면에 뜰 문구
    public float m_MessageTime = 3f;             // 그 문구가 화면에 머무는 시간 (초)
}
