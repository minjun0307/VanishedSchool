using UnityEngine;

// 아이템 종류 — F로 사용했을 때 어떤 효과로 분기할지 결정합니다.
// Evidence(증거)는 맨 뒤에 추가했습니다. 중간에 끼워 넣으면 기존 에셋에 저장된
// 숫자(Key=0, Tool=1, Consumable=2)가 밀려 종류가 통째로 바뀌기 때문입니다.
public enum ItemType { Key, Tool, Consumable, Evidence }

// 아이템을 사용했을 때 함께 발동하는 특수 효과.
// ItemType이 '소모 규칙'(쓰면 칸이 비는지)이라면, 이쪽은 '무슨 일이 일어나는지'입니다.
// 두 가지가 별개라서 나눠뒀습니다. (예: 소화기 = Consumable + Fog)
public enum ItemEffect { None, Fog }

// 아이템 한 종류의 정보를 담는 스크립터블 오브젝트 (DeathMessageData와 같은 방식).
// Create > Game > Item Data 로 에셋을 만들며,
// 반드시 Assets/Resources/Items/ 아래에 "파일명 = m_Id" 로 저장해야
// 세이브 파일에 기록된 문자열로 다시 찾아올 수 있습니다.
[CreateAssetMenu(fileName = "ItemData", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    public string m_Id;      // 세이브에 기록되는 고유 키 (에셋 파일명과 동일하게)
    public string m_Name;    // 화면에 표시할 이름
    public Sprite m_Icon;    // 인벤토리 아이콘 겸 맵에 놓였을 때의 스프라이트
    public ItemType m_Type;
    public ItemEffect m_Effect;   // 사용했을 때 발동할 특수 효과 (없으면 None)
    [TextArea(2, 4)] public string m_Desc;
}
