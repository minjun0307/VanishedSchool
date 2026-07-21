using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 플레이어의 아이템 조작 담당 (Player 오브젝트에 부착)
// - 숫자키 1~4 : 인벤토리 슬롯 장착
// - F : 주울 수 있는 아이템이 근처에 있으면 획득, 없으면 장착한 아이템 사용
// 범위 판정은 아이템 쪽의 Is Trigger 콜라이더로 하며,
// 플레이어에 Rigidbody2D가 있어서 트리거 진입/이탈 이벤트가 이쪽으로 들어옵니다.
public class PlayerInventory : MonoBehaviour
{
    public Text m_PromptText;   // "[F] OO 줍기" 안내 텍스트 (비워두면 표시 생략)

    List<ItemPickup> m_Nearby = new List<ItemPickup>();   // 트리거 범위 안에 있는 아이템들
    PlayerMove m_Move;

    void Start()
    {
        m_Move = GetComponent<PlayerMove>();
        ShowPrompt(null);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        ItemPickup pickup;
        if (col.TryGetComponent<ItemPickup>(out pickup) && !m_Nearby.Contains(pickup))
            m_Nearby.Add(pickup);
    }

    void OnTriggerExit2D(Collider2D col)
    {
        ItemPickup pickup;
        if (col.TryGetComponent<ItemPickup>(out pickup))
            m_Nearby.Remove(pickup);
    }

    void Update()
    {
        // 페이드 전환/사망 등 조작 불가 상태에서는 아이템 조작도 막습니다.
        if (m_Move == null || !m_Move.m_IsActive)
        {
            ShowPrompt(null);
            return;
        }

        for (int i = 0; i < AssetMgr.SlotCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                AssetMgr.Inst().Equip(i);
                Debug.Log("장착 슬롯 변경: " + (i + 1) + "번");
            }
        }

        // 주울 수 있는 아이템이 있으면 줍기가 우선, 없으면 장착 아이템 사용
        if (Input.GetKeyDown(KeyCode.F))
        {
            ItemPickup target = FindNearest();
            if (target != null)
                TryPickup(target);
            else
                UseEquipped();
        }

        ShowPrompt(FindNearest());
    }

    // 근처 목록에서 가장 가까운 아이템을 찾습니다.
    // 이미 획득되어 꺼진(풀로 돌아간) 항목은 목록에서 함께 정리합니다.
    ItemPickup FindNearest()
    {
        ItemPickup nearest = null;
        float nearestDist = float.MaxValue;

        for (int i = m_Nearby.Count - 1; i >= 0; i--)
        {
            ItemPickup pickup = m_Nearby[i];
            if (pickup == null || !pickup.gameObject.activeInHierarchy)
            {
                m_Nearby.RemoveAt(i);
                continue;
            }

            float dist = Vector2.Distance(transform.position, pickup.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = pickup;
            }
        }
        return nearest;
    }

    void TryPickup(ItemPickup pickup)
    {
        ItemData data = pickup.Data;
        if (data == null)
        {
            Debug.LogWarning("ItemPickup에 ItemData가 연결되지 않았습니다.", pickup);
            return;
        }

        int slot = AssetMgr.Inst().AddItem(data);
        if (slot < 0)
        {
            Debug.Log("인벤토리가 가득 찼습니다 (" + AssetMgr.SlotCount + "칸)");
            return;
        }

        Debug.Log("획득: " + data.m_Name + " (슬롯 " + (slot + 1) + ")");
        m_Nearby.Remove(pickup);
        pickup.Consume();
    }

    // 장착한 아이템 사용 — 지금은 종류별 분기 자리만 만들어두고 로그만 남깁니다.
    void UseEquipped()
    {
        AssetMgr mgr = AssetMgr.Inst();
        ItemData data = mgr.GetEquipped();
        if (data == null)
        {
            Debug.Log((mgr.EquippedIndex + 1) + "번 슬롯이 비어 있습니다");
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
                mgr.ClearSlot(mgr.EquippedIndex);
                break;
        }
    }

    void ShowPrompt(ItemPickup target)
    {
        if (m_PromptText == null)
            return;

        bool show = target != null && target.Data != null;
        m_PromptText.text = show ? "[F] " + target.Data.m_Name + " 줍기" : "";
    }
}
