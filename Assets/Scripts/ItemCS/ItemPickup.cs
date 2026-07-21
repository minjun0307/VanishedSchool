using UnityEngine;

// 맵에 놓인 아이템 하나. 씬에 직접 배치한 아이템(testitem)과
// ItemSpawner가 풀에서 꺼내 쓰는 아이템 모두 이 스크립트를 사용합니다.
// 플레이어가 F로 주울 수 있으려면 Is Trigger가 켜진 콜라이더가 함께 있어야 합니다.
public class ItemPickup : MonoBehaviour
{
    public ItemData m_Data;   // 씬에 직접 배치한 경우 인스펙터에서 연결

    ItemSpawner m_Owner;      // 풀에서 꺼내진 경우에만 채워짐 (씬에 직접 배치한 것은 null)
    SpriteRenderer m_Renderer;

    public ItemData Data { get { return m_Data; } }

    void Awake()
    {
        m_Renderer = GetComponent<SpriteRenderer>();
    }

    // 풀에서 꺼낼 때 어떤 아이템인지 주입합니다.
    // 프리팹 하나로 모든 종류를 표현하기 위해 스프라이트도 여기서 교체합니다.
    public void Setup(ItemData data, ItemSpawner owner)
    {
        m_Data = data;
        m_Owner = owner;

        if (m_Renderer != null && data != null)
            m_Renderer.sprite = data.m_Icon;
    }

    // 플레이어가 획득했을 때 호출 — 풀에서 나온 것이면 풀로 되돌리고, 아니면 그냥 끕니다.
    public void Consume()
    {
        if (m_Owner != null)
            m_Owner.Release(this);
        else
            gameObject.SetActive(false);
    }
}
