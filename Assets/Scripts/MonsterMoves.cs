using System.Collections;
using System.Collections.Generic;
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

// 경계(Alert) 상태에 들어온 이유 — 이유에 따라 행동이 달라집니다.
public enum AlertReason
{
    LostPlayer,   // 연막 등으로 플레이어를 놓침 → 그 자리에 잠시 멈춰 있음
    RoomNoise     // 플레이어가 방에서 나오는 소리를 들음 → 그 층을 한 바퀴 수색
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

    [Header("Floor Chase (추격 중 플레이어를 따라 층 이동)")]
    [Tooltip("추격 중 플레이어가 다른 층으로 도망갔을 때, 이 시간(초) 안에 계단에 닿지 못하면 추격을 포기합니다. 몬스터는 길찾기가 없어 벽에 막히면 계단까지 못 가기 때문입니다. 실제로 재보니 복도 끝에서 계단까지 12~15초가 걸려, 넉넉히 30초로 둡니다.")]
    public float m_FloorChaseTimeLimit = 30f;
    [Tooltip("계단으로 층을 옮긴 뒤 이 시간(초) 동안은 계단을 다시 타지 않습니다. 도착 지점 바로 옆 계단에 닿아 곧장 되돌아가는 것을 막습니다.")]
    public float m_StairCooldownTime = 0.8f;

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
    private bool m_PatrolReturned;  // 한 바퀴(최소→최대)를 다 돌고도 못 찾아서 반대편으로 되돌아가는 중
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
    private const float StairArriveThreshold = 0.5f;   // 수색 지점(캐비넷 등) 도착 판정 거리
    private const float StairTouchMargin = 0.15f;      // 계단에 '닿았다'고 볼 여유 (딱 맞닿은 순간을 놓치지 않기 위함)
    private const float StairReachDistance = 2f;       // 계단 중심에서 이 거리 안이면 도착으로 봅니다 (몸이 벽에 걸려 콜라이더까지 못 닿는 자리 대비)
    private const float StairStuckTime = 0.4f;         // 이 시간(초) 이상 제자리면 벽에 막힌 것으로 봅니다
    private const float StairStuckDist = 0.05f;        // 이 거리보다 조금 움직였으면 제자리로 봅니다
    private const float StairGiveUpTime = 4f;          // 이 시간(초) 동안 계단에 더 가까워지지 못하면 그 계단을 포기합니다
    private const float StairSkipTime = 12f;           // 포기한 계단을 다시 후보로 넣기까지 기다리는 시간(초)

    // ── 계단 층이동 ──
    // 씬에서 찾아둔 계단 한 개의 정보 (플레이어가 쓰는 계단을 몬스터도 그대로 사용합니다)
    class StairInfo
    {
        public Transform m_Tr;      // 계단 콜라이더의 Transform
        public int m_Floor;         // 이 계단이 있는 층 (1~3)
        public bool m_Up;           // true = 올라가는 계단, false = 내려가는 계단
        public Transform m_Exit;    // 이 계단을 탔을 때 도착하는 지점 (플레이어가 나오는 자리와 같음)
    }

    private List<StairInfo> m_Stairs;    // 씬에서 찾아둔 계단 목록 (처음 필요할 때 한 번만 만듭니다)
    private Transform m_NoticedStair;    // 플레이어가 방금 타고 사라진 계단 (추격 중에만 기억)
    private float m_StairReadyTime;      // 이 시각이 지나야 계단을 다시 탈 수 있습니다
    private float m_FloorChaseElapsed;   // 다른 층에 있는 플레이어를 쫓기 시작한 뒤 흐른 시간
    private Vector2 m_StairStuckPos;     // 벽에 막혔는지 보기 위해 기억해두는 직전 위치
    private float m_StairStuckTimer;     // 같은 자리에 머문 시간
    private bool m_StairDetour;          // true면 막힌 축을 피해 다른 축으로 돌아가는 중
    private Transform m_StairTarget;     // 지금 향하고 있는 계단 (바뀌면 진행 상황을 새로 잽니다)
    private float m_StairBestDist;       // 그 계단에 가장 가까이 갔던 거리
    private float m_StairNoProgress;     // 더 가까워지지 못한 채 흐른 시간
    private Transform m_SkipStair;       // 끝내 닿지 못해 당분간 건너뛸 계단
    private float m_SkipStairUntil;      // 이 시각이 지나면 그 계단을 다시 후보에 넣습니다

    // ── 수색(캐비넷 등 특정 지점으로 가보기) 상태 ──
    private CrumbTrail m_CrumbTrail;    // 플레이어가 흘리는 과자(발자취) — 추격 중 경로 추종에 사용
    private const float CrumbArriveThreshold = 0.25f;   // 과자 도착 판정 거리 (과자 간격보다 작아야 떨리지 않음)

    // ── 문소리(방 출입) 감지 ──
    private int m_NoticedFloor;         // 플레이어가 방에서 나온 층 (0이면 들은 소리 없음)
    private AlertReason m_AlertReason;  // 지금 경계 상태에 들어온 이유
    private int m_SweepPhase;           // 경계 수색 단계: 0 = 최소 x로, 1 = 최대 x로
    private float m_SweepElapsed;       // 경계 수색을 시작한 뒤 흐른 시간 (제한 시간 확인용)

    private bool m_Searching;           // 수색 중이면 배회 대신 수색 지점으로 이동
    private Vector2 m_SearchPos;        // 수색할 좌표
    private int m_SearchFloor;          // 그 좌표가 있는 층
    private bool m_LeaveAfterSearch;    // 수색을 마친 뒤 다른 층으로 떠날지
    private float m_SearchLingerTimer;  // 도착 후 머문 시간
    private float m_SearchElapsed;      // 수색을 시작한 뒤 흐른 시간 (제한 시간 확인용)
    private bool m_SearchGoOpposite;    // 수색 지점에서 못 찾아 그 층 반대편 끝으로 가보는 중

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
        m_NoticedStair = null;    // 추격이 끝났으므로 기억해둔 계단도 지웁니다
    }

    public void OnChase()
    {
        m_Chasing = true;
        m_Patrol = false;
        m_Alert = false;
        m_PlayerMissed = false;
        m_PatrolFailCount = 0;   // 플레이어를 감지했으므로 배회 실패 누적 리셋
        m_Searching = false;     // 플레이어를 다시 찾았으니 수색은 필요 없음
        m_NoticedFloor = 0;      // 직접 찾았으므로 문소리 정보는 더 이상 필요 없음
        m_FloorChaseElapsed = 0f;   // 층 따라가기 제한 시간을 새로 잽니다
        m_StairDetour = false;      // 계단 우회 상태도 새로 시작합니다
        m_StairStuckTimer = 0f;
        m_StairTarget = null;       // 포기했던 계단도 새 추격에서는 다시 후보로
        m_StairNoProgress = 0f;
        m_SkipStair = null;
    }

    // 경계 상태 진입 — 이유(m_AlertReason)에 따라 행동이 갈립니다.
    // LostPlayer : 추격을 멈추고 그 자리에 섭니다. (일정 시간 뒤 배회 복귀 타이머는 MonsterScene이 돌림)
    // RoomNoise  : 문소리가 난 층을 한 바퀴 훑는 수색을 시작합니다.
    public void OnAlert()
    {
        m_Chasing = false;
        m_Patrol = false;
        m_Alert = true;

        // 문소리를 듣고 온 경계는 그 층을 훑고 다녀야 하므로 멈추지 않습니다.
        if (m_AlertReason == AlertReason.RoomNoise)
        {
            m_SweepPhase = 0;
            m_SweepElapsed = 0f;
            return;
        }

        movement = Vector2.zero;
    }

    // 지금 경계 상태에 들어온 이유 (MonsterScene이 복귀 타이머를 걸지 판단할 때 사용)
    public AlertReason AlertReasonNow { get { return m_AlertReason; } }

    // 플레이어가 방에서 나오는 소리를 들었을 때, 그 방이 있는 층(1~3)을 전달받습니다.
    // 다른 층이면 계단으로 그 층까지 이동하고, 도착하는 순간 경계(수색) 상태로 바뀝니다.
    public void NoticeFloor(int floor)
    {
        if (!m_IsActive || m_Chasing)
            return;   // 아직 활동 전이거나, 이미 추격 중이라면 더 확실한 정보를 갖고 있으므로 무시

        floor = Mathf.Clamp(floor, 1, 3);

        if (floor == GetCurrentFloor())
        {
            m_NoticedFloor = 0;
            BeginNoiseAlert();
            return;
        }

        m_NoticedFloor = floor;   // 배회 처리에서 이 층으로 향합니다
        m_Searching = false;      // 캐비넷 수색보다 방금 들은 소리를 우선합니다
    }

    // 문소리 경계 상태로 진입합니다.
    void BeginNoiseAlert()
    {
        m_AlertReason = AlertReason.RoomNoise;
        m_monsterFSM.SetAlertState();
    }

    // 문소리를 들은 층으로 계단을 타고 이동합니다. 도착하면 경계(수색) 상태로 넘어갑니다.
    // 이동을 처리했으면 true를 돌려주어 평소 배회가 끼어들지 않게 합니다.
    bool MoveToNoticedFloor()
    {
        if (m_NoticedFloor == 0)
            return false;

        if (m_NoticedFloor == GetCurrentFloor())
        {
            m_NoticedFloor = 0;
            movement = Vector2.zero;
            BeginNoiseAlert();
            return true;
        }

        if (!MoveToFloorViaStair(m_NoticedFloor))
        {
            m_NoticedFloor = 0;   // 계단 Spot이 없어 그 층으로 갈 수 없으면 포기
            return false;
        }

        return true;
    }

    // 경계 수색 한 프레임 처리: 현재 층의 배회 반경을 최소 x → 최대 x 로 딱 한 번만 훑습니다.
    // 다 훑었거나 제한 시간을 넘기면 평소 배회로 돌아갑니다.
    void AlertSweepStep()
    {
        m_SweepElapsed += Time.deltaTime;
        if (m_SweepElapsed >= m_SearchTimeLimit)
        {
            EndAlertSweep();
            return;
        }

        float minX, maxX;
        GetPatrolRange(out minX, out maxX);

        float targetX = m_SweepPhase == 0 ? minX : maxX;
        float diff = targetX - transform.position.x;

        if (Mathf.Abs(diff) <= ArriveThreshold)
        {
            movement = Vector2.zero;
            AdvanceSweepPhase();
            return;
        }

        movement = new Vector2(Mathf.Sign(diff), 0f);
    }

    // 수색 한 구간이 끝났을 때: 최소 x 도착 → 최대 x로, 최대 x까지 다 훑었으면 수색 종료.
    void AdvanceSweepPhase()
    {
        if (m_SweepPhase == 0)
            m_SweepPhase = 1;
        else
            EndAlertSweep();
    }

    // 경계 수색을 끝내고 평소 배회로 돌아갑니다.
    void EndAlertSweep()
    {
        m_SweepPhase = 0;
        m_SweepElapsed = 0f;
        ResetPatrolPhase();   // 새 위치에서 다시 최소 x부터 배회
        m_monsterFSM.SetPatrolState();
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
                if (playerFloor != GetCurrentFloor())
                {
                    // 몬스터는 길찾기가 없어서 벽에 막히면 계단까지 영영 가지 못합니다.
                    // 그래서 다른 층을 쫓는 동안만 시간을 재고, 너무 오래 걸리면 추격을 포기합니다.
                    m_FloorChaseElapsed += Time.deltaTime;
                    if (m_FloorChaseElapsed >= m_FloorChaseTimeLimit)
                    {
                        movement = Vector2.zero;
                        Missed();   // 경계 상태로 바뀌고, 잠시 뒤 배회로 돌아갑니다
                    }
                    else if (!MoveToFloorViaStair(playerFloor))
                    {
                        CalculateDirection();
                    }
                }
                // 같은 층: 눈에 직접 보이면 곧장 달려가고, 안 보이면 플레이어가 흘린
                // 과자(발자취)를 따라 실제로 지나간 경로를 되짚어 갑니다.
                else
                {
                    m_FloorChaseElapsed = 0f;   // 같은 층으로 따라붙었으니 제한 시간을 다시 잽니다
                    if (IsPlayerVisible() || !FollowCrumbs())
                        CalculateDirection();
                }
            }
        }
        else if (m_Alert)
        {
            if (m_AlertReason == AlertReason.RoomNoise)
            {
                // 문소리를 듣고 온 경계: 그 층을 한 바퀴 훑으며 플레이어를 찾습니다.
                moveSpeed = patrolSpeed;
                AlertSweepStep();
                DetectPlayer();
            }
            else
            {
                // 플레이어를 놓친 경계: 놓친 자리에서 제자리에 멈춰 있습니다.
                // MonsterScene이 m_AlertTime초 뒤에 배회 상태로 되돌려 줍니다.
                movement = Vector2.zero;
            }
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
        if (IsPlayerVisible())
        {
            //Debug.Log("spotlight 시야 범위에 플레이어 감지! 추적을 시작합니다.");
            m_monsterFSM.SetChasingState();
        }
    }

    // 지금 이 순간 플레이어가 몬스터의 시야(스포트라이트가 비추는 범위) 안에 있는지 판정합니다.
    // 감지(배회 중 추격 시작)와 추격 중 '직선으로 달려갈지' 판단에 함께 사용합니다.
    public bool IsPlayerVisible()
    {
        // 플레이어가 캐비넷에 숨어 있으면 어떤 경우에도 보이지 않습니다 = '못 찾음'.
        // 숨는 동안 플레이어 콜라이더가 꺼져 raycast에도 안 잡히지만,
        // 그건 부수 효과일 뿐이라 판정 근거를 여기에 명시해 둡니다.
        // (검사 비용이 가장 싼 조건이라 맨 앞에 두어 아래 계산도 함께 아낍니다)
        if (Cabinet.Hiding != null)
            return false;

        // spotlight(Spot Light 2D)가 실제로 비추는 범위 = 몬스터의 시야로 사용하는 감지 시스템
        // 거리/각도 검사는 순수 계산이라 가볍고, 그 안에 들어왔을 때만 raycast를 수행합니다.
        if (player == null || m_SpotLight2D == null || m_spotLight == null)
            return false;

        // 소화기 연막 안에 서 있으면 아무것도 보이지 않습니다.
        // 거리/각도/시선 검사보다 먼저 걸러서 불필요한 raycast도 아낍니다.
        if (IsBlindedByFog())
            return false;

        Vector2 toPlayer = player.position - m_spotLight.position;
        float distance = toPlayer.magnitude;

        // 1) 거리 검사 : spotlight의 Outer Radius(빛이 닿는 최대 거리)보다 멀면 감지 실패
        if (distance > m_SpotLight2D.pointLightOuterRadius)
            return false;

        // 2) 각도 검사 : 콘의 중심축(m_spotLight.up)과 플레이어 방향 사이 각도가
        //               Outer Angle의 절반을 넘으면 부채꼴 밖이므로 감지 실패
        if (Vector2.Angle(m_spotLight.up, toPlayer) > m_SpotLight2D.pointLightOuterAngle * 0.5f)
            return false;

        // 3) 시선 검사 : 빛의 방향으로 raycast를 쏴서 벽 등 장애물에 가려지면 감지 실패.
        //    Queries Start In Colliders가 켜져 있어 몬스터 자신의 콜라이더에 맞을 수 있으므로
        //    RaycastAll(거리순 정렬)에서 자기 자신은 건너뛰고 첫 번째 대상만 판정합니다.
        RaycastHit2D[] hits = Physics2D.RaycastAll(m_spotLight.position, toPlayer.normalized, distance);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
                continue;   // 몬스터 자신(및 자식 콜라이더)은 무시

            return hit.collider.CompareTag("Player");   // 벽 등이 먼저 맞았으면 시야가 가려진 것
        }

        return false;
    }

    // 플레이어가 흘린 과자를 한 알씩 밟으며 따라갑니다.
    // 같은 층에 남은 과자가 없으면 false를 돌려주어 호출한 쪽이 직선 추격으로 돌아가게 합니다.
    bool FollowCrumbs()
    {
        CrumbTrail trail = GetCrumbTrail();
        if (trail == null)
            return false;

        Vector2 target;
        if (!trail.TryGetTarget(transform.position, GetCurrentFloor(), out target))
            return false;

        // 과자에 닿았으면 먹어 치우고 다음 과자를 목표로 삼습니다.
        if (Vector2.Distance(transform.position, target) <= CrumbArriveThreshold)
        {
            trail.Consume();
            if (!trail.TryGetTarget(transform.position, GetCurrentFloor(), out target))
                return false;
        }

        MoveToward(target);
        return true;
    }

    // 플레이어에 붙어 있는 CrumbTrail 컴포넌트 (처음 한 번만 찾아 보관)
    CrumbTrail GetCrumbTrail()
    {
        if (m_CrumbTrail == null && player != null)
            m_CrumbTrail = player.GetComponent<CrumbTrail>();

        return m_CrumbTrail;
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

    // 목표 층이 지금 층과 다를 때: 실제 계단까지 걸어가서, 도착하면 그 계단으로 한 층 이동합니다.
    // 갈 수 있는 계단을 찾지 못하면 false를 돌려주어 호출한 쪽이 대체 동작을 하게 합니다.
    bool MoveToFloorViaStair(int targetFloor)
    {
        StairInfo stair = PickStair(GetCurrentFloor(), targetFloor);
        if (stair == null)
            return false;

        if (!HasReachedStair(stair))
        {
            if (GiveUpStairIfStuck(stair))
                return true;   // 이 계단은 포기 — 다음 프레임에 다른 계단을 고릅니다

            MoveTowardStair(stair.m_Tr.position);
            return true;
        }

        // 계단에 도착 — 공격 직후나 방금 층을 옮긴 직후에는 층이동이 막히므로 제자리에서 대기합니다.
        movement = Vector2.zero;
        if (m_HitCooldown || Time.time < m_StairReadyTime)
            return true;

        UseStair(stair);
        return true;
    }

    // 목표 계단에 좀처럼 가까워지지 못하면 그 계단을 당분간 후보에서 빼고 다른 계단을 찾게 합니다.
    // 몬스터 몸집(가로 1.1 세로 2.3)이 커서, 플레이어는 지나가도 몬스터는 끝내 닿지 못하는 계단이 실제로 있습니다.
    // 포기하면 다음 프레임에 PickStair가 다른 계단을 골라줍니다.
    bool GiveUpStairIfStuck(StairInfo stair)
    {
        float dist = Vector2.Distance(transform.position, stair.m_Tr.position);

        if (stair.m_Tr != m_StairTarget)
        {
            m_StairTarget = stair.m_Tr;   // 목표가 바뀌었으니 처음부터 다시 잽니다
            m_StairBestDist = dist;
            m_StairNoProgress = 0f;
            return false;
        }

        if (dist < m_StairBestDist - StairStuckDist)
        {
            m_StairBestDist = dist;   // 더 가까워졌다 = 잘 가고 있는 중
            m_StairNoProgress = 0f;
            return false;
        }

        m_StairNoProgress += Time.deltaTime;
        if (m_StairNoProgress < StairGiveUpTime)
            return false;

        m_SkipStair = stair.m_Tr;
        m_SkipStairUntil = Time.time + StairSkipTime;
        m_StairTarget = null;
        m_StairNoProgress = 0f;
        m_StairDetour = false;
        movement = Vector2.zero;
        return true;
    }

    // 계단으로 갈 때 쓰는 이동입니다.
    // 이 몬스터는 길찾기가 없어서, 가로/세로 중 한 축을 골라 이동하다 벽을 만나면 그 축을 계속 밀기만 합니다.
    // 그래서 잠깐 제자리에 머무르면 막힌 것으로 보고 축을 바꿔 돌아가게 합니다.
    // (복도를 낀 학교 구조에서는 이 정도만으로 대부분 계단까지 갈 수 있습니다.
    //  그래도 끝내 못 가면 m_FloorChaseTimeLimit이 지나 추격을 포기합니다)
    void MoveTowardStair(Vector2 target)
    {
        Vector2 pos = transform.position;

        if (Vector2.Distance(pos, m_StairStuckPos) < StairStuckDist)
        {
            m_StairStuckTimer += Time.deltaTime;
            if (m_StairStuckTimer >= StairStuckTime)
            {
                m_StairDetour = !m_StairDetour;   // 막혔으니 반대 축으로 돌아갑니다
                m_StairStuckTimer = 0f;
            }
        }
        else
        {
            m_StairStuckTimer = 0f;
            m_StairStuckPos = pos;
        }

        Vector2 dir = target - pos;
        bool useX = Mathf.Abs(dir.x) > Mathf.Abs(dir.y);
        if (m_StairDetour)
            useX = !useX;

        // 고른 축이 이미 목표에 거의 맞아 있으면 남은 축으로 움직입니다.
        if (useX && Mathf.Abs(dir.x) < ArriveThreshold)
            useX = false;
        else if (!useX && Mathf.Abs(dir.y) < ArriveThreshold)
            useX = true;

        movement = useX ? new Vector2(Mathf.Sign(dir.x), 0f) : new Vector2(0f, Mathf.Sign(dir.y));
    }

    // 계단에 도착했는지 확인합니다.
    // 계단은 벽처럼 막는 콜라이더라 몬스터 몸통(가로 약 1.1, 세로 약 2.3)이 먼저 부딪혀
    // 계단 중심까지는 1.0~1.6 정도가 남습니다. 그래서 '중심까지의 거리'로 재면 영영 도착하지 못합니다.
    // 대신 플레이어가 계단을 밟는 것과 같게 '몸이 계단에 닿았는지'로 판정합니다.
    bool HasReachedStair(StairInfo stair)
    {
        // 계단 코앞까지 왔으면 도착으로 봅니다.
        // 옆 벽에 몸이 걸려 콜라이더에 끝내 닿지 못하는 자리가 실제로 있어서, 거리로도 한 번 봐줍니다.
        if (Vector2.Distance(transform.position, stair.m_Tr.position) <= StairReachDistance)
            return true;

        Collider2D stairCol = stair.m_Tr.GetComponent<Collider2D>();
        if (stairCol == null || m_BodyCollider == null)
            return false;

        // 큰 계단이라면 중심은 멀어도 몸이 이미 닿아 있을 수 있으므로, 맞닿음도 함께 봅니다.
        Bounds body = m_BodyCollider.bounds;
        body.Expand(StairTouchMargin);
        return body.Intersects(stairCol.bounds);
    }

    // 지금 층에서 목표 층 쪽으로 갈 수 있는 계단을 고릅니다.
    // 플레이어가 방금 타고 사라진 계단이 조건에 맞으면 그것을 먼저 쓰고(발자취를 쫓듯),
    // 그런 계단이 없으면 몬스터에게 가장 가까운 계단을 씁니다.
    StairInfo PickStair(int curFloor, int targetFloor)
    {
        List<StairInfo> stairs = GetStairs();
        if (stairs == null)
            return null;

        bool up = targetFloor > curFloor;
        StairInfo nearest = null;
        float nearestDist = float.MaxValue;

        foreach (StairInfo stair in stairs)
        {
            if (stair.m_Floor != curFloor || stair.m_Up != up)
                continue;   // 다른 층 계단이거나 반대 방향 계단

            if (stair.m_Tr == m_SkipStair && Time.time < m_SkipStairUntil)
                continue;   // 아까 끝내 닿지 못한 계단은 당분간 건너뜁니다

            if (stair.m_Tr == m_NoticedStair)
                return stair;   // 플레이어가 탄 바로 그 계단 — 더 볼 것 없이 이걸로

            // 제곱근 없이 거리제곱끼리 비교합니다.
            float dist = Vector2.SqrMagnitude((Vector2)stair.m_Tr.position - (Vector2)transform.position);
            if (dist >= nearestDist)
                continue;

            nearestDist = dist;
            nearest = stair;
        }

        return nearest;
    }

    // 계단을 타고 한 층 이동합니다. 플레이어가 그 계단으로 나오는 자리와 똑같은 곳에 나타납니다.
    void UseStair(StairInfo stair)
    {
        SetFloorFlags(stair.m_Up ? stair.m_Floor + 1 : stair.m_Floor - 1);
        rb.position = stair.m_Exit.position;

        m_StairReadyTime = Time.time + m_StairCooldownTime;   // 도착 지점 옆 계단에 곧바로 다시 닿는 것을 막습니다
        m_NoticedStair = null;   // 이 계단은 다 썼습니다
        ResetPatrolPhase();      // 새 층에서는 다시 최소 x부터 배회
        m_StairDetour = false;   // 새 층에서는 우회 없이 다시 판단합니다
        m_StairStuckTimer = 0f;
        m_StairTarget = null;    // 새 층에서 계단 진행 상황을 새로 잽니다
        m_StairNoProgress = 0f;
    }

    // 플레이어가 계단을 타고 층을 옮겼을 때 그 계단을 알려받습니다. (PlayerMove에서 호출)
    // 쫓기던 중이었다면 몬스터가 바로 그 계단으로 따라갑니다.
    public void NoticeStair(Transform stair)
    {
        if (!m_IsActive || !m_Chasing)
            return;   // 활동 전이거나 쫓고 있지 않았다면 플레이어가 어디로 갔는지 알 수 없습니다

        m_NoticedStair = stair;
    }

    // 씬에 있는 계단을 모두 찾아 표로 만들어 둡니다. (처음 필요할 때 한 번만 만들고 이후 재사용)
    // 각 층 맵 오브젝트(Floor1/Floor2/Floor3) 아래를 훑으므로 어느 층 계단인지가 계층만으로 정확히 갈립니다.
    // GameObject.FindGameObjectsWithTag는 꺼져 있는 오브젝트를 찾지 못해 사용하지 않습니다.
    List<StairInfo> GetStairs()
    {
        if (m_Stairs != null)
            return m_Stairs;

        GameScene gameScene = GameMgr.Inst().m_GameScene;
        GameUI ui = gameScene != null ? gameScene.m_GameUI : null;
        if (ui == null || ui.m_Floor1 == null || ui.m_Floor2 == null || ui.m_Floor3 == null)
            return null;   // 아직 준비되기 전이면 다음 프레임에 다시 시도합니다

        Transform[] floorRoots = { ui.m_Floor1.transform, ui.m_Floor2.transform, ui.m_Floor3.transform };

        m_Stairs = new List<StairInfo>();
        for (int i = 0; i < floorRoots.Length; i++)
        {
            // 꺼져 있는 자식까지 포함해서 훑습니다. (층 맵이 SetActive(false)인 순간이 있습니다)
            Transform[] all = floorRoots[i].GetComponentsInChildren<Transform>(true);
            foreach (Transform tr in all)
                TryAddStair(tr, i + 1, floorRoots);
        }

        if (m_Stairs.Count == 0)
            Debug.LogWarning("몬스터가 쓸 계단을 하나도 찾지 못했습니다. 추격 중 층 따라가기가 동작하지 않습니다.", this);

        return m_Stairs;
    }

    // 이 오브젝트가 계단이면 방향과 도착 지점을 알아내 표에 넣습니다.
    // 도착 지점을 찾지 못한 계단은 넣지 않습니다. (엉뚱한 곳으로 순간이동하는 사고를 막기 위함)
    void TryAddStair(Transform tr, int floor, Transform[] floorRoots)
    {
        bool up;
        if (tr.CompareTag("Stairs") || tr.CompareTag("OtherStair"))
            up = true;
        else if (tr.CompareTag("Stairs2") || tr.CompareTag("OtherStair2"))
            up = false;
        else
            return;   // 계단이 아님

        int targetFloor = up ? floor + 1 : floor - 1;
        if (targetFloor < 1 || targetFloor > 3)
            return;   // 1층 아래나 3층 위로는 갈 곳이 없습니다

        string exitName = GetExitSpotName(tr, floor);
        Transform exit = exitName != null ? floorRoots[targetFloor - 1].Find(exitName) : null;
        if (exit == null)
        {
            Debug.LogWarning("계단 '" + tr.name + "'의 도착 지점 '" + exitName + "'을(를) 찾지 못해 몬스터가 이 계단을 쓸 수 없습니다.", tr);
            return;
        }

        m_Stairs.Add(new StairInfo { m_Tr = tr, m_Floor = floor, m_Up = up, m_Exit = exit });
    }

    // 계단의 태그와 층에 따라, 그 계단을 탔을 때 도착하는 지점의 이름을 돌려줍니다.
    // PlayerMove.OnCollisionEnter2D의 계단 처리와 똑같은 표라서 몬스터가 플레이어와 같은 자리로 나옵니다.
    string GetExitSpotName(Transform stair, int floor)
    {
        if (stair.CompareTag("Stairs"))          // 좌측 올라가는 계단
            return floor == 1 ? "F2Spot" : floor == 2 ? "F3Spot" : null;
        if (stair.CompareTag("OtherStair"))      // 우측 올라가는 계단
            return floor == 1 ? "F2Spot2" : floor == 2 ? "F3Spot2" : null;
        if (stair.CompareTag("Stairs2"))         // 좌측 내려가는 계단
            return floor == 2 ? "F1SpotD" : floor == 3 ? "F2Spot" : null;
        if (stair.CompareTag("OtherStair2"))     // 우측 내려가는 계단
            return floor == 2 ? "F1SpotD2" : floor == 3 ? "F2Spot3" : null;

        return null;
    }

    // 외부(캐비넷 등)에서 "이 지점을 확인해 봐라"라고 지시할 때 호출합니다.
    // leaveAfter가 true면 그 지점을 확인한 뒤 위/아래 다른 층으로 떠납니다.
    public void SearchAt(Vector3 pos, int floor, bool leaveAfter)
    {
        m_Searching = true;
        m_NoticedFloor = 0;   // 캐비넷 수색 지시가 이전에 들은 문소리보다 우선합니다
        m_SearchPos = pos;
        m_SearchFloor = Mathf.Clamp(floor, 1, 3);
        m_LeaveAfterSearch = leaveAfter;
        m_SearchGoOpposite = false;
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

        // 수색 지점까지 와서 머물렀는데도 플레이어를 못 찾음.
        // (캐비넷에 숨어 있으면 IsPlayerVisible이 false라 반드시 이쪽으로 옵니다)
        // 층을 떠나기 전에 이 층 반대편 끝까지 딱 한 번 가봅니다.
        if (!m_SearchGoOpposite)
        {
            BeginOppositeSearch();
            return;
        }

        // 반대편에서도 못 찾음 → 이제 층을 옮깁니다.
        if (m_LeaveAfterSearch)
            SetPatrolFloor(PickLeaveFloor(GetCurrentFloor()));

        EndSearch();
    }

    // 수색 지점에서 못 찾았을 때: 같은 층에서 지금 자리보다 먼 쪽 끝을 새 수색 지점으로 삼습니다.
    // 배회와 똑같이 x로만 움직이도록 y는 현재 높이를 그대로 씁니다.
    void BeginOppositeSearch()
    {
        float minX, maxX;
        GetPatrolRange(out minX, out maxX);

        float x = transform.position.x;
        float targetX = (x - minX) > (maxX - x) ? minX : maxX;   // 더 먼 쪽 끝 = 반대편

        m_SearchGoOpposite = true;
        m_SearchPos = new Vector2(targetX, transform.position.y);
        m_SearchLingerTimer = 0f;
        m_SearchElapsed = 0f;   // 반대편까지 걸어갈 시간을 새로 잽니다
    }

    // 수색을 끝내고 평소 배회로 돌아갑니다.
    void EndSearch()
    {
        m_Searching = false;
        m_SearchGoOpposite = false;
        m_SearchLingerTimer = 0f;
        m_SearchElapsed = 0f;
        ResetPatrolPhase();   // 새 위치에서 다시 최소 x부터 배회
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
    // 인스펙터에 직접 지정한 것이 있으면 그것을 쓰고, 비어 있으면 계단 표에서 찾아 씁니다.
    Transform GetStairSpot(int floor)
    {
        Transform spot = floor == 1 ? m_Floor1StairSpot : floor == 2 ? m_Floor2StairSpot : m_Floor3StairSpot;
        if (spot != null)
            return spot;

        return GetFloorArrivalSpot(floor);
    }

    // 그 층으로 올라오거나 내려오는 계단의 도착 지점을 계단 표에서 찾습니다.
    // (플레이어가 그 층으로 이동했을 때 나타나는 자리와 같은 곳이라 확실히 통로 위입니다)
    // 후보가 여러 개면 몬스터 몸이 계단에 걸치지 않는 자리를 먼저 씁니다.
    Transform GetFloorArrivalSpot(int floor)
    {
        List<StairInfo> stairs = GetStairs();
        if (stairs == null)
            return null;

        Transform fallback = null;   // 걸치지 않는 자리가 하나도 없을 때 쓸 예비

        foreach (StairInfo stair in stairs)
        {
            int arriveFloor = stair.m_Up ? stair.m_Floor + 1 : stair.m_Floor - 1;
            if (arriveFloor != floor)
                continue;

            if (fallback == null)
                fallback = stair.m_Exit;

            if (!IsSpotBlockedByStair(stair.m_Exit, floor, stairs))
                return stair.m_Exit;
        }

        return fallback;
    }

    // 그 자리에 섰을 때 몬스터 몸통이 같은 층 계단에 걸치는지 확인합니다.
    // 걸치는 자리에 내리면 계단 콜라이더에 밀리거나 곧바로 다시 층을 옮기게 됩니다.
    bool IsSpotBlockedByStair(Transform spot, int floor, List<StairInfo> stairs)
    {
        if (spot == null || m_BodyCollider == null)
            return false;

        Bounds body = new Bounds(spot.position, m_BodyCollider.bounds.size);
        foreach (StairInfo stair in stairs)
        {
            if (stair.m_Floor != floor)
                continue;

            Collider2D col = stair.m_Tr.GetComponent<Collider2D>();
            if (col != null && body.Intersects(col.bounds))
                return true;
        }

        return false;
    }

    // 배회 상태: 현재 위치에서 최소 x까지 걸어간 뒤, 최대 x까지 왕복합니다.
    // 그동안 플레이어를 감지하지 못하면 다음 층으로 이동해 반복합니다.
    // 배회 중에는 y로 이동하지 않습니다.
    void Patrol()
    {
        // 방에서 나오는 소리를 들었으면 평소 배회 대신 그 층부터 가봅니다.
        if (MoveToNoticedFloor())
            return;

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

    // 배회 한 구간이 끝났을 때 (= 그 방향으로는 플레이어를 못 찾았을 때) 어디로 갈지 정합니다.
    //   최소 x 도착        → 최대 x 로  (한 바퀴 진행)
    //   최대 x 도착        → 못 찾았으므로 층을 떠나기 전에 반대편(최소 x)으로 딱 한 번 더
    //   반대편까지 갔는데도 못 찾음 → 그때 다음 층으로 이동
    void AdvancePatrolPhase()
    {
        // 아직 반대편으로 되돌아가 보지 않았다면 층을 떠나지 않고 방향만 바꿉니다.
        if (!m_PatrolReturned)
        {
            // 최소 x → 최대 x 로 바꾸는 것은 원래 한 바퀴에 포함되므로,
            // '반대편으로 한 번 더'는 최대 x 까지 다 훑은 뒤부터 셉니다.
            if (m_movingToMax)
                m_PatrolReturned = true;

            m_movingToMax = !m_movingToMax;
            return;
        }

        // 반대편까지 되돌아왔는데도 못 찾음 → 이 층에는 없다고 보고 다음 층으로
        MoveToNextFloor();
        ResetPatrolPhase();
    }

    // 배회 단계를 처음 상태로 되돌립니다. (새 층·새 위치에서는 다시 최소 x 부터 왕복)
    void ResetPatrolPhase()
    {
        m_movingToMax = false;
        m_PatrolReturned = false;
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
        ResetPatrolPhase();   // 새 층에서는 다시 최소 x부터
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
            ResetPatrolPhase();   // 새 위치에서는 다시 최소 x부터 배회
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
            // 쓸 수 있는 Spot을 끝내 찾지 못했으면 기존 방식대로 y 좌표만 변경 (x는 유지)
            float y = floor == 1 ? floor1Y : floor == 2 ? floor2Y : floor3Y;
            rb.position = new Vector2(rb.position.x, y);
        }

        // 도착 지점 바로 옆에 계단이 있는 층이 있어서, 내리자마자 그 계단에 닿아 되돌아가는 것을 막습니다.
        m_StairReadyTime = Time.time + m_StairCooldownTime;
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
        m_AlertReason = AlertReason.LostPlayer;
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

        if (!m_IsActive)
            return;

        if (m_Alert)
        {
            // 문소리를 듣고 온 경계 수색 중 벽에 막혔다면, 그 방향은 다 훑은 것으로 보고
            // 다음 단계로 넘어갑니다. (플레이어를 놓친 경계는 그 자리에 서 있어야 하므로 아무것도 안 함)
            if (m_AlertReason == AlertReason.RoomNoise)
                AdvanceSweepPhase();
            return;
        }

        // 추격 중 / 캐비넷 수색 중 / 문소리를 듣고 특정 층으로 가는 중에는 목적지가 정해져 있으므로
        // 계단 이동과 배회 단계 진행이 끼어들지 않게 막습니다.
        if (m_Chasing || m_Searching || m_NoticedFloor != 0)
            return;

        // 계단에 닿으면 플레이어처럼 계단 Spot을 통해 층을 이동합니다.
        // (공격 후 2초 동안과 방금 층을 옮긴 직후에는 포탈이 작동하지 않고 벽처럼 처리되어 콜라이더에 막힘)
        if (!m_HitCooldown && Time.time >= m_StairReadyTime && TryUseStairs(collision.collider))
            return;

        // 벽 등에 막혔으면 그 구간 배회를 마친 것으로 보고 다음 단계로 넘어갑니다.
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
