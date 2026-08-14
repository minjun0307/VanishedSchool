using System.Collections;
using UnityEngine;

public class MonsterScene : MonoBehaviour
{

    public MonsterFSM m_MonsterFSM = new MonsterFSM();
    public MonsterMoves m_MonsterMoves;

    [Header("Chase BGM (추격 상태 음악)")]
    public AudioSource m_ChaseBgm;   // 추격 음악 AudioSource (에디터에서 연결, Loop 체크 권장)

    [Header("Alert (경계 상태)")]
    [Tooltip("플레이어를 놓친 뒤 제자리에서 경계하는 시간(초). 이 시간이 지나면 배회로 돌아갑니다")]
    public float m_AlertTime = 3f;

    Coroutine m_AlertRoutine;   // 경계 → 배회 복귀 타이머 (중복 실행 방지용으로 들고 있습니다)

    void Awake()
    {
        GameMgr.Inst().m_MonsterScene = this;
    }

    void Start()  //몬스터의 추격시스템 1. 문열고 들어가면 그 주변으로 가기 2
    {
        m_MonsterFSM.Initialize(Callback_ReadyState, Callback_AlertState, Callback_ChasingState, Callback_PatrolState);
        m_MonsterFSM.SetReadyState();

    }
    void Callback_ReadyState()
    { //게임 시작하면 ready상태로 시작하고 2층에서 몬스터 배회    주변에서 달리기사용시 발소리듣고 경계
        if (m_MonsterMoves != null)
            m_MonsterMoves.m_IsActive = true;
        StopChaseBgm();   // 추격 상태가 아니므로 추격 음악 끄기
        m_MonsterFSM.SetPatrolState();   // 기본 상태: 층을 배회하며 플레이어 탐색
    }
    void Callback_PatrolState()
    {
        StopAlertRoutine();   // 배회로 돌아왔으니 경계 타이머는 더 필요 없음
        if (m_MonsterMoves != null)
            m_MonsterMoves.OnPatrol();
        StopChaseBgm();   // 추격에서 배회로 돌아오면 추격 음악 끄기
    }
    void Callback_AlertState() //경계하다 발견 -> 추격 > 아이템 아니면 무조건 추격성공
    {
        StopChaseBgm();   // 추격에서 경계로 바뀌면 추격 음악 끄기

        // 소화기 연막에 들어가 플레이어를 놓쳤을 때 이 상태로 들어옵니다.
        // 추격을 풀고 제자리에 멈춘 뒤, m_AlertTime초가 지나면 배회로 돌아갑니다.
        if (m_MonsterMoves != null)
            m_MonsterMoves.OnAlert();

        StopAlertRoutine();
        m_AlertRoutine = StartCoroutine(AlertRoutine());
    }
    void Callback_ChasingState()
    {
        StopAlertRoutine();   // 경계 도중 다시 발견했다면 복귀 타이머 취소
        if (m_MonsterMoves != null)
            m_MonsterMoves.OnChase();

        // 추격 상태 진입: 추격 음악 재생 (이미 재생 중이면 그대로 둠)
        if (m_ChaseBgm != null && !m_ChaseBgm.isPlaying)
            m_ChaseBgm.Play();
    }

    // 경계 시간이 지나면 배회 상태로 되돌립니다.
    IEnumerator AlertRoutine()
    {
        yield return new WaitForSeconds(m_AlertTime);

        m_AlertRoutine = null;   // 다 끝난 코루틴을 나중에 StopCoroutine하지 않도록 먼저 비움
        m_MonsterFSM.SetPatrolState();
    }

    void StopAlertRoutine()
    {
        if (m_AlertRoutine != null)
        {
            StopCoroutine(m_AlertRoutine);
            m_AlertRoutine = null;
        }
    }

    // 추격이 아닌 상태로 바뀔 때 추격 음악 정지 (사망 시 GameScene에서도 호출)
    public void StopChaseBgm()
    {
        if (m_ChaseBgm != null && m_ChaseBgm.isPlaying)
            m_ChaseBgm.Stop();
    }
    void Update()
    {
        if (m_MonsterFSM != null)
        {
            m_MonsterFSM.OnUpdate();
        }
    }

}
