using System.Collections.Generic;
using UnityEngine;

// 플레이어의 아이템 조작 담당 (Player 오브젝트에 부착)
// - F   : 캐비넷 > 상호작용물(Interactable) > 아이템 줍기 > 장착 아이템 사용 순으로 처리
// - Q E : 근처에 상호작용물이 여러 개일 때 어느 것과 상호작용할지 고릅니다
//         (이동에 쓰는 화살표/AD와 겹치지 않도록 Q = 왼쪽, E = 오른쪽)
// 장착은 인벤토리 패널(B키 → 인벤토리)에서만 합니다.
// 범위 판정은 아이템 쪽의 Is Trigger 콜라이더로 하며,
// 플레이어에 Rigidbody2D가 있어서 트리거 진입/이탈 이벤트가 이쪽으로 들어옵니다.
public class PlayerInventory : MonoBehaviour
{
    public InvenP m_InvenPanel; // 인벤토리 패널 (열어둔 채로 주웠을 때 갱신용, 비워두면 생략)

    List<ItemPickup> m_Nearby = new List<ItemPickup>();   // 트리거 범위 안에 있는 아이템들
    List<Interactable> m_NearbyInteract = new List<Interactable>();   // 트리거 범위 안에 있는 상호작용물들
    PlayerMove m_Move;
    Cabinet m_NearCabinet;   // 상호작용 범위 안에 있는 캐비넷 (Cabinet이 알려줍니다)

    string m_Message;        // F로 상호작용해서 지금 화면에 띄우고 있는 메시지
    float m_MessageTimer;    // 그 메시지가 사라지기까지 남은 시간 (0 이하면 평소 안내 문구로 돌아감)

    Interactable m_Selected;   // Q/E로 고른 상호작용 대상 (기본값은 가장 가까운 것)
    HudUI m_Hud;               // 안내 문구를 띄울 HUD (Canvas > interactText를 들고 있습니다)

    // 안내 문구는 선택이나 상황이 바뀔 때만 새로 만들어 재사용합니다.
    // (매 프레임 문자열을 이어붙이면 쓰레기 메모리가 쌓여 프레임이 끊깁니다)
    string m_InteractPrompt;             // 만들어 둔 안내 문구
    Interactable m_InteractPromptFor;    // 그 문구를 만든 대상
    bool m_InteractPromptMulti;          // 그때 근처에 여러 개였는지 (화살표 표시 여부)

    void Start()
    {
        m_Move = GetComponent<PlayerMove>();
        m_Hud = FindHud();
        ShowPrompt("");
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        ItemPickup pickup;
        if (col.TryGetComponent<ItemPickup>(out pickup) && !m_Nearby.Contains(pickup))
            m_Nearby.Add(pickup);

        // 상호작용물은 콜라이더를 자식에 둘 수도 있으므로 부모까지 거슬러 찾습니다.
        Interactable interact = col.GetComponentInParent<Interactable>();
        if (interact != null && !m_NearbyInteract.Contains(interact))
            m_NearbyInteract.Add(interact);

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

        Interactable interact = col.GetComponentInParent<Interactable>();
        if (interact != null)
        {
            m_NearbyInteract.Remove(interact);
            // 고르고 있던 대상이 범위를 벗어났으면 선택을 비워, 다음 프레임에 가장 가까운 것으로 다시 잡습니다.
            if (interact == m_Selected)
                m_Selected = null;
        }

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
        // 상호작용 메시지가 떠 있으면 남은 시간을 줄입니다.
        // (아래 어느 경로로 빠져나가든 시간이 흐르도록 맨 위에서 처리합니다)
        if (m_MessageTimer > 0f)
            m_MessageTimer -= Time.deltaTime;

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

        // Q/E로 어떤 상호작용물과 상호작용할지 먼저 정합니다.
        UpdateInteractSelection();

        // 캐비넷 > 상호작용물 > 주울 수 있는 아이템 > 장착 아이템 사용 순으로 처리
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
                if (m_Selected != null)
                {
                    DoInteract(m_Selected);
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
        }

        // 상호작용 메시지가 살아 있는 동안에는 그 메시지를, 아니면 평소 안내 문구를 보여줍니다.
        // (메시지 중에는 BuildPrompt를 아예 호출하지 않으므로 가까운 대상 탐색도 건너뜁니다)
        ShowPrompt(m_MessageTimer > 0f ? m_Message : BuildPrompt());
    }

    // 지금 상호작용할 대상을 정합니다.
    // 선택이 비었으면 가장 가까운 것을 기본으로 잡고, Q/E를 누르면 그 방향의 다음 대상으로 옮깁니다.
    void UpdateInteractSelection()
    {
        // 주워지거나 꺼진 대상을 붙잡고 있지 않도록 확인합니다. (범위 이탈은 OnTriggerExit2D가 비워줍니다)
        if (m_Selected != null && !m_Selected.gameObject.activeInHierarchy)
            m_Selected = null;

        if (m_Selected == null)
            m_Selected = FindNearestInteract();

        if (m_Selected == null)
            return;

        // Q/E는 눌린 프레임에만 처리하므로 아래 탐색은 평소에 돌지 않습니다.
        if (Input.GetKeyDown(KeyCode.Q))
            SelectSide(-1);   // 왼쪽 대상으로
        else if (Input.GetKeyDown(KeyCode.E))
            SelectSide(1);    // 오른쪽 대상으로
    }

    // 지금 고른 대상에서 dir 방향(-1 = 왼쪽, +1 = 오른쪽)으로 가장 가까운 상호작용물로 선택을 옮깁니다.
    // 그 방향에 아무것도 없으면 선택을 그대로 둡니다. (끝에서 반대편으로 넘어가지 않습니다)
    void SelectSide(int dir)
    {
        float baseX = m_Selected.transform.position.x;
        Interactable best = null;
        float bestGap = float.MaxValue;

        for (int i = 0; i < m_NearbyInteract.Count; i++)
        {
            Interactable interact = m_NearbyInteract[i];
            if (interact == null || interact == m_Selected || !interact.gameObject.activeInHierarchy)
                continue;

            // dir을 곱해두면 왼쪽/오른쪽을 같은 식으로 비교할 수 있습니다. (그 방향이면 양수)
            float gap = (interact.transform.position.x - baseX) * dir;
            if (gap > 0f && gap < bestGap)
            {
                bestGap = gap;
                best = interact;
            }
        }

        if (best != null)
            m_Selected = best;
    }

    // 상호작용 실행 — 데이터에 적힌 메시지를 정해진 시간만큼 화면에 띄웁니다.
    void DoInteract(Interactable interact)
    {
        InteractData data = interact.Data;
        if (data == null)
        {
            Debug.LogWarning("Interactable에 InteractData가 연결되지 않았습니다.", interact);
            return;
        }

        m_Message = data.m_Message;
        // 메시지를 비워둔 데이터라면 안내 문구를 괜히 가리지 않도록 표시하지 않습니다.
        m_MessageTimer = string.IsNullOrEmpty(m_Message) ? 0f : data.m_MessageTime;
    }

    // 지금 화면에 띄울 상호작용 안내 문구 (없으면 빈 문자열)
    string BuildPrompt()
    {
        if (m_NearCabinet != null)
            return "[F] 캐비넷에 숨기";

        if (m_Selected != null && m_Selected.Data != null)
            return GetInteractPrompt(m_NearbyInteract.Count > 1);

        ItemPickup nearest = FindNearest();
        if (nearest != null && nearest.Data != null)
            return "[F] " + nearest.Data.m_Name + " 줍기";

        return "";
    }

    // 고른 상호작용물의 안내 문구를 돌려줍니다.
    // 근처에 여러 개일 때만 Q/E로 고를 수 있다는 뜻으로 "◀Q  E▶"를 붙입니다.
    // 선택이나 개수가 바뀔 때만 문자열을 새로 만들고, 평소에는 만들어 둔 것을 그대로 씁니다.
    string GetInteractPrompt(bool multi)
    {
        if (m_InteractPromptFor != m_Selected || m_InteractPromptMulti != multi)
        {
            m_InteractPromptFor = m_Selected;
            m_InteractPromptMulti = multi;

            string prompt = m_Selected.Data.m_Prompt;
            m_InteractPrompt = multi ? "◀Q " + prompt + " E▶" : prompt;
        }

        return m_InteractPrompt;
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

    // 근처 목록에서 가장 가까운 상호작용물을 찾습니다. (꺼진 항목은 함께 정리)
    // 거리 '비교'만 하면 되므로, 제곱근을 계산하는 Distance 대신 제곱거리(sqrMagnitude)로 비교합니다.
    Interactable FindNearestInteract()
    {
        Interactable nearest = null;
        float nearestSqr = float.MaxValue;
        Vector2 myPos = transform.position;

        for (int i = m_NearbyInteract.Count - 1; i >= 0; i--)
        {
            Interactable interact = m_NearbyInteract[i];
            if (interact == null || !interact.gameObject.activeInHierarchy)
            {
                m_NearbyInteract.RemoveAt(i);
                continue;
            }

            float sqr = ((Vector2)interact.transform.position - myPos).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = interact;
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

    // 안내 문구 표시 — 실제 출력은 HudUI가 들고 있는 Canvas > interactText가 담당합니다.
    // (빈 문자열을 넣으면 아무것도 보이지 않습니다)
    void ShowPrompt(string message)
    {
        if (m_Hud == null)
            m_Hud = FindHud();   // Start 시점에 아직 준비 전이었을 수 있어 한 번 더 시도합니다

        if (m_Hud == null)
            return;

        m_Hud.ShowInteract(message);
    }

    // GameScene에 연결된 HudUI를 찾아옵니다. (씬 구성이 덜 됐으면 null)
    HudUI FindHud()
    {
        GameScene scene = GameMgr.Inst().m_GameScene;
        return scene != null ? scene.m_HudUI : null;
    }
}
