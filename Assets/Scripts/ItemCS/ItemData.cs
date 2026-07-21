using UnityEngine;

// 아이템 종류 — F로 사용했을 때 어떤 효과로 분기할지 결정합니다.
public enum ItemType { Key, Tool, Consumable }

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
    [TextArea(2, 4)] public string m_Desc;
}
