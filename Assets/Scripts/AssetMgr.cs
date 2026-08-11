using System.Collections.Generic;
using UnityEngine;

// 아이템/인벤토리를 관리하는 싱글톤 (GameMgr와 같은 패턴)
// 인벤토리는 고정 4칸이며, 그중 한 칸을 '장착'해서 F키로 사용합니다.
// 예) AssetMgr.Inst().AddItem(itemData);  AssetMgr.Inst().HasItem("Key")
public class AssetMgr
{
    static AssetMgr inst = new AssetMgr();

    public static AssetMgr Inst()
    {
        if (inst == null)
            inst = new AssetMgr();

        return inst;
    }

    public const int SlotCount = 4;   // 인벤토리 최대 칸 수

    // 세이브 파일에는 아이템의 m_Id 문자열만 기록되므로, 로드할 때 이 경로에서 에셋을 되찾습니다.
    // (ItemData 에셋은 Assets/Resources/Items/ 아래에 "파일명 = m_Id" 로 저장해야 합니다)
    const string ItemPath = "Items/";

    ItemData[] m_Slots = new ItemData[SlotCount];
    int m_EquippedIndex = 0;

    public int EquippedIndex { get { return m_EquippedIndex; } }

    // 빈 슬롯을 앞에서부터 찾아 넣고 그 칸 번호를 반환합니다. 4칸이 모두 차 있으면 -1.
    public int AddItem(ItemData item)
    {
        if (item == null)
            return -1;

        for (int i = 0; i < SlotCount; i++)
        {
            if (m_Slots[i] == null)
            {
                m_Slots[i] = item;
                return i;
            }
        }
        return -1;
    }

    public ItemData GetSlot(int index)
    {
        if (index < 0 || index >= SlotCount)
            return null;

        return m_Slots[index];
    }

    public ItemData GetEquipped()
    {
        return m_Slots[m_EquippedIndex];
    }

    public void Equip(int index)
    {
        if (index < 0 || index >= SlotCount)
            return;

        m_EquippedIndex = index;
    }

    public void ClearSlot(int index)
    {
        if (index < 0 || index >= SlotCount)
            return;

        m_Slots[index] = null;
    }

    // 슬롯의 아이템을 사용합니다 (F키와 인벤토리 패널 클릭 양쪽에서 호출).
    // 지금은 종류별 분기 자리만 만들어두고, 소모품만 실제로 칸을 비웁니다.
    public void UseSlot(int index)
    {
        ItemData data = GetSlot(index);
        if (data == null)
        {
            Debug.Log((index + 1) + "번 슬롯이 비어 있습니다");
            return;
        }

        switch (data.m_Type)
        {
            case ItemType.Key:
                Debug.Log("사용: " + data.m_Name + " (Key) — 잠긴 문 해제는 다음 단계");
                break;
            case ItemType.Tool:
                Debug.Log("사용: " + data.m_Name + " (Tool)");
                break;
            case ItemType.Consumable:
                Debug.Log("사용: " + data.m_Name + " (Consumable) — 슬롯 비움");
                ClearSlot(index);
                break;
        }
    }

    // 특정 아이템을 갖고 있는지 확인 (잠긴 방의 열쇠 판정 등에 사용)
    public bool HasItem(string itemId)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (m_Slots[i] != null && m_Slots[i].m_Id == itemId)
                return true;
        }
        return false;
    }

    // ── 세이브/로드 연동 (SaveFileMgr가 호출) ──
    // 항상 길이 4의 목록을 만들고, 빈 칸은 빈 문자열로 채워 칸 위치를 그대로 보존합니다.
    public List<string> GetInventoryForSave()
    {
        List<string> ids = new List<string>(SlotCount);
        for (int i = 0; i < SlotCount; i++)
            ids.Add(m_Slots[i] != null ? m_Slots[i].m_Id : "");

        return ids;
    }

    public void SetInventoryFromLoad(List<string> items)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            string id = (items != null && i < items.Count) ? items[i] : "";
            m_Slots[i] = string.IsNullOrEmpty(id) ? null : LoadItem(id);
        }
        m_EquippedIndex = 0;
    }

    // m_Id 문자열로 아이템 에셋을 되찾습니다 (ItemSpawner의 맵 아이템 복원에서도 사용).
    public ItemData LoadItem(string itemId)
    {
        ItemData data = Resources.Load<ItemData>(ItemPath + itemId);
        if (data == null)
            Debug.LogWarning("아이템 에셋을 찾을 수 없습니다: Resources/" + ItemPath + itemId);

        return data;
    }
}
