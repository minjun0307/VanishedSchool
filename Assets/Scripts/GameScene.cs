using JetBrains.Annotations;
using UnityEngine;

public class GameScene : MonoBehaviour
{
    public BattleFSM m_BattleFSM = new BattleFSM();
    public HudUI m_HudUI;
    public GameUI m_GameUI;
    public DeathPanel m_DeathPanel;   // 사망 시 켜줄 패널 (에디터에서 연결)

    void Awake()
    {
        GameMgr.Inst().m_GameScene = this;
    }

    void Start()
    {
        m_GameUI.m_Player.SetFloor(Floor.F1);
        m_BattleFSM.SetReadyState();
        m_BattleFSM.Initialize(Callback_ReadyState, Callback_GameState, Callback_ResultState);
    }
    void Callback_ReadyState()
    {
        m_GameUI.m_Player.m_IsActive = true;
        Debug.Log("Enter GameReadyState");
        Invoke("Callback_GameState", 3);
    }
    void Callback_GameState()
    {
        // 게임(재개) 상태 진입: 사망 → 로드 후 복귀를 포함해 모두 초기 상태로 되돌림
        m_GameUI.m_Player.m_IsActive = true;   // 플레이어 움직임 활성화

        // 몬스터 FSM을 초기 상태(Ready → Patrol)로
        MonsterScene monsterScene = GameMgr.Inst().m_MonsterScene;
        if (monsterScene != null)
            monsterScene.m_MonsterFSM.SetReadyState();

        // 사망 패널(과 사망 패널이 열어둔 로드 패널) 닫기
        if (m_DeathPanel != null)
        {
            if (m_DeathPanel.m_LoadPanel != null)
                m_DeathPanel.m_LoadPanel.gameObject.SetActive(false);
            m_DeathPanel.gameObject.SetActive(false);
        }

        // BGM/SFX는 HudUI에 저장된 설정(토글/볼륨)대로 복원
        if (m_HudUI != null)
            m_HudUI.RestoreVolumes();
    }
    void Callback_ResultState()
    {
        // 플레이어 사망: 조작 정지, 몬스터 정지, 사망 패널 표시
        m_GameUI.m_Player.m_IsActive = false;

        MonsterScene monsterScene = GameMgr.Inst().m_MonsterScene;
        if (monsterScene != null)
        {
            if (monsterScene.m_MonsterMoves != null)
                monsterScene.m_MonsterMoves.m_IsActive = false;
            monsterScene.StopChaseBgm();   // 추격 음악 끄기
        }

        // 사망 패널에 방해되지 않도록 모든 BGM/SFX 음소거 (저장된 설정은 유지)
        if (m_HudUI != null)
            m_HudUI.MuteAll();

        if (m_DeathPanel != null)
            m_DeathPanel.gameObject.SetActive(true);
    }
    void Update()
    {
        if (m_BattleFSM != null)
            m_BattleFSM.OnUpdate();

        // 플레이시간 누적 (저장 시 SaveData.playTime으로 기록됨 — 정수 초 단위)
        SaveFileMgr.AddPlayTime(Time.deltaTime);
    }
}
