using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 인벤토리 패널 — 가지고 있는 아이템만 프리팹으로 생성해 보여줍니다.
// 아이템을 클릭하면 DecidePanel이 켜지면서 타입에 맞는 질문을 띄웁니다.
//  - Key / Tool  → "OO을(를) 장착하시겠습니까?"  → btnEquip이 장착
//  - Consumable  → "OO을(를) 사용하시겠습니까?"  → btnEquip이 사용(칸 비움)
// (B키 → 메뉴 → 인벤토리 버튼으로 Setting2가 이 패널을 켜줍니다)
public class InvenP : MonoBehaviour
{
    [Header("슬롯 목록")]
    public InvenSlot m_SlotPrefab;                 // 아이템 1칸 프리팹 (SaveP의 슬롯 프리팹과 같은 방식)
    public Transform m_Content;                    // 슬롯이 생성될 부모
    public Color m_EquippedColor = Color.yellow;   // 장착 중인 슬롯의 이름 색
    public Color m_NormalColor = Color.white;      // 나머지 슬롯의 이름 색

    [Header("결정 패널 (DecidePanel)")]
    public GameObject m_DecidePanel;   // 아이템을 클릭했을 때 켜지는 패널 (평소 비활성)
    public Text m_QuestionTxt;         // "OO을(를) 장착하시겠습니까?" 질문 문구
    public Button m_btnEquip;          // 장착 (소모품이면 사용) 실행
    public Text m_btnEquipTxt;         // btnEquip에 적히는 글자 — 타입에 따라 "장착"/"사용"
    public Button m_btnCancel;         // 아무것도 하지 않고 패널만 닫기

    List<InvenSlot> m_Spawned = new List<InvenSlot>();   // 지금 화면에 만들어둔 슬롯 UI들
    int m_PendingSlot = -1;                              // 결정 패널에서 처리할 슬롯 번호

    // 이 패널은 꺼진 채로 시작하므로 켜지는 순간 Awake → OnEnable → Start 순서로 불립니다.
    // Start에 두면 첫 OnEnable의 Refresh()보다 늦게 실행되므로 Awake에서 연결합니다.
    void Awake()
    {
        if (m_btnEquip != null)
            m_btnEquip.onClick.AddListener(OnClickEquip);

        if (m_btnCancel != null)
            m_btnCancel.onClick.AddListener(OnClickCancel);

        CloseDecidePanel();
    }

    // 패널이 켜질 때마다 최신 인벤토리 내용으로 다시 그립니다.
    void OnEnable()
    {
        CloseDecidePanel();
        Refresh();
    }

    // 가지고 있는 아이템만 다시 그립니다 (아이템을 주운 직후 PlayerInventory도 호출).
    // 최대 4개뿐이라 통째로 지웠다 다시 만드는 편이 증분 관리보다 단순합니다.
    public void Refresh()
    {
        for (int i = 0; i < m_Spawned.Count; i++)
        {
            if (m_Spawned[i] != null)
                Destroy(m_Spawned[i].gameObject);
        }
        m_Spawned.Clear();

        if (m_SlotPrefab == null || m_Content == null)
        {
            Debug.LogWarning("InvenP: 슬롯 프리팹과 Content를 인스펙터에서 연결해 주세요.", this);
            return;
        }

        AssetMgr mgr = AssetMgr.Inst();
        for (int i = 0; i < AssetMgr.SlotCount; i++)
        {
            ItemData data = mgr.GetSlot(i);
            if (data == null)
                continue;   // 빈 칸은 UI를 만들지 않습니다

            InvenSlot slot = Instantiate(m_SlotPrefab, m_Content);
            slot.Initialize(i, data, OnClickSlot);
            slot.SetNameColor(i == mgr.EquippedIndex ? m_EquippedColor : m_NormalColor);
            m_Spawned.Add(slot);
        }
    }

    // 아이템을 클릭했을 때 — 결정 패널을 켜고 타입에 맞는 질문을 띄웁니다.
    void OnClickSlot(int slotIndex)
    {
        ItemData data = AssetMgr.Inst().GetSlot(slotIndex);
        if (data == null)
            return;

        m_PendingSlot = slotIndex;

        // 소모품은 "사용", 나머지(Key/Tool)는 "장착"으로 질문과 버튼 글자를 함께 바꿉니다.
        bool isUse = data.m_Type == ItemType.Consumable;

        if (m_QuestionTxt != null)
            m_QuestionTxt.text = data.m_Name + (isUse ? "을(를) 사용하시겠습니까?" : "을(를) 장착하시겠습니까?");

        if (m_btnEquipTxt != null)
            m_btnEquipTxt.text = isUse ? "사용" : "장착";

        if (m_DecidePanel != null)
            m_DecidePanel.SetActive(true);
    }

    // btnEquip — 소모품이면 사용, 나머지는 장착합니다.
    void OnClickEquip()
    {
        AssetMgr mgr = AssetMgr.Inst();
        ItemData data = mgr.GetSlot(m_PendingSlot);   // 슬롯 번호가 -1이면 null이 돌아옵니다
        if (data != null)
        {
            if (data.m_Type == ItemType.Consumable)
                mgr.UseSlot(m_PendingSlot);   // 효과 적용 후 칸을 비웁니다
            else
                mgr.Equip(m_PendingSlot);     // 1~4 숫자키 장착과 같은 처리
        }

        CloseDecidePanel();
        Refresh();   // 장착 색상 변경 / 사용한 아이템 제거를 화면에 반영
    }

    // Cancel — 아무것도 하지 않고 결정 패널만 닫습니다.
    void OnClickCancel()
    {
        CloseDecidePanel();
    }

    void CloseDecidePanel()
    {
        m_PendingSlot = -1;

        if (m_DecidePanel != null)
            m_DecidePanel.SetActive(false);
    }
}
