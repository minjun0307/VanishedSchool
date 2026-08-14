using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering.Universal;   // Light2D (Spot Light 2D)

// 배회 실패 시 텔레포트할 룸 Spot 정보 (에디터에서 배열로 등록)
[System.Serializable]
public class RoomSpotInfo
{
    public Transform m_Spot;   // 룸의 자식 Spot
    public int m_Floor = 1;    // 그 룸이 속한 층 (1~3) — 텔레포트 후 배회 반경 결정용
}

public class MonsterMoves : MonoBehaviour
{
    public Transform player;         // 쫓아갈 플레이어의 Transform
    public float moveSpeed = 3f;     // 현재 이동 속도 (상태에 따라 patrolSpeed/chaseSpeed가 대입됨)

    [Header("State Speed Settings")]
    public float patrolSpeed = 3f;   // 배회(순찰) 상태 속도
    public float chaseSpeed = 5.5f;  // 추격 상태 속도 — 플레이어를 감지하면 이 속도로 빨라짐

    [Header("Patrol Range Settings (층별 배회 반경)")]
    public float floor1MinX = 2f;
    public float floor1MaxX = 18f;
    public float floor2MinX = 2f;
    public float floor2MaxX = 25f;
    public float floor3MinX = 2f;
    public float floor3MaxX = 22f;

    [Header("Floor Y Positions (층 이동 시 사용할 각 층의 y 좌표)")]
    public float floor1Y;
    public float floor2Y;
    public float floor3Y;

    [Header("Current Floor (현재 층 — 하나만 켜세요)")]
    public bool m_OnFloor1 = false;
    public bool m_OnFloor2 = true;   // 기본: 2층에서 배회 시작
    public bool m_OnFloor3 = false;

    [Header("Stair Spots (계단 층이동 도착 지점 — 각 층 계단의 Spot 자식을 연결)")]
    public Transform m_Floor1StairSpot;   // 1층 도착 Spot (예: F1SpotD)
    public Transform m_Floor2StairSpot;   // 2층 도착 Spot (예: F2Spot)
    public Transform m_Floor3StairSpot;   // 3층 도착 Spot (예: F3Spot)

    [Header("Room Teleport (배회를 다 돌아도 감지 실패 시 랜덤 룸 Spot으로 텔레포트)")]
    [Tooltip("룸들의 자식 Spot만 넣어주세요. 계단 Spot은 넣어도 자동으로 제외됩니다.")]
    public RoomSpotInfo[] m_RoomSpots;
    public int m_PatrolFailLimit = 3;     // 몇 개 층을 배회하고도 감지 실패하면 텔레포트할지

    [Header("Attack Settings (공격)")]
    public int m_DamageMin = 35;          // 공격 데미지 최소값 (정수)
    public int m_DamageMax = 45;          // 공격 데미지 최대값 (정수)
    public float m_HitCooldownTime = 2f;  // 공격 후: 플레이어 통과 허용 + 기본 속도 + 층이동 금지 시간

    [Header("Search Settings (캐비넷 등 특정 지점 수색)")]
    [Tooltip("수색 지점에 도착한 뒤 그 자리에서 두리번거리는 시간(초)입니다.")]
    public float m_SearchLingerTime = 2f;
    [Tooltip("벽 등에 막혀 수색 지점에 닿지 못할 때, 이 시간(초)이 지나면 수색을 포기하고 배회로 돌아갑니다.")]
    public float m_SearchTimeLimit = 20f;

    [Header("Radar Collider Settings")]
    [Tooltip("부채꼴 등 시야 범위 모양의 콜라이더(Is Trigger)가 있는 자식 게임오브젝트를 연결해주세요.")]
    public Transform visionCone;

    [Header("Vision Direction Settings")]
    [Tooltip("왼쪽으로 걸을 때 시야(스포트라이트)의 로컬 x 위치입니다. y는 처음 배치된 높이로 고정됩니다.")]
    public float visionLeftX = 0.14f;
    [Tooltip("오른쪽으로 걸을 때 시야(스포트라이트)의 로컬 x 위치입니다. y는 처음 배치된 높이로 고정됩니다.")]
    public float visionRightX = 0.08f;
    [Tooltip("시야가 이동 방향으로 돌아가는 회전 속도(도/초)입니다. 0 이하로 설정하면 즉시 회전합니다.")]
    public float visionRotateSpeed = 360f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator m_Animator;
    private float m_visionLocalY;   // 시야(스포트라이트/콜라이더)의 초기 로컬 y — 항상 이 높이로 고정
    private bool m_movingToMax;     // 배회 단계: false = 최소 x로 이동 중, true = 최대 x로 이동 중
    private const float ArriveThreshold = 0.1f;   // 목표 x 도착 판정 거리

    public Transform m_spotLight;
    private Light2D m_SpotLight2D;   // m_spotLight에 붙어 있는 Spot Light 2D — 빛의 반경/각도를 시야 범위로 사용
    public bool m_Patrol = false;
    public bool m_IsActive = false;
    public bool m_Chasing = false;
    public bool m_Alert = false;   // 경계 상태 — 플레이어를 놓친 자리에서 잠시 멈춰 있습니다
    public GameObject m_Gameobj;
    public bool m_PlayerMissed = false;

    private MonsterFSM m_monsterFSM;
    private int m_PatrolFailCount;      // 감지 실패한 층 배회 횟수 (감지 성공/텔레포트 시 리셋)
    private bool m_HitCooldown;         // 공격 후 2초 동안 true (기본 속도, 층이동 금지, 플레이어 통과)
    private Collider2D m_BodyCollider;  // 몬스터 몸통 콜라이더 (플레이어와의 충돌 무시용)
    private PlayerMove m_PlayerMove;    // 플레이어가 현재 몇 층에 있는지 확인용
    private const float StairArriveThreshold = 0.5f;   // 추격 중 계단 Spot 도착 판정 거리

    // ── 수색(캐비넷 등 특정 지점으로 가보기) 상태 ──
    private bool m_Searching;           // 수색 중이면 배회 대신 수색 지점으로 이동
    private Vector2 m_SearchPos;        // 수색할 좌표
    private int m_SearchFloor;          // 그 좌표가 있는 층
    private bool m_LeaveAfterSearch;    // 수색을 마친 뒤 다른 층으로 떠날지
    private float m_SearchLingerTimer;  // 도착 후 머문 시간
    private float m_SearchElapsed;      // 수색을 시작한 뒤 흐른 시간 (제한 시간 확인용)

    void Awake()
    {
        m_Animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        m_BodyCollider = GetComponent<Collider2D>();
        if (m_spotLight != null)
        {
            m_SpotLight2D = m_spotLight.GetComponent<Light2D>();
            m_visionLocalY = m_spotLight.localPosition.y;
        }
    }

    void Start()
    {
        GameMgr.Inst().m_MonsterScene.m_MonsterMoves = this;
        m_monsterFSM = GameMgr.Inst().m_MonsterScene.m_MonsterFSM;

        if (GameMgr.Inst().m_GameScene != null && GameMgr.Inst().m_GameScene.m_GameUI != null)
            m_PlayerMove = GameMgr.Inst().m_GameScene.m_GameUI.m_Player;
    }

    // ── FSM 상태 진입 시 MonsterScene의 콜백에서 호출됩니다 ──
    public void OnPatrol()
    {
        m_Patrol = true;
        m_Chasing = false;
        m_Alert = false;
        m_PlayerMissed = false;   // 다시 배회로 돌아왔으니 '놓침' 표시를 지웁니다
    }

    public void OnChase()
    {
        m_Chasing = true;
        m_Patrol = false;
        m_Alert = false;
        m_PlayerMissed = false;
        m_PatrolFailCount = 0;   // 플레이어를 감지했으므로 배회 실패 누적 리셋
        m_Searching = false;     // 플레이어를 다시 찾았으니 수색은 필요 없음
    }

    // 플레이어를 놓쳐 경계 상태에 들어갈 때 — 추격을 멈추고 그 자리에 섭니다.
    // 일정 시간 뒤 배회로 되돌리는 타이머는 MonsterScene이 돌립니다.
    public void OnAlert()
    {
        m_Chasing = false;
        m_Patrol = false;
        m_Alert = true;
        movement = Vector2.zero;
    }

    // Update에서는 사용자 입력이나 방향, Transform 계산 등의 '논리 연산'만 처리해야 부드럽습니다.
    void Update()
    {
        if (!m_IsActive)
            return;

        if (m_Chasing)
        {
            // 추격 중 소화기 연막 안으로 들어가면 눈이 가려져 플레이어를 놓칩니다.
            // (여기서 return하면 아래 애니메이션 갱신을 건너뛰어 달리는 동작이 그대로 굳으므로
            //  멈추기만 하고 아래로 계속 흘려보냅니다)
            if (IsBlindedByFog())
            {
                movement = Vector2.zero;
                Missed();
            }
            else
            {
                // 추격 상태: 빨라진 속도로 플레이어를 향해 달려갑니다.
                // (공격 직후 2초 동안은 기본 이동 속도로 제한)
                moveSpeed = m_HitCooldown ? patrolSpeed : chaseSpeed;

                // 플레이어가 계단으로 다른 층에 올라/내려갔다면, 몬스터도 계단을 타고 따라갑니다.
                int playerFloor = GetPlayerFloor();
                if (playerFloor == GetCurrentFloor() || !MoveToFloorViaStair(playerFloor))
                    CalculateDirection();
            }
        }
        else if (m_Alert)
        {
            // 경계 상태: 플레이어를 놓친 자리에서 제자리에 멈춰 있습니다.
            // MonsterScene이 m_AlertTime초 뒤에 배회 상태로 되돌려 줍니다.
            movement = Vector2.zero;
        }
        else
        {
            // 기본(배회) 상태: 층을 돌아다니며 레이더로 플레이어를 찾습니다.
            moveSpeed = patrolSpeed;
            Patrol();
            DetectPlayer();
        }

        // 이동 방향에 맞춰 시야(스포트라이트 + 콜라이더)를 회전/전진시킵니다.
        UpdateVisionDirection();
        UpdateAnimation();
    }

    public void DetectPlayer()
    {
        // spotlight(Spot Light 2D)가 실제로 비추는 범위 = 몬스터의 시야로 사용하는 감지 시스템
        // 거리/각도 검사는 순수 계산이라 가볍고, 그 안에 들어왔을 때만 raycast를 수행합니다.
        if (player == null || m_SpotLight2D == null)
            return;

        // 소화기 연막 안에 서 있으면 아무것도 보이지 않습니다.
        // 거리/각도/시선 검사보다 먼저 걸러서 불필요한 raycast도 아낍니다.
        if (IsBlindedByFog())
            return;

        Vector2 toPlayer = player.position - m_spotLight.position;
        float distance = toPlayer.magnitude;

        // 1) 거리 검사 : spotlight의 Outer Radius(빛이 닿는 최대 거리)보다 멀면 감지 실패
        if (distance > m_SpotLight2D.pointLightOuterRadius)
            return;

        // 2) 각도 검사 : 콘의 중심축(m_spotLight.up)과 플레이어 방향 사이 각도가
        //               Outer Angle의 절반을 넘으면 부채꼴 밖이므로 감지 실패
        if (Vector2.Angle(m_spotLight.up, toPlayer) > m_SpotLight2D.pointLightOuterAngle * 0.5f)
            return;

        // 3) 시선 검사 : 빛의 방향으로 raycast를 쏴서 벽 등 장애물에 가려지면 감지 실패.
        //    Queries Start In Colliders가 켜져 있어 몬스터 자신의 콜라이더에 맞을 수 있으므로
        //    RaycastAll(거리순 정렬)에서 자기 자신은 건너뛰고 첫 번째 대상만 판정합니다.
        RaycastHit2D[] hits = Physics2D.RaycastAll(m_spotLight.position, toPlayer.normalized, distance);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
                continue;   // 몬스터 자신(및 자식 콜라이더)은 무시

            if (hit.collider.CompareTag("Player"))
            {
                //Debug.Log("spotlight 시야 범위에 플레이어 감지! 추적을 시작합니다.");
                m_monsterFSM.SetChasingState();
            }
            break;   // 플레이어보다 벽 등이 먼저 맞았으면 시야가 가려진 것
        }
    }

    // FixedUpdate에서는 Rigidbody를 사용하는 물리적인 이동을 전담해야 끊기거나 속도가 느려지지 않습니다.
    void FixedUpdate()
    {
        if (m_IsActive && rb != null)
        {
            // MovePosition(Update와 충돌 가능) 대신 물리 속도(velocity)를 직접 조절하면
            // 2D 환경에서 밀림 저항 없이 깔끔하고 일정한 이동 속도를 보장합니다.
            rb.linearVelocity = movement * moveSpeed;
        }
        else if (rb != null)
        {
            // 비활성화 상태에서는 움직이지 않도록 속도를 0으로 설정합니다.
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void CalculateDirection()
    {
        if (player == null)
            return;

        MoveToward(player.position);
    }

    // 지정한 좌표를 향해 한 축씩(대각선 없이) 이동 방향을 정합니다.
    void MoveToward(Vector2 target)
    {
        Vector2 direction = target - (Vector2)transform.position;

        // 대각선 이동 방지 (가로/세로 중 더 멀리 떨어진 축을 우선으로 이동)
        // movement는 항상 단위 벡터라서 속도는 moveSpeed로 일정합니다.
        // (거리 벡터를 그대로 속도에 곱하면 멀수록 빨라져 순간이동처럼 보이게 됨)
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            movement = new Vector2(Mathf.Sign(direction.x), 0);
        }
        else
        {
            movement = new Vector2(0, Mathf.Sign(direction.y));
        }
    }

    // 목표 층이 지금 층과 다를 때: 현재 층의 계단 Spot까지 걸어가서, 도착하면 목표 층 방향으로 한 층 이동합니다.
    // 계단 Spot이 인스펙터에 연결돼 있지 않으면 false를 돌려주어 호출한 쪽이 대체 동작을 하게 합니다.
    bool MoveToFloorViaStair(int targetFloor)
    {
        int curFloor = GetCurrentFloor();
        Transform spot = GetStairSpot(curFloor);
        if (spot == null)
            return false;

        if (Vector2.Distance(transform.position, spot.position) > StairArriveThreshold)
        {
            MoveToward(spot.position);
            return true;
        }

        // 계단에 도착 — 공격 직후 쿨다운 중에는 층이동이 막히므로 제자리에서 대기합니다.
        movement = Vector2.zero;
        if (m_HitCooldown)
            return true;

        SetPatrolFloor(targetFloor > curFloor ? curFloor + 1 : curFloor - 1);
        return true;
    }

    // 외부(캐비넷 등)에서 "이 지점을 확인해 봐라"라고 지시할 때 호출합니다.
    // leaveAfter가 true면 그 지점을 확인한 뒤 위/아래 다른 층으로 떠납니다.
    public void SearchAt(Vector3 pos, int floor, bool leaveAfter)
    {
        m_Searching = true;
        m_SearchPos = pos;
        m_SearchFloor = Mathf.Clamp(floor, 1, 3);
        m_LeaveAfterSearch = leaveAfter;
        m_SearchLingerTimer = 0f;
        m_SearchElapsed = 0f;
    }

    // 수색 한 프레임 처리: 다른 층이면 계단으로 이동, 같은 층이면 수색 지점까지 걸어간 뒤
    // m_SearchLingerTime 동안 그 자리에서 머물다가 (필요하면) 다른 층으로 떠나고 배회로 돌아갑니다.
    void SearchStep()
    {
        // 벽 등에 막혀 수색 지점에 끝내 닿지 못하는 경우를 대비한 제한 시간
        m_SearchElapsed += Time.deltaTime;
        if (m_SearchElapsed >= m_SearchTimeLimit)
        {
            EndSearch();
            return;
        }

        if (m_SearchFloor != GetCurrentFloor())
        {
            if (!MoveToFloorViaStair(m_SearchFloor))
                EndSearch();   // 계단 Spot이 없어 그 층으로 갈 수 없으면 수색 포기
            return;
        }

        if (Vector2.Distance(transform.position, m_SearchPos) > StairArriveThreshold)
        {
            MoveToward(m_SearchPos);
            return;
        }

        // 수색 지점 도착 — 잠시 머물렀다가 마무리합니다.
        movement = Vector2.zero;
        m_SearchLingerTimer += Time.deltaTime;
        if (m_SearchLingerTimer < m_SearchLingerTime)
            return;

        if (m_LeaveAfterSearch)
            SetPatrolFloor(PickLeaveFloor(GetCurrentFloor()));

        EndSearch();
    }

    // 수색을 끝내고 평소 배회로 돌아갑니다.
    void EndSearch()
    {
        m_Searching = false;
        m_SearchLingerTimer = 0f;
        m_SearchElapsed = 0f;
        m_movingToMax = false;   // 새 위치에서 다시 최소 x부터 배회
    }

    // 수색을 마친 뒤 떠날 층: 1층은 2층, 3층은 2층으로 고정, 2층은 1층/3층 중 랜덤.
    int PickLeaveFloor(int curFloor)
    {
        if (curFloor == 1)
            return 2;
        if (curFloor == 3)
            return 2;
        return Random.Range(0, 2) == 0 ? 1 : 3;
    }

    // 플레이어가 현재 있는 층 (참조가 없으면 몬스터와 같은 층으로 간주해 기존 추격 유지)
    int GetPlayerFloor()
    {
        if (m_PlayerMove == null)
        {
            // Start 시점에 못 받아왔다면 여기서 한 번 더 시도합니다.
            if (GameMgr.Inst().m_GameScene != null && GameMgr.Inst().m_GameScene.m_GameUI != null)
                m_PlayerMove = GameMgr.Inst().m_GameScene.m_GameUI.m_Player;
            if (m_PlayerMove == null)
                return GetCurrentFloor();
        }

        return m_PlayerMove.CurrentFloor == Floor.F1 ? 1 : m_PlayerMove.CurrentFloor == Floor.F3 ? 3 : 2;
    }

    // 현재 층 불리언을 숫자(1~3)로 바꿔 돌려줍니다.
    int GetCurrentFloor()
    {
        return m_OnFloor1 ? 1 : m_OnFloor3 ? 3 : 2;
    }

    // 해당 층의 계단 도착 Spot
    Transform GetStairSpot(int floor)
    {
        return floor == 1 ? m_Floor1StairSpot : floor == 2 ? m_Floor2StairSpot : m_Floor3StairSpot;
    }

    // 배회 상태: 현재 위치에서 최소 x까지 걸어간 뒤, 최대 x까지 왕복합니다.
    // 그동안 플레이어를 감지하지 못하면 다음 층으로 이동해 반복합니다.
    // 배회 중에는 y로 이동하지 않습니다.
    void Patrol()
    {
        // 수색 지시를 받았으면 평소 배회 대신 그 지점부터 확인하러 갑니다.
        if (m_Searching)
        {
            SearchStep();
            return;
        }

        float minX, maxX;
        GetPatrolRange(out minX, out maxX);

        float targetX = m_movingToMax ? maxX : minX;
        float diff = targetX - transform.position.x;

        if (Mathf.Abs(diff) <= ArriveThreshold)
        {
            movement = Vector2.zero;
            AdvancePatrolPhase();
            return;
        }

        movement = new Vector2(Mathf.Sign(diff), 0f);
    }

    // 배회 한 구간이 끝났을 때: 최소 x 도착 → 최대 x로 방향 전환,
    // 최대 x까지 다 돌았는데 플레이어 감지 실패 → 다음 층으로 이동합니다.
    void AdvancePatrolPhase()
    {
        if (m_movingToMax)
        {
            MoveToNextFloor();
            m_movingToMax = false;   // 새 층에서는 다시 최소 x부터
        }
        else
        {
            m_movingToMax = true;
        }
    }

    // 현재 층 불리언에 맞는 배회 반경을 돌려줍니다.
    void GetPatrolRange(out float minX, out float maxX)
    {
        if (m_OnFloor1)
        {
            minX = floor1MinX; maxX = floor1MaxX;
        }
        else if (m_OnFloor3)
        {
            minX = floor3MinX; maxX = floor3MaxX;
        }
        else   // 기본 2층
        {
            minX = floor2MinX; maxX = floor2MaxX;
        }
    }

    // 1층 → 2층 → 3층 → 1층 순서로 순환하며 이동합니다.
    void MoveToNextFloor()
    {
        if (m_OnFloor1)
            TransferFloor(2);
        else if (m_OnFloor2)
            TransferFloor(3);
        else
            TransferFloor(1);
    }

    // 층이동 공통 처리: 공격 후 2초간 금지, 배회 실패를 누적하다 한도에 도달하면 랜덤 룸 Spot으로 텔레포트합니다.
    void TransferFloor(int targetFloor)
    {
        if (m_HitCooldown)
            return;   // 공격 직후 2초간 층이동 금지 (계단 포탈이 작동하지 않음)

        m_PatrolFailCount++;   // 한 층을 다 배회했는데도 플레이어 감지 실패
        if (m_PatrolFailCount >= m_PatrolFailLimit && TeleportToRandomRoomSpot())
            return;

        SetPatrolFloor(targetFloor);
        m_movingToMax = false;   // 새 층에서는 다시 최소 x부터
    }

    // 배회 실패가 누적되면 랜덤 룸의 자식 Spot으로 텔레포트합니다. (계단 Spot은 후보에서 제외)
    bool TeleportToRandomRoomSpot()
    {
        if (m_RoomSpots == null)
            return false;

        // 유효한 후보 개수 세기 (계단과 같은 Spot으로는 이동 불가)
        int count = 0;
        foreach (RoomSpotInfo info in m_RoomSpots)
        {
            if (info.m_Spot != null && !IsStairSpot(info.m_Spot))
                count++;
        }
        if (count == 0)
            return false;

        int pick = Random.Range(0, count);
        foreach (RoomSpotInfo info in m_RoomSpots)
        {
            if (info.m_Spot == null || IsStairSpot(info.m_Spot))
                continue;
            if (pick-- > 0)
                continue;

            SetFloorFlags(info.m_Floor);
            rb.position = info.m_Spot.position;
            m_movingToMax = false;   // 새 위치에서는 다시 최소 x부터 배회
            m_PatrolFailCount = 0;
            return true;
        }
        return false;
    }

    bool IsStairSpot(Transform spot)
    {
        return spot == m_Floor1StairSpot || spot == m_Floor2StairSpot || spot == m_Floor3StairSpot;
    }

    // 현재 층 불리언을 켜고 끄고, 몬스터를 해당 층 계단의 Spot 위치로 옮깁니다. (플레이어의 계단 이동과 같은 방식)
    void SetPatrolFloor(int floor)
    {
        SetFloorFlags(floor);

        Transform spot = GetStairSpot(floor);
        if (spot != null)
        {
            rb.position = spot.position;   // 계단 Spot으로 도착
        }
        else
        {
            // Spot이 지정되지 않았으면 기존 방식대로 y 좌표만 변경 (x는 유지)
            float y = floor == 1 ? floor1Y : floor == 2 ? floor2Y : floor3Y;
            rb.position = new Vector2(rb.position.x, y);
        }
    }

    void SetFloorFlags(int floor)
    {
        m_OnFloor1 = floor == 1;
        m_OnFloor2 = floor == 2;
        m_OnFloor3 = floor == 3;
    }

    void UpdateAnimation()
    {
        if (movement != Vector2.zero)
        {
            m_Animator.SetFloat("DirX", movement.x);
            m_Animator.SetFloat("DirY", movement.y);
        }
        m_Animator.SetBool("Walk", !m_Chasing && movement != Vector2.zero);
        m_Animator.SetBool("RUN", m_Chasing);
    }

    // 시야 범위를 이동 방향에 맞춰 회전시키고, visionForwardOffset만큼 앞으로 내보냅니다.
    private void UpdateVisionDirection()
    {
        if (movement == Vector2.zero)
            return;

        Vector2 dir = movement.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        // Spot Light 2D의 콘은 로컬 +Y(up) 방향으로 열리므로, 이동 방향 각도에서 90도를 빼서 일치시킵니다.
        Quaternion targetRot = Quaternion.Euler(0, 0, angle - 90f);
        // 좌/우 이동 시 x를 각각 고정값(visionLeftX/visionRightX)으로 두고, y는 초기 높이로 고정합니다.
        float visionX = 0f;
        if (dir.x < 0f)
            visionX = visionLeftX;
        else if (dir.x > 0f)
            visionX = visionRightX;
        Vector3 targetLocalPos = new Vector3(visionX, m_visionLocalY, 0f);

        if (m_spotLight != null)
        {
            if (visionRotateSpeed <= 0f)
                m_spotLight.rotation = targetRot;   // 즉시 회전
            else
                m_spotLight.rotation = Quaternion.RotateTowards(m_spotLight.rotation, targetRot, visionRotateSpeed * Time.deltaTime);

            m_spotLight.localPosition = targetLocalPos;
        }

        // 부채꼴 트리거 콜라이더도 스포트라이트와 같은 방향/위치로 맞춥니다.
        if (visionCone != null)
        {
            visionCone.rotation = m_spotLight != null ? m_spotLight.rotation : targetRot;
            visionCone.localPosition = targetLocalPos;
        }
    }
    public void Missed()  //만약에 플레이어를 (숨기) 놓침 or 위치가 너무 멀어서 놓침  == 호출
    {
        // FSM은 SetState를 부른 다음 프레임의 OnUpdate에서야 실제로 상태가 바뀝니다.
        // 그 사이에는 m_Chasing이 아직 true라 Update가 이 함수를 또 부르므로 여기서 막아줍니다.
        if (m_PlayerMissed)
            return;

        m_PlayerMissed = true;
        m_monsterFSM.SetAlertState();
    }

    // 소화기 연막 안에 서 있으면 눈이 가려져 플레이어를 보지도, 때리지도 못합니다.
    // 연막을 한 번도 쓰지 않았으면 FogMgr 자체가 없을 수 있으므로 null부터 확인합니다.
    bool IsBlindedByFog()
    {
        FogMgr fog = GameMgr.Inst().m_FogMgr;
        return fog != null && fog.IsInFog(transform.position);
    }

    // 플레이어와 닿으면 공격 판정, 계단에 닿으면 층이동,
    // 배회 중 벽 등에 막히면 그 구간 배회를 마친 것으로 보고 다음 단계로 넘어갑니다.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            AttackPlayer(collision.collider);
            return;
        }

        // 계단에 닿으면 플레이어처럼 계단 Spot을 통해 층을 이동합니다.
        // (공격 후 2초 동안은 포탈이 작동하지 않고 아래에서 벽처럼 처리되어 콜라이더에 막힘)
        // 경계 중에는 그 자리에 서 있어야 하므로 층이동과 배회 단계 진행을 모두 막습니다.
        // 수색 중에는 목적지가 정해져 있으므로 계단 이동/배회 단계 진행이 끼어들지 않게 막습니다.
        if (m_IsActive && !m_Chasing && !m_Alert && !m_Searching && !m_HitCooldown && TryUseStairs(collision.collider))
            return;

        if (m_IsActive && !m_Chasing && !m_Alert && !m_Searching)
            AdvancePatrolPhase();
    }

    // 계단 태그면 목적지 층을 계산해 층이동을 시도하고 true를 반환합니다.
    bool TryUseStairs(Collider2D col)
    {
        int curFloor = GetCurrentFloor();
        int target;

        if (col.CompareTag("Stairs") || col.CompareTag("OtherStair"))         // 올라가는 계단
            target = curFloor + 1;
        else if (col.CompareTag("Stairs2") || col.CompareTag("OtherStair2"))  // 내려가는 계단
            target = curFloor - 1;
        else
            return false;

        if (target < 1 || target > 3)
            return false;   // 더 갈 층이 없으면 벽처럼 처리

        TransferFloor(target);
        return true;
    }

    // 플레이어가 몬스터 콜라이더에 닿으면 공격 판정: 정수 데미지(35~45)를 주고 2초 쿨다운을 시작합니다.
    void AttackPlayer(Collider2D playerCol)
    {
        if (m_HitCooldown)
            return;

        // 연막 안에서는 플레이어를 찾지 못하므로 몸이 닿아도 공격하지 않습니다.
        if (IsBlindedByFog())
            return;

        PlayerStats stats = playerCol.GetComponent<PlayerStats>();
        if (stats == null)
            return;

        int damage = Random.Range(m_DamageMin, m_DamageMax + 1);   // 35~45 랜덤 정수 (max 포함)
        stats.TakeDamage(damage);
        StartCoroutine(HitCooldownRoutine(playerCol));
    }

    // 공격 후 2초 동안: 플레이어와의 충돌만 무시해서 플레이어가 몬스터를 뚫고 지나갈 수 있게 합니다.
    // (콜라이더 전체를 끄면 몬스터가 벽/계단까지 뚫고 다니므로, 플레이어와의 충돌 쌍만 무시)
    IEnumerator HitCooldownRoutine(Collider2D playerCol)
    {
        m_HitCooldown = true;
        if (m_BodyCollider != null)
            Physics2D.IgnoreCollision(m_BodyCollider, playerCol, true);

        yield return new WaitForSeconds(m_HitCooldownTime);

        if (m_BodyCollider != null && playerCol != null)
            Physics2D.IgnoreCollision(m_BodyCollider, playerCol, false);
        m_HitCooldown = false;
    }

    // 콜라이더 트리거(Is Trigger가 체크된) 영역에 다른 콜라이더(Player 등)가 들어왔을 때 자동으로 호출됩니다.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // composite는 null 에러가 날 수 있으므로 제외하고, 권장 방식인 CompareTag를 사용
        if (!m_IsActive && collision.CompareTag("Player"))
        {
            m_IsActive = true;
            // Debug.Log("콜라이더 시야 범위에 플레이어 감지! 추적을 시작합니다.");
            m_monsterFSM.SetChasingState();

            if (player == null)
            {
                player = collision.transform;
            }
        }
    }
}
