using System.Collections.Generic;
using UnityEngine;

// 특정 아이템이 맵에 동시에 몇 개까지 나올 수 있는지 정하는 규칙.
// 여기에 등록하지 않은 아이템은 개수 제한 없이 나옵니다.
[System.Serializable]
public class SpawnLimit
{
    public ItemData m_Item;
    public int m_MaxOnMap = 2;   // 맵에 동시에 존재할 수 있는 최대 개수
}

// 맵에 아이템을 랜덤으로 뿌리는 스포너.
// 오브젝트 풀링: 최대 개수(m_MaxSpawn)만큼 미리 만들어두고 껐다 켜서 재사용합니다.
// (플레이 도중 Instantiate/Destroy를 반복하지 않기 위함)
public class ItemSpawner : MonoBehaviour
{
    public GameObject m_PickupPrefab;      // ItemPickup이 붙은 프리팹 (1종류면 충분)
    public ItemData[] m_SpawnTable;        // 스폰 후보 아이템 목록
    public SpawnLimit[] m_SpawnLimits;     // 개수를 제한할 아이템만 등록 (예: 망치 2개, 구급상자 2개)
    public Transform m_SpawnPointsRoot;    // 이 오브젝트의 자식들이 스폰 후보 위치
    public int m_MinSpawn = 5;             // 최소 스폰 개수
    public int m_MaxSpawn = 8;             // 최대 스폰 개수

    Queue<ItemPickup> m_Pool = new Queue<ItemPickup>();

    // 세이브/로드 코드(SaveFileMgr)가 스포너를 찾을 수 있도록 자기 자신을 등록합니다.
    // MonsterScene이 GameMgr에 자기를 등록하는 것과 같은 방식이며,
    // Start보다 먼저 실행되는 Awake에서 등록해 순서 문제를 피합니다.
    void Awake()
    {
        GameMgr.Inst().m_ItemSpawner = this;
    }

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

        // 맵에 이미 나와 있는 아이템 개수를 먼저 세어둡니다.
        // 씬에 직접 배치해 둔 hamma/medic/book도 여기 포함되므로,
        // "이미 놓인 것까지 합쳐서 최대 몇 개"라는 제한이 그대로 지켜집니다.
        Dictionary<string, int> onMap = CountItemsOnMap();

        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            ItemData data = PickSpawnableItem(onMap);
            if (data == null)
                break;   // 모든 아이템이 제한에 걸려 더 놓을 것이 없음

            ItemPickup pickup = Get();
            pickup.Setup(data, this);
            pickup.transform.position = points[i].position;
            pickup.gameObject.SetActive(true);

            // 방금 놓은 것도 개수에 반영해야 다음 뽑기에서 제한이 걸립니다.
            onMap[data.m_Id] = onMap.ContainsKey(data.m_Id) ? onMap[data.m_Id] + 1 : 1;
            spawned++;
        }

        Debug.Log("아이템 " + spawned + "개 스폰");
    }

    // 지금 맵에 나와 있는 아이템을 종류(m_Id)별로 셉니다.
    // 씬에 직접 배치한 것도 함께 세기 위해 씬 전체에서 ItemPickup을 찾습니다.
    Dictionary<string, int> CountItemsOnMap()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();

        ItemPickup[] all = FindObjectsByType<ItemPickup>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < all.Length; i++)
        {
            // 꺼져 있는 것 = 이미 주웠거나 풀에서 대기 중 → 개수에 넣지 않음
            if (!all[i].gameObject.activeSelf || all[i].Data == null)
                continue;

            string id = all[i].Data.m_Id;
            counts[id] = counts.ContainsKey(id) ? counts[id] + 1 : 1;
        }

        return counts;
    }

    // 스폰 목록에서 아직 개수 제한에 걸리지 않은 아이템 하나를 랜덤으로 고릅니다.
    // 놓을 수 있는 것이 하나도 없으면 null을 돌려줍니다.
    ItemData PickSpawnableItem(Dictionary<string, int> onMap)
    {
        List<ItemData> candidates = new List<ItemData>();

        for (int i = 0; i < m_SpawnTable.Length; i++)
        {
            ItemData item = m_SpawnTable[i];
            if (item == null)
                continue;

            int max = GetMaxOnMap(item);
            if (max > 0)
            {
                int now = onMap.ContainsKey(item.m_Id) ? onMap[item.m_Id] : 0;
                if (now >= max)
                    continue;   // 이미 제한 개수만큼 나와 있으므로 후보에서 제외
            }

            candidates.Add(item);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    // m_SpawnLimits에 등록된 최대 개수를 찾습니다. 등록되지 않았으면 0 = 제한 없음.
    int GetMaxOnMap(ItemData item)
    {
        if (m_SpawnLimits == null)
            return 0;

        for (int i = 0; i < m_SpawnLimits.Length; i++)
        {
            if (m_SpawnLimits[i] != null && m_SpawnLimits[i].m_Item == item)
                return m_SpawnLimits[i].m_MaxOnMap;
        }

        return 0;
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

    // ── 세이브/로드 연동 (SaveFileMgr가 호출) ──

    // 지금 맵에 나와 있는(아직 안 주운) 아이템을 종류+좌표 목록으로 만듭니다.
    // 씬에 직접 배치한 아이템(hamma/medic/book)은 스포너의 자식이 아니므로
    // 자식만 훑으면 놓칩니다. 그래서 씬 전체에서 ItemPickup을 찾습니다.
    public List<MapItemSave> GetPickupsForSave()
    {
        List<MapItemSave> list = new List<MapItemSave>();

        ItemPickup[] all = FindObjectsByType<ItemPickup>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < all.Length; i++)
        {
            ItemPickup pickup = all[i];

            // 꺼져 있는 것 = 이미 주웠거나 풀에서 대기 중 → 저장하지 않음
            if (!pickup.gameObject.activeSelf || pickup.Data == null)
                continue;

            MapItemSave saved = new MapItemSave();
            saved.id = pickup.Data.m_Id;
            saved.x = pickup.transform.position.x;
            saved.y = pickup.transform.position.y;
            list.Add(saved);
        }

        return list;
    }

    // 맵을 저장된 상태로 되돌립니다.
    // (1) 지금 나와 있는 아이템을 모두 치우고 → (2) 저장된 목록대로 다시 놓습니다.
    // 다시 놓을 때는 전부 풀에서 꺼내 쓰므로, 씬에 직접 배치한 것인지
    // 스폰된 것인지 구분할 필요 없이 한 가지 방법으로 처리됩니다.
    public void RestorePickups(List<MapItemSave> saved)
    {
        ItemPickup[] all = FindObjectsByType<ItemPickup>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < all.Length; i++)
        {
            // Consume은 풀에서 나온 것이면 풀로 되돌리고, 아니면 그냥 끕니다.
            if (all[i].gameObject.activeSelf)
                all[i].Consume();
        }

        // 프리팹이 연결되지 않았으면 다시 놓을 수단이 없습니다 (경고는 Start에서 이미 출력).
        if (saved == null || m_PickupPrefab == null)
            return;

        int restored = 0;
        for (int i = 0; i < saved.Count; i++)
        {
            ItemData data = AssetMgr.Inst().LoadItem(saved[i].id);
            if (data == null)
                continue;   // 에셋을 못 찾은 경우 — 경고는 LoadItem이 출력

            ItemPickup pickup = Get();
            pickup.Setup(data, this);
            pickup.transform.position = new Vector3(
                saved[i].x, saved[i].y, pickup.transform.position.z);
            pickup.gameObject.SetActive(true);
            restored++;
        }

        Debug.Log("맵 아이템 " + restored + "개 복원");
    }
}
