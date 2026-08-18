using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 소화기 연막을 만들고 관리하는 매니저 (씬에 하나).
/// 몬스터는 매 프레임 IsInFog로 "내가 지금 연막 안인가"를 물어보고, 안이면 감지/공격을 포기합니다.
///
/// 씬에 직접 배치하지 않아도 Ensure()가 필요할 때 자동으로 만들어 줍니다.
/// 반경·지속시간 등을 인스펙터에서 조절하고 싶으면 빈 게임오브젝트에 이 스크립트를 붙이세요.
/// </summary>
public class FogMgr : MonoBehaviour
{
    [Header("연막 설정")]
    [Tooltip("감지 차단이 걸리는 원의 반지름 (월드 단위). 몬스터 시야 반경보다 작아야 연막 밖에서는 정상 감지됩니다")]
    public float m_Radius = 2.5f;

    [Tooltip("연기를 계속 뿜는 시간(초). 이 시간이 지나면 뿜기를 멈추고 서서히 흩어집니다")]
    public float m_HoldTime = 3f;

    [Tooltip("뿜기를 멈춘 뒤 남은 연기가 다 흩어지기를 기다리는 시간(초). 이 시간이 끝나야 감지 차단이 풀립니다")]
    public float m_FadeTime = 1.5f;

    [Header("연막 이펙트")]
    [Tooltip("연기 이펙트 프리팹. 비워두면 Resources/fx/FireExtinguisherSmoke를 자동으로 불러옵니다")]
    public GameObject m_FogPrefab;

    [Tooltip("이펙트 크기 배율. 보이는 연기와 위의 판정 반지름이 안 맞으면 이 값으로 맞추세요")]
    public float m_EffectScale = 1f;

    [Header("렌더링 설정")]
    [Tooltip("연기를 그릴 소팅 레이어. 프리팹 기본값(Default)은 맨 아래라 맵 타일에 가려집니다")]
    public string m_SortingLayer = "Character";

    [Tooltip("소팅 순서. PlayerFog(32767)보다 하나 아래에 그려 캐릭터 위·시야 안개 아래에 놓습니다")]
    public int m_SortingOrder = 32766;

    [Header("대상")]
    [Tooltip("연막을 뿌릴 기준. 비워두면 PlayerMove가 붙은 오브젝트를 자동으로 찾습니다")]
    public Transform m_Player;

    // 프리팹을 인스펙터에서 연결하지 않았을 때 찾아볼 경로 (FogMgr가 자동 생성되는 경우가 있어 대비합니다)
    const string FogPrefabPath = "fx/FireExtinguisherSmoke";

    List<FogZone> m_Zones = new List<FogZone>();   // 만들어둔 연막들 (꺼진 것은 재사용)

    // ItemSpawner.Awake와 같은 방식으로 자기 자신을 GameMgr에 등록합니다.
    void Awake()
    {
        GameMgr.Inst().m_FogMgr = this;
    }

    // 씬에 FogMgr가 없으면 하나 만들어 돌려줍니다.
    // AssetMgr는 MonoBehaviour가 아니라 오브젝트를 직접 만들 수 없어서, 이 통로로 위임받습니다.
    public static FogMgr Ensure()
    {
        FogMgr mgr = GameMgr.Inst().m_FogMgr;
        if (mgr == null)
        {
            GameObject go = new GameObject("FogMgr");
            mgr = go.AddComponent<FogMgr>();   // AddComponent 즉시 Awake가 돌아 GameMgr에 등록됩니다
        }
        return mgr;
    }

    // 플레이어 발밑에 연막을 하나 뿌립니다 (소화기 사용).
    public void SpawnFogAtPlayer()
    {
        Transform target = GetPlayer();
        if (target == null)
        {
            Debug.LogWarning("FogMgr: 플레이어를 찾지 못해 연막을 만들 수 없습니다.", this);
            return;
        }

        SpawnFog(target.position);
    }

    public void SpawnFog(Vector3 pos)
    {
        FogZone zone = GetZone();

        // 이펙트를 이 오브젝트의 자식으로 붙일 것이므로 자리부터 잡아둡니다.
        zone.transform.position = new Vector3(pos.x, pos.y, 0f);
        zone.Activate(m_Radius, m_HoldTime, m_FadeTime, CreateEffect(zone.transform));
    }

    // 이 좌표가 켜져 있는 연막 원 안에 들어가 있는지 검사합니다.
    // 몬스터가 매 프레임 호출하므로 제곱근(Mathf.Sqrt) 없이 거리제곱끼리 비교합니다.
    public bool IsInFog(Vector2 pos)
    {
        for (int i = 0; i < m_Zones.Count; i++)
        {
            FogZone zone = m_Zones[i];
            if (zone == null || !zone.gameObject.activeSelf)
                continue;   // 꺼져 있는 = 이미 사라진 연막

            Vector2 diff = (Vector2)zone.transform.position - pos;
            if (diff.sqrMagnitude <= zone.Radius * zone.Radius)
                return true;
        }
        return false;
    }

    // 세이브를 불러올 때 화면에 남아 있는 연막을 모두 치웁니다 (연막은 저장하지 않으므로).
    public void ClearAll()
    {
        for (int i = 0; i < m_Zones.Count; i++)
        {
            if (m_Zones[i] != null)
                m_Zones[i].gameObject.SetActive(false);   // FogZone.OnDisable이 이펙트까지 정리합니다
        }
    }

    // 연기 이펙트를 하나 만들어 연막 자리에 붙입니다.
    // 프리팹이 꺼진 부모 밑에 만들어지므로, 연막이 켜지는 순간에 맞춰 재생이 시작됩니다.
    GameObject CreateEffect(Transform parent)
    {
        GameObject prefab = GetPrefab();
        if (prefab == null)
            return null;   // 이펙트가 없어도 감지 차단 자체는 동작합니다

        GameObject fx = Instantiate(prefab, parent);
        fx.transform.localPosition = Vector3.zero;
        fx.transform.localScale = Vector3.one * m_EffectScale;

        // 프리팹의 소팅 레이어는 Default(목록 맨 아래)라 그대로 두면 맵 타일 뒤에 숨습니다.
        // 파티클 렌더러를 모두 찾아 캐릭터 위에 그려지도록 다시 지정합니다.
        ParticleSystemRenderer[] renderers = fx.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = m_SortingLayer;
            renderers[i].sortingOrder = m_SortingOrder;
        }

        return fx;
    }

    // 인스펙터에 연결해두지 않았으면 Resources에서 찾아 기억해둡니다.
    GameObject GetPrefab()
    {
        if (m_FogPrefab == null)
        {
            m_FogPrefab = Resources.Load<GameObject>(FogPrefabPath);
            if (m_FogPrefab == null)
                Debug.LogWarning("FogMgr: 연막 이펙트 프리팹을 찾을 수 없습니다: Resources/" + FogPrefabPath, this);
        }
        return m_FogPrefab;
    }

    // 인스펙터에서 연결해두지 않았으면 씬에서 플레이어를 찾아 기억해둡니다.
    Transform GetPlayer()
    {
        if (m_Player == null)
        {
            PlayerMove player = FindFirstObjectByType<PlayerMove>();
            if (player != null)
                m_Player = player.transform;
        }
        return m_Player;
    }

    // 꺼져 있는 연막이 있으면 재사용하고, 없으면 새로 만듭니다 (ItemSpawner의 풀링과 같은 방식).
    FogZone GetZone()
    {
        for (int i = 0; i < m_Zones.Count; i++)
        {
            if (m_Zones[i] != null && !m_Zones[i].gameObject.activeSelf)
                return m_Zones[i];
        }

        FogZone zone = CreateZone();
        m_Zones.Add(zone);
        return zone;
    }

    FogZone CreateZone()
    {
        GameObject go = new GameObject("FogZone");
        go.transform.SetParent(transform, false);

        FogZone zone = go.AddComponent<FogZone>();
        go.SetActive(false);   // Activate에서 켜집니다
        return zone;
    }
}
