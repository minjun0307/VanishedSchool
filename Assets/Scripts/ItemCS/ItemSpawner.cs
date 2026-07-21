using System.Collections.Generic;
using UnityEngine;

// 맵에 아이템을 랜덤으로 뿌리는 스포너.
// 오브젝트 풀링: 최대 개수(m_MaxSpawn)만큼 미리 만들어두고 껐다 켜서 재사용합니다.
// (플레이 도중 Instantiate/Destroy를 반복하지 않기 위함)
public class ItemSpawner : MonoBehaviour
{
    public GameObject m_PickupPrefab;      // ItemPickup이 붙은 프리팹 (1종류면 충분)
    public ItemData[] m_SpawnTable;        // 스폰 후보 아이템 목록
    public Transform m_SpawnPointsRoot;    // 이 오브젝트의 자식들이 스폰 후보 위치
    public int m_MinSpawn = 5;             // 최소 스폰 개수
    public int m_MaxSpawn = 8;             // 최대 스폰 개수

    Queue<ItemPickup> m_Pool = new Queue<ItemPickup>();

    void Start()
    {
        if (m_PickupPrefab == null || m_SpawnPointsRoot == null ||
            m_SpawnTable == null || m_SpawnTable.Length == 0)
        {
            Debug.LogWarning("ItemSpawner: 프리팹/스폰 포인트/아이템 목록을 인스펙터에서 연결해 주세요.", this);
            return;
        }

        for (int i = 0; i < m_MaxSpawn; i++)
            m_Pool.Enqueue(CreatePooled());

        SpawnRandom();
    }

    // 풀에 넣어둘 아이템 오브젝트를 하나 만듭니다 (꺼진 상태로 대기).
    ItemPickup CreatePooled()
    {
        GameObject go = Instantiate(m_PickupPrefab, transform);
        go.SetActive(false);
        return go.GetComponent<ItemPickup>();
    }

    // 스폰 포인트 중 랜덤한 곳에 m_MinSpawn ~ m_MaxSpawn 개의 아이템을 배치합니다.
    void SpawnRandom()
    {
        List<Transform> points = new List<Transform>();
        foreach (Transform point in m_SpawnPointsRoot)
            points.Add(point);

        // Random.Range(int, int)는 max를 포함하지 않으므로 +1 해야 m_MaxSpawn개까지 나옵니다.
        int count = Random.Range(m_MinSpawn, m_MaxSpawn + 1);
        if (count > points.Count)
        {
            Debug.LogWarning("ItemSpawner: 스폰 포인트가 " + points.Count + "개뿐이라 "
                             + count + "개를 다 놓을 수 없습니다.", this);
            count = points.Count;
        }

        // 포인트 목록을 섞은 뒤 앞에서 count개만 사용 → 같은 자리에 두 번 놓이지 않음
        Shuffle(points);

        for (int i = 0; i < count; i++)
        {
            ItemPickup pickup = Get();
            pickup.Setup(m_SpawnTable[Random.Range(0, m_SpawnTable.Length)], this);
            pickup.transform.position = points[i].position;
            pickup.gameObject.SetActive(true);
        }

        Debug.Log("아이템 " + count + "개 스폰");
    }

    // 피셔-예이츠 셔플 — 뒤에서부터 앞쪽의 임의 원소와 자리를 바꿉니다.
    void Shuffle(List<Transform> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Transform tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    ItemPickup Get()
    {
        // 동시에 존재하는 아이템은 m_MaxSpawn개를 넘지 않으므로 보통 비지 않지만,
        // 모자라면 하나 더 만들어 씁니다.
        return m_Pool.Count > 0 ? m_Pool.Dequeue() : CreatePooled();
    }

    // 아이템을 획득했을 때 ItemPickup이 호출 — 파괴하지 않고 풀로 되돌립니다.
    public void Release(ItemPickup pickup)
    {
        pickup.gameObject.SetActive(false);
        m_Pool.Enqueue(pickup);
    }
}
