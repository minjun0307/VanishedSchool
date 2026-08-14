using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 인벤토리 패널 — 가지고 있는 아이템을 ItemSlotGroup의 Item1~Item4 칸에 하나씩 넣어 보여줍니다.
// 아이템 UI는 담당하는 칸(Item1~Item4)의 '자식'으로 생성되고, 그 칸이 비면 자식이 사라집니다.
// 아이템을 클릭하면 DecidePanel이 켜지면서 상황에 맞는 질문을 띄웁니다.
//  - 장착한 게 없을 때  → "OO을(를) 장착하시겠습니까?"  → btnEquip이 장착
//  - 이미 장착 중일 때  → "OO(으)로 교체하시겠습니까?"  → btnEquip이 두 칸의 아이템을 맞바꿈
// 이 패널은 장착만 담당합니다. 아이템을 실제로 쓰는 것은 장착한 뒤 게임 화면에서 F키입니다.
// (B키 → 메뉴 → 인벤토리 버튼으로 Setting2가 이 패널을 켜줍니다)
public class InvenP : MonoBehaviour
{
    [Header("슬롯 목록")]
    public InvenSlot m_SlotPrefab;                 // 아이템 1칸 프리팹 (SaveP의 슬롯 프리팹과 같은 방식)
    public Transform[] m_ItemSlots;                // ItemSlotGroup의 Item1~Item4 (배열 순서 = 인벤토리 칸 번호)

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

    // 가지고 있는 아이템을 다시 그립니다 (아이템을 주운 직후 PlayerInventory도 호출).
    // 최대 4개뿐이라 통째로 지웠다 다시 만드는 편이 증분 관리보다 단순합니다.
    // 흐름: ① 이전에 만든 아이템 UI를 모두 지움 → ② 0~3번 칸을 차례로 확인 →
    //       ③ 아이템이 든 칸에만 Item(번호+1) 밑에 아이템 UI를 자식으로 생성
    // 아이템을 사용해 칸이 비면 ②에서 지워진 뒤 ③에서 다시 만들지 않으므로 그 칸이 비어 보입니다.
    public void Refresh()
    {
        for (int i = 0; i < m_Spawned.Count; i++)
        {
            if (m_Spawned[i] == null)
                continue;

            // Destroy는 이번 프레임이 끝날 때 처리됩니다. 그래서 한 프레임에 Refresh가 두 번 불리면
            // 지워지기 직전의 UI가 칸에 남아 새 UI와 겹칩니다. 먼저 부모에서 떼어내 칸을 확실히 비웁니다.
            m_Spawned[i].transform.SetParent(null, false);
            Destroy(m_Spawned[i].gameObject);
        }
        m_Spawned.Clear();

        if (m_SlotPrefab == null || m_ItemSlots == null || m_ItemSlots.Length == 0)
        {
            Debug.LogWarning("InvenP: 슬롯 프리팹과 Item 슬롯들(Item1~Item4)을 인스펙터에서 연결해 주세요.", this);
            return;
        }

        AssetMgr mgr = AssetMgr.Inst();
        // 인벤토리 칸 수와 연결해둔 슬롯 수 중 작은 쪽까지만 돌아 배열 범위를 벗어나지 않게 합니다.
        int count = Mathf.Min(AssetMgr.SlotCount, m_ItemSlots.Length);
        for (int i = 0; i < count; i++)
        {
            Transform slotRoot = m_ItemSlots[i];
            if (slotRoot == null)
            {
                Debug.LogWarning("InvenP: " + (i + 1) + "번 Item 슬롯이 비어 있습니다.", this);
                continue;
            }

            ItemData data = mgr.GetSlot(i);
            if (data == null)
                continue;   // 빈 칸은 UI를 만들지 않습니다

            InvenSlot slot = Instantiate(m_SlotPrefab, slotRoot);
            FitToSlot(slot.transform as RectTransform);
            slot.Initialize(i, data, OnClickSlot);
            m_Spawned.Add(slot);
        }
    }

    // 프리팹에 저장된 원래 크기 대신, 부모가 된 Item 칸에 딱 맞게 늘려줍니다.
    // (네 모서리를 부모에 붙이고 여백을 0으로 만들어 칸 전체를 덮는 방식)
    void FitToSlot(RectTransform rt)
    {
        if (rt == null)
            return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    // 아이템을 클릭했을 때 — 결정 패널을 켜고 상황에 맞는 질문을 띄웁니다.
    // 이 패널에서는 '장착'만 합니다. 실제 사용은 장착한 뒤 게임 화면에서 F키로 합니다.
    void OnClickSlot(int slotIndex)
    {
        AssetMgr mgr = AssetMgr.Inst();
        ItemData data = mgr.GetSlot(slotIndex);
        if (data == null)
            return;

        m_PendingSlot = slotIndex;

        // 이미 다른 아이템을 장착 중이면 '교체', 아무것도 없으면 '장착'으로 문구를 바꿉니다.
        bool isSwap = mgr.IsEquipped && slotIndex != mgr.EquippedIndex;

        if (m_QuestionTxt != null)
            m_QuestionTxt.text = data.m_Name + (isSwap ? "(으)로 교체하시겠습니까?" : "을(를) 장착하시겠습니까?");

        if (m_btnEquipTxt != null)
            m_btnEquipTxt.text = isSwap ? "교체" : "장착";

        if (m_DecidePanel != null)
            m_DecidePanel.SetActive(true);
    }

    // btnEquip — 고른 아이템을 장착합니다.
    // 이미 장착 중이던 아이템이 있으면 두 칸의 아이템이 서로 자리를 맞바꿉니다.
    void OnClickEquip()
    {
        AssetMgr.Inst().EquipOrSwap(m_PendingSlot);   // 슬롯 번호가 -1이면 아무 일도 하지 않습니다

        CloseDecidePanel();
        Refresh();   // 맞바뀐 칸 위치를 화면에 반영
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
