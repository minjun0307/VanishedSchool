using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Floor { F1, F2, F3 }
public enum Room  { None, MedicRoom, F1C1, F1C2, Utillity,
                    TeacherRoom, PrincipalRoom, F2C1, F2C2, ToiletMale, ToiletWomen,
                    F3C1, F3C2, Davinchi, LockedRoom, RuinedRoom, ItemRoom, BroadCast }

public class PlayerMove : MonoBehaviour
{
    public float m_Speed = 4f;
    public float runSpeed = 6f;
    float slowSpeed = 1.5f; // 컨트롤 키 누를 때의 속도
    float runTransitionTime = 0.8f;
    private float defaultSpeed;
    private Animator m_Animator;

    Rigidbody2D m_rb;
    Vector2 vector;   //getaxis를 활용하여 0 좌 1 우 이걸 받아주는 역할
    public bool m_AnimActive = false;
    public bool m_IsActive = false;

    private float lastHorizontal;
    private float lastVertical;
    private bool isHorizontalLast;
    private BoxCollider2D Boxcollider2d;
    public LayerMask layerMask;

    // ── 방/층 상태: enum 으로 통합 ──
    public Floor CurrentFloor { get; private set; } = Floor.F1;
    public Room  CurrentRoom  { get; private set; } = Room.None;
    private IRoom _activeRoom;  // 현재 들어가 있는 방 참조
    private Collider2D _entranceCollider;  // 입구 콜라이더 참조 (나갈 때 일시 비활성용)

    // 방에 들어가기 전 원래 위치를 저장할 변수입니다.
    public Vector3 oldLocation;
    public Vector3 PreviousStairLocation;

    private Floor1 m_floor1;
    private Floor2 m_floor2;
    private Floor3 m_floor3;
    private HudUI m_hudUI;
    private PlayerStats m_Stats;   // 스테미나 확인용 (같은 오브젝트의 PlayerStats)
    private CrumbTrail m_CrumbTrail;   // 몬스터가 따라오는 과자 흔적 (같은 오브젝트의 CrumbTrail)

    void Start()
    {
        m_floor1 = GameMgr.Inst().m_GameScene.m_GameUI.m_Floor1;
        m_floor2 = GameMgr.Inst().m_GameScene.m_GameUI.m_Floor2;
        m_floor3 = GameMgr.Inst().m_GameScene.m_GameUI.m_Floor3;
        m_hudUI = GameMgr.Inst().m_GameScene.m_HudUI;

        Boxcollider2d = GetComponent<BoxCollider2D>();
        m_Stats = GetComponent<PlayerStats>();
        m_CrumbTrail = GetComponent<CrumbTrail>();
        m_Animator = GetComponent<Animator>();
        m_rb = GetComponent<Rigidbody2D>();
        m_rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 물리 프레임과 렌더 프레임의 차이를 보간하여 떨림(Jitter) 방지
        defaultSpeed = m_Speed; // 초기 속도를 default로 저장합니다.
    }
    public void startcor()
    {

    }
    // Shift를 누른 채 실제로 이동 중인지 (달리기 — PlayerStats의 스테미나 소모 판정용)
    public bool IsRunning { get; private set; }

    void Update()
    {
        if (m_IsActive)
            Playermovement();
        else
            IsRunning = false;   // 조작 불가(페이드 등) 중에는 달리기 아님
    }
    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    if (collision.gameObject.tag == "Stairs")
    //    {
    //        SceneManager.LoadScene("2FloorScene");

    //    }

    //}
    public void Playermovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");


        // 최근에 어떤 축이 새로 입력되었는지(0에서 0이 아닌 값) 파악
        if (h != 0 && lastHorizontal == 0)
        {
            isHorizontalLast = true;
        }
        else if (v != 0 && lastVertical == 0)
        {
            isHorizontalLast = false;
        }

        // 이전 프레임의 입력값을 저장해둠
        lastHorizontal = h;
        lastVertical = v;
        // 두 축이 모두 입력 중일 때(대각선 입력 시도), 가장 최근에 눌린 축만 우선시함
        if (h != 0 && v != 0)
        {
            if (isHorizontalLast)
            {
                v = 0; // 수평이 나중에 눌렸다면 수직 이동 무시
            }
            else
            {
                h = 0; // 수직이 나중에 눌렸다면 수평 이동 무시
            }
        }




        vector.x = h;
        vector.y = v;
        vector = vector.normalized;

        //dirX에서 할당되는 -1, 1을 axisRaw가 할당된 벡터의 x값으로 받겠다 이런말
        if (vector != Vector2.zero)
        {
            m_Animator.SetFloat("DirX", vector.x);
            m_Animator.SetFloat("DirY", vector.y);
        }

        // RaycastHit2D hit;
        // a, b지점
        // b지점에 레이저로 쏨 > 레이저에 b지점에 레이저 맞음 = null 호출 ,
        // b지점사이에 장애물(벽)에 레이저 맞음 = 장애물 호출 벽에 맞앗어용 호출)

        Vector2 start = transform.position; //a 지점 : 캐릭터의 현재 위치
        Vector2 end = start + new Vector2(vector.x * m_Speed, vector.y * m_Speed);  //b 지점 : 캐릭터가 이동하고자 하는 위치값

        // Boxcollider2d.enabled = false;
        // hit = Physics2D.Linecast(start, end, layerMask); //하지만 저걸하면 캐릭터의 boxcollider에 레이저쏴서 개 ㅈㄹ남
        // Boxcollider2d.enabled = true;




        m_Animator.SetBool("Walking", vector != Vector2.zero);

        float targetSpeed = defaultSpeed;

        // 스테미나가 0이면 달리기 불가 (Shift를 눌러도 기본 속도)
        bool canRun = m_Stats == null || m_Stats.Str > 0;

        // Shift 키를 누르면 달리기 속도, Left Control 키를 누르면 걷기(느린) 속도
        if (Input.GetKey(KeyCode.LeftShift) && canRun)
        {
            targetSpeed = runSpeed;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            targetSpeed = slowSpeed;
        }

        // 달리기 판정: Shift를 누른 채 실제로 이동 입력이 있을 때 (스테미나 소모 조건)
        IsRunning = Input.GetKey(KeyCode.LeftShift) && canRun && vector != Vector2.zero;
        // 목표 속도 설정 (기본은 defaultSpeed)


        // (목표속도 - 기본속도) / 걸리는시간 = 초당 변화량
        // 여기서는 가장 큰 변화폭인 (runSpeed - slowSpeed)를 기준으로 계산할 수도 있지만,
        // 기존처럼 (runSpeed - defaultSpeed) 비율로 통일하거나 직접 지정할 수 있습니다.
        float speedChangeRate = Mathf.Abs(runSpeed - defaultSpeed) / runTransitionTime;

        // 현재 속도에서 목표 속도까지 초당 변화량에 따라 서서히 변경
        m_Speed = Mathf.MoveTowards(m_Speed, targetSpeed, speedChangeRate * Time.deltaTime);
    }

    // ── 방 상태 관리 ──
    public void EnterRoom(Room room, Floor floor)
    {
        CurrentRoom = room;
        CurrentFloor = floor;
    }

    public void ExitRoom()
    {
        CurrentRoom = Room.None;

        // 방문을 열고 나오는 소리 — 몬스터에게 '몇 층에서 소리가 났는지'만 알려줍니다.
        // 몬스터는 그 층으로 찾아와 한 바퀴 수색합니다. (Floor.F1 → 1, F2 → 2, F3 → 3)
        MonsterScene monsterScene = GameMgr.Inst().m_MonsterScene;
        if (monsterScene != null)
            monsterScene.NoticeFloor((int)CurrentFloor + 1);
    }

    /// <summary>
    /// 외부에서 초기 층을 설정할 때 사용 (예: GameScene.Start)
    /// </summary>
    public void SetFloor(Floor floor)
    {
        CurrentFloor = floor;
    }

    private void OnCollisionEnter2D(Collision2D collision) // 방이나 포탈, 계단 등을 탈 때 현재(이동 전) 위치를 oldLocation에 저장합니다.
    {
        Collider2D col = collision.collider;

        if (m_hudUI.IsFading)   // 화면 전환 중에는 새 이동을 받지 않음
            return;

        // ── 방 진입: IRoom 인터페이스로 일괄 처리 ──
        if (col.TryGetComponent<IRoom>(out var room))
        {
            Room prevRoom = CurrentRoom;
            Vector3 target = room.Enter(this);
            if (CurrentRoom == prevRoom)    // 입장 실패(잠긴 방 등)면 이동/페이드 없음
                return;

            // 입구 콜라이더의 Spot 자식 위치 저장 (나갈 때 복귀용)
            Transform spot = col.transform.Find("Spot");
            oldLocation = spot != null ? spot.position : col.transform.position;
            _entranceCollider = col;
            _activeRoom = room;
            FadeTeleport(() => transform.position = target);
        }
        // ── 방에서 나가기 ──
        else if (col.CompareTag("Doors"))
        {
            FadeTeleport(() =>
            {
                if (_activeRoom != null)
                {
                    _activeRoom.Exit(this);
                    _activeRoom = null;
                }

                // 층별 맵 복원
                switch (CurrentFloor)
                {
                    case Floor.F1: m_floor1.MainMapEnable(); break;
                    case Floor.F2: m_floor2.OnMap(); break;
                    case Floor.F3: m_floor3.MapEnable(); break;
                }
                RestoreLocation();

                // 입구 콜라이더를 1초간 비활성화하여 복귀 시 재충돌 방지
                if (_entranceCollider != null)
                {
                    StartCoroutine(DisableEntranceTemporarily(_entranceCollider));
                    _entranceCollider = null;
                }
            });
        }
        // ── 계단: 올라가기 ──
        else if (col.CompareTag("Stairs")) //올라가는코드
        {
            FadeTeleport(() =>
            {
                if (CurrentFloor == Floor.F1)    //1층에서 올라가유~ -> 2층가유~|  2층에서 올라가유~ > 3층가유~
                {
                    f2Pos();
                    PreviousStairLocation = FindStairSpot(col, "F1Spot", PreviousStairLocation);
                }
                else if (CurrentFloor == Floor.F2)
                {
                    f3Pos();
                    PreviousStairLocation = FindStairSpot(col, "F2SpotUP", PreviousStairLocation);
                }


                /* else if(m_ItsFloor2)*/
                //floor.F3Pos();
                //GameMgr.Inst().m_GameScene.m_GameUI.m_Floor2.m_Stair2.m_IsActive = true;
            });
        }
        // ── 계단: 내려가기 ──
        else if (col.CompareTag("Stairs2")) //내려가는 코드
        {
            FadeTeleport(() =>
            {
                gameObject.transform.position = PreviousStairLocation;
                if (CurrentFloor == Floor.F2)
                {
                    f1Pos();
                    PreviousStairLocation = FindStairSpot(col, "F2SpotD", PreviousStairLocation);
                }else if (CurrentFloor == Floor.F3)
                {
                    f2Pos();
                    //Vector3 v = col.transform.Find("F2SpotUP").position;
                    //gameObject.transform.position = v;
                    PreviousStairLocation = FindStairSpot(col, "F3SpotD", PreviousStairLocation);
                }


                //GameMgr.Inst().m_GameScene.m_GameUI.m_Floor2.m_Stair2.m_IsActive = true;
            });
        }
        else if (col.CompareTag("OtherStair")) //올라가는 코드 (우측 계단: 1층→2층, 2층→3층)
        {
            FadeTeleport(() =>
            {
                if (CurrentFloor == Floor.F1)    // 1층 우측 계단 → 2층 (F2Spot2 도착)
                {
                    f2Pos2();
                    PreviousStairLocation = FindStairSpot(col, "F1SpotD", PreviousStairLocation);
                }
                else if (CurrentFloor == Floor.F2)    // 2층 우측 계단 → 3층 (F3Spot2 도착)
                {
                    f3Pos2();
                    PreviousStairLocation = FindStairSpot(col, "F2SpotUp2", PreviousStairLocation);
                }
                //if (CurrentFloor == Floor.F2)
                //{
                //    f3Pos2();
                //    PreviousStairLocation = col.transform.Find("F2Spot2").position;

                //}
            });
        }
        else if (col.CompareTag("OtherStair2")) //내려가는 코드 (우측 계단: 2층→1층, 3층→2층)
        {
            FadeTeleport(() =>
            {
                if (CurrentFloor == Floor.F2)    // 2층 우측 계단 → 1층 (F1SpotD2 도착)
                {
                    f1Pos2();
                    PreviousStairLocation = FindStairSpot(col, "F2SpotD2", PreviousStairLocation);
                }
                else if (CurrentFloor == Floor.F3)    // 3층 우측 계단 → 2층 (F2Spot3 도착)
                {
                    f2Pos3();
                    PreviousStairLocation = FindStairSpot(col, "F3SpotD2", PreviousStairLocation);
                }
            });
        }
    }

    // 화면이 검게 덮인 사이에 이동(move)을 실행하고, 전환이 끝날 때까지 조작을 잠급니다.
    void FadeTeleport(System.Action move)
    {
        bool wasActive = m_IsActive;
        m_IsActive = false;
        vector = Vector2.zero;
        m_Animator.SetBool("Walking", false);
        m_hudUI.FadeTransition(move, () =>
        {
            m_IsActive = wasActive;

            // 방/계단 이동은 좌표가 순간이동하므로, 여기까지 남아 있던 과자 흔적을 끊습니다.
            // (그대로 두면 몬스터가 벽을 관통하는 직선을 따라가려 합니다)
            if (m_CrumbTrail != null)
                m_CrumbTrail.Clear();
        });
    }

    // 계단 콜라이더의 자식 Spot 위치를 안전하게 가져옵니다.
    // Spot이 없으면 NullReferenceException 대신 경고를 찍고 fallback(기존 저장값)을 반환해
    // 페이드 코루틴이 죽지 않게 합니다.
    Vector3 FindStairSpot(Collider2D col, string spotName, Vector3 fallback)
    {
        Transform spot = col.transform.Find(spotName);
        if (spot != null)
            return spot.position;

        Debug.LogWarning("계단 '" + col.name + "'에 자식 Spot '" + spotName + "'이(가) 없습니다. 이전 저장 위치를 그대로 사용합니다.", col);
        return fallback;
    }
    public void f3Pos()
    {
        CurrentFloor = Floor.F3;
        m_floor2.m_IsWayUp = true;
        m_floor3.MapEnable();
        Vector3 v = m_floor3.F3Pos();
        gameObject.transform.position = v;
        PreviousStairLocation = transform.position;

    }
    public void f3Pos2()
    {
        CurrentFloor = Floor.F3;
        m_floor3.MapEnable();
        Vector3 v = m_floor3.F3Pos2();
        gameObject.transform.position = v;
        PreviousStairLocation = transform.position;

    }
    public void f2Pos()
    {
        Vector3 v = m_floor2.F2Pos();
        gameObject.transform.position = v;
        PreviousStairLocation = transform.position;
        //if(m_floor2.m_IsWayUp)
        //    PreviousStairLocation = transform.position;
        //else
        //{
        //    Vector3 v = m_floor2.F2Pos();
        //    gameObject.transform.position = v;
        //}

        CurrentFloor = Floor.F2;
    }
    public void f2Pos2()
    {
        Vector3 v = m_floor2.F2Pos2();
        gameObject.transform.position = v;
        PreviousStairLocation = transform.position;
        CurrentFloor = Floor.F2;
    }
    public void f2Pos3()
    {
        Vector3 v = m_floor2.F2Pos3();
        gameObject.transform.position = v;
        PreviousStairLocation = transform.position;
        CurrentFloor = Floor.F2;
    }
    public void f1Pos()
    {
        PreviousStairLocation = transform.position;
        Vector3 v = m_floor1.F1Pos();
        gameObject.transform.position = v;
        CurrentFloor = Floor.F1;
    }
    public void f1Pos2()
    {
        Vector3 v = m_floor1.F1Pos2();
        gameObject.transform.position = v;
        PreviousStairLocation = transform.position;
        CurrentFloor = Floor.F1;
    }

    // 다시 방 밖으로 나갈 때 호출하면 저장했던 원래 위치로 캐릭터를 이동시킵니다.
    public void RestoreLocation()
    {
        transform.position = oldLocation;
    }
    public void RestoreLocation2()
    {
        transform.position = PreviousStairLocation;
    }

    void FixedUpdate()
    {
        if (m_IsActive == true)
            m_rb.MovePosition(m_rb.position + vector * m_Speed * Time.fixedDeltaTime);
    }

    // 입구 콜라이더를 1초간 비활성화했다가 다시 활성화하는 코루틴
    private IEnumerator DisableEntranceTemporarily(Collider2D entrance)
    {
        entrance.enabled = false;
        yield return new WaitForSeconds(1f);
        entrance.enabled = true;
    }
}
