using UnityEngine;
using UnityEngine.UI;

// 인벤토리 패널에 아이템 1개당 하나씩 생성되는 UI 프리팹 (SaveSlot과 같은 방식)
// 빈 칸은 아예 만들지 않으므로 목록 순서와 슬롯 번호가 서로 다를 수 있습니다.
// 그래서 자기가 담당하는 AssetMgr 슬롯 번호를 직접 들고 있어야 합니다.
public class InvenSlot : MonoBehaviour
{
    public Image m_Icon;       // 아이템 아이콘
    public Text m_NameText;    // 아이템 이름 (m_Name은 Unity 내장 이름과 겹쳐서 쓰지 않습니다)
    public Button m_Button;    // 슬롯 클릭용 버튼

    int m_SlotIndex;                  // 이 UI가 담당하는 AssetMgr 슬롯 번호 (0~3)
    System.Action<int> m_OnClick;     // 클릭 시 슬롯 번호를 넘겨줄 콜백 (InvenP가 지정)

    public void Initialize(int slotIndex, ItemData data, System.Action<int> onClick)
    {
        m_SlotIndex = slotIndex;
        m_OnClick = onClick;

        if (m_Icon != null)
            m_Icon.sprite = data.m_Icon;

        if (m_NameText != null)
            m_NameText.text = data.m_Name;

        if (m_Button != null)
            m_Button.onClick.AddListener(OnClickSlot);
    }

    // 장착 중인 슬롯을 다른 색으로 표시할 때 사용합니다.
    public void SetNameColor(Color color)
    {
        if (m_NameText != null)
            m_NameText.color = color;
    }

    void OnClickSlot()
    {
        if (m_OnClick != null)
            m_OnClick(m_SlotIndex);
    }
}
