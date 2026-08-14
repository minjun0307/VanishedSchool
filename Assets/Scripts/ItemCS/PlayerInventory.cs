using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 플레이어의 아이템 조작 담당 (Player 오브젝트에 부착)
// - F : 주울 수 있는 아이템이 근처에 있으면 획득, 없으면 장착한 아이템 사용
// 장착은 인벤토리 패널(B키 → 인벤토리)에서만 합니다.
// 범위 판정은 아이템 쪽의 Is Trigger 콜라이더로 하며,
// 플레이어에 Rigidbody2D가 있어서 트리거 진입/이탈 이벤트가 이쪽으로 들어옵니다.
public class PlayerInventory : MonoBehaviour
{
    public Text m_PromptText;   // "[F] OO 줍기" 안내 텍스트 (비워두면 표시 생략)
    public InvenP m_InvenPanel; // 인벤토리 패널 (열어둔 채로 주웠을 때 갱신용, 비워두면 생략)

    List<ItemPickup> m_Nearby = new List<ItemPickup>();   // 트리거 범위 안에 있는 아이템들
    PlayerMove m_Move;
    Cabinet m_NearCabinet;   // 상호작용 범위 안에 있는 캐비넷 (Cabinet이 알려줍니다)

    void Start()
    {
        m_Move = GetComponent<PlayerMove>();
        ShowPrompt("");
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        ItemPickup pickup;
        if (col.TryGetComponent<ItemPickup>(out pickup) && !m_Nearby.Contains(pickup))
            m_Nearby.Add(pickup);

        // 캐비넷은 판정 콜라이더가 자식(CabinetTrigger)에 있으므로 부모에서 찾습니다.
        // 같은 자식인 '들키는 범위(CaughtRange)'는 상호작용 범위가 아니므로 제외합니다.
        Cabinet cabinet = col.GetComponentInParent<Cabinet>();
        if (cabinet != null && !cabinet.IsCaughtZone(col))
            SetNearCabinet(cabinet, true);
    }

    void OnTriggerExit2D(Collider2D col)
    {
        ItemPickup pickup;
        if (col.TryGetComponent<ItemPickup>(out pickup))
            m_Nearby.Remove(pickup);

        Cabinet cabinet = col.GetComponentInParent<Cabinet>();
        if (cabinet != null && !cabinet.IsCaughtZone(col))
            SetNearCabinet(cabinet, false);
    }

    // 캐비넷이 상호작용 범위 진입/이탈을 알려줄 때 호출합니다.
    public void SetNearCabinet(Cabinet cabinet, bool near)
    {
        if (near)
            m_NearCabinet = cabinet;
        else if (m_NearCabinet == cabinet)
            m_NearCabinet = null;
    }

    void Update()
    {
        // 캐비넷에 숨어 있는 동안은 플레이어 조작이 꺼져 있으므로,
        // 아래 m_IsActive 검사보다 먼저 '나오기'를 처리해야 F키로 빠져나올 수 있습니다.
        if (Cabinet.Hiding != null)
        {
            ShowPrompt("[F] 캐비넷에서 나오기");
            if (Input.GetKeyDown(KeyCode.F))
                Cabinet.Hiding.Exit(m_Move);
            return;
        }

        // 페이드 전환/사망 등 조작 불가 상태에서는 아이템 조작도 막습니다.
        if (m_Move == null || !m_Move.m_IsActive)
        {
            ShowPrompt("");
            return;
        }

        // 캐비넷이 가장 우선, 그다음 주울 수 있는 아이템, 둘 다 없으면 장착 아이템 사용
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (m_NearCabinet != null)
            {
                m_NearCabinet.Hide(m_Move);
                if (Cabinet.Hiding != null)
                    return;   // 숨기에 성공했으면 안내 문구는 다음 프레임에 '나오기'로 바뀝니다
            }
            else
            {
                ItemPickup target = FindNearest();
                if (target != null)
                    TryPickup(target);
                else
                    UseEquipped();
            }
        }

        ShowPrompt(BuildPrompt());
    }

    // 지금 화면에 띄울 상호작용 안내 문구 (없으면 빈 문자열)
    string BuildPrompt()
    {
        if (m_NearCabinet != null)
            return "[F] 캐비넷에 숨기";

        ItemPickup nearest = FindNearest();
        if (nearest != null && nearest.Data != null)
            return "[F] " + nearest.Data.m_Name + " 줍기";

        return "";
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
        RefreshPanel();
    }

    // 장착한 아이템 사용 — 실제 처리는 AssetMgr.UseSlot에 모여 있습니다.
    void UseEquipped()
    {
        AssetMgr mgr = AssetMgr.Inst();
        mgr.UseSlot(mgr.EquippedIndex);
        RefreshPanel();
    }

    // B키 메뉴는 플레이어 이동을 막지 않으므로 인벤토리를 열어둔 채로 주울 수 있습니다.
    // 그 경우에도 목록이 바로 바뀌도록 열려 있을 때만 다시 그려줍니다.
    void RefreshPanel()
    {
        if (m_InvenPanel != null && m_InvenPanel.gameObject.activeInHierarchy)
            m_InvenPanel.Refresh();
    }

    // 안내 문구 표시 (빈 문자열을 넣으면 아무것도 보이지 않습니다)
    void ShowPrompt(string message)
    {
        if (m_PromptText == null)
            return;

        m_PromptText.text = message;
    }
}
