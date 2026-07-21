using UnityEngine;
using UnityEngine.UI;

// 인벤토리 패널 — 4칸의 아이콘/이름과 현재 장착 중인 슬롯을 보여줍니다.
// (B키 → 메뉴 → 인벤토리 버튼으로 Setting2가 이 패널을 켜줍니다)
public class InvenP : MonoBehaviour
{
    public Image[] m_SlotIcons;   // 슬롯 4개의 아이콘 이미지 (인스펙터에서 1~4번 순서대로 연결)
    public Text[] m_SlotNames;    // 슬롯 4개의 이름 텍스트
    public Color m_EquippedColor = Color.yellow;   // 장착 중인 슬롯의 이름 색
    public Color m_NormalColor = Color.white;      // 나머지 슬롯의 이름 색

    // 패널이 켜질 때마다 최신 인벤토리 내용으로 갱신합니다 (HudUI.OnEnable과 같은 방식)
    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        AssetMgr mgr = AssetMgr.Inst();

        for (int i = 0; i < AssetMgr.SlotCount; i++)
        {
            ItemData data = mgr.GetSlot(i);

            if (m_SlotIcons != null && i < m_SlotIcons.Length && m_SlotIcons[i] != null)
            {
                m_SlotIcons[i].sprite = data != null ? data.m_Icon : null;
                m_SlotIcons[i].enabled = data != null;   // 빈 칸은 아이콘을 숨김
            }

            if (m_SlotNames != null && i < m_SlotNames.Length && m_SlotNames[i] != null)
            {
                m_SlotNames[i].text = data != null ? data.m_Name : "";
                m_SlotNames[i].color = i == mgr.EquippedIndex ? m_EquippedColor : m_NormalColor;
            }
        }
    }
}
