using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 세이브 파일에 기록되는 데이터 (JsonUtility로 txt에 저장됨)
[System.Serializable]
public class SaveData
{
    public string savedTime;   // 저장한 시각 (참고용)
    public int playTime;       // 저장 시점까지 플레이한 시간 (초, 정수)

    // ── 플레이어 ──
    public int playerFloor;    // Floor enum 값 (0=F1, 1=F2, 2=F3)
    public float playerX;
    public float playerY;

    // ── 몬스터 ──
    public int monsterState;   // MonsterState enum 값 (0=Ready, 1=Patrol, 2=Alert, 3=Chase)
    public bool monsterActive; // 몬스터 활동 여부 (m_IsActive)
    public int monsterFloor;   // 몬스터가 배회 중인 층 (1~3)
    public float monsterX;
    public float monsterY;

    // ── 인벤토리 (AssetMgr) — 아이템 시스템이 생기면 그대로 사용 ──
    public List<string> inventory = new List<string>();
}

// SaveFile1.txt ~ SaveFile4.txt 파일 입출력을 전담하는 정적 클래스
public static class SaveFileMgr
{
    public const int SlotCount = 4;

    // 이번 세션에서 누적된 총 플레이시간 (초, 정수) — GameScene.Update가 AddPlayTime으로 더해줌
    public static int PlayTimeSec = 0;
    static float s_playTimeAccum = 0f;   // 정수를 유지하기 위한 1초 미만 누적 버퍼

    // 매 프레임 deltaTime을 누적하다가 1초가 채워질 때마다 PlayTimeSec(정수)에 더합니다.
    public static void AddPlayTime(float deltaSeconds)
    {
        s_playTimeAccum += deltaSeconds;
        if (s_playTimeAccum >= 1f)
        {
            int whole = (int)s_playTimeAccum;
            s_playTimeAccum -= whole;
            PlayTimeSec += whole;
        }
    }

    // 플레이시간을 0으로 초기화 (새 게임 시작 시)
    public static void ResetPlayTime()
    {
        PlayTimeSec = 0;
        s_playTimeAccum = 0f;
    }

    // Application.dataPath == 프로젝트의 Assets 폴더 (C:\Users\user\game2\Assets)
    public static string GetPath(int slotNum)
    {
        return Path.Combine(Application.dataPath, "SaveFile" + slotNum + ".txt");
    }

    public static void Save(int slotNum)
    {
        GameScene gameScene = GameMgr.Inst().m_GameScene;
        if (gameScene == null)
        {
            Debug.LogWarning("게임 씬이 아니어서 저장할 수 없습니다.");
            return;
        }

        SaveData data = new SaveData();
        data.savedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.playTime = PlayTimeSec;

        // 플레이어: 현재 층 + 위치
        PlayerMove player = gameScene.m_GameUI.m_Player;
        data.playerFloor = (int)player.CurrentFloor;
        data.playerX = player.transform.position.x;
        data.playerY = player.transform.position.y;

        // 몬스터: FSM 상태 + 활동 여부 + 층 + 위치
        MonsterScene monsterScene = GameMgr.Inst().m_MonsterScene;
        if (monsterScene != null && monsterScene.m_MonsterMoves != null)
        {
            MonsterMoves monster = monsterScene.m_MonsterMoves;
            data.monsterState = (int)monsterScene.m_MonsterFSM.GetCurStateType();
            data.monsterActive = monster.m_IsActive;
            data.monsterFloor = monster.m_OnFloor1 ? 1 : monster.m_OnFloor3 ? 3 : 2;
            data.monsterX = monster.transform.position.x;
            data.monsterY = monster.transform.position.y;
        }

        // 인벤토리 (수집한 아이템 목록)
        data.inventory = AssetMgr.Inst().GetInventoryForSave();

        File.WriteAllText(GetPath(slotNum), JsonUtility.ToJson(data, true));
        Debug.Log("저장 완료: " + GetPath(slotNum));
    }

    public static void Load(int slotNum)
    {
        GameScene gameScene = GameMgr.Inst().m_GameScene;
        if (gameScene == null)
        {
            Debug.LogWarning("게임 씬이 아니어서 불러올 수 없습니다.");
            return;
        }

        string path = GetPath(slotNum);
        if (!File.Exists(path))
        {
            Debug.LogWarning("세이브 파일이 없습니다: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("세이브 파일이 비어 있습니다: " + path);
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // 플레이시간 복원 (저장 시점부터 이어서 카운트)
        PlayTimeSec = data.playTime;
        s_playTimeAccum = 0f;

        // 플레이어: 저장된 층에 맞는 맵을 켜고 위치 복원 (방 나가기 로직과 같은 방식)
        PlayerMove player = gameScene.m_GameUI.m_Player;
        Floor floor = (Floor)data.playerFloor;
        player.SetFloor(floor);
        switch (floor)
        {
            case Floor.F1: gameScene.m_GameUI.m_Floor1.MainMapEnable(); break;
            case Floor.F2: gameScene.m_GameUI.m_Floor2.OnMap(); break;
            case Floor.F3: gameScene.m_GameUI.m_Floor3.MapEnable(); break;
        }
        player.transform.position = new Vector3(data.playerX, data.playerY, player.transform.position.z);

        // 몬스터: 층/위치/활동 여부를 되돌리고 FSM 상태 복원
        // (SetState로 상태를 넣으면 MonsterScene의 콜백이 실행되어 m_Chasing 등 플래그도 맞춰짐)
        MonsterScene monsterScene = GameMgr.Inst().m_MonsterScene;
        if (monsterScene != null && monsterScene.m_MonsterMoves != null)
        {
            MonsterMoves monster = monsterScene.m_MonsterMoves;
            monster.m_OnFloor1 = data.monsterFloor == 1;
            monster.m_OnFloor2 = data.monsterFloor == 2;
            monster.m_OnFloor3 = data.monsterFloor == 3;
            monster.m_IsActive = data.monsterActive;
            monster.transform.position = new Vector3(data.monsterX, data.monsterY, monster.transform.position.z);
            monsterScene.m_MonsterFSM.SetState((MonsterState)data.monsterState);
        }

        // 인벤토리 복원
        AssetMgr.Inst().SetInventoryFromLoad(data.inventory);

        // 사망 패널에서 로드한 경우: 게임 상태로 복귀
        // (GameState 진입 콜백이 플레이어 활성화/몬스터 FSM 초기화/패널 닫기/사운드 복원을 일괄 처리)
        if (gameScene.m_BattleFSM.IsResultState())
            gameScene.m_BattleFSM.SetGameState();

        Debug.Log("불러오기 완료: " + path);
    }

    // 슬롯에 표시할 플레이시간 문자열 — 저장된 파일이 없으면 "00:00"
    public static string GetSlotTimeText(int slotNum)
    {
        string path = GetPath(slotNum);
        if (!File.Exists(path))
            return "00:00";

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            return "00:00";

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        return FormatTime(data.playTime);
    }

    // 초(정수) → "분:초" 문자열 (예: 03:24)
    public static string FormatTime(int seconds)
    {
        return string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
    }

    // 모든 세이브 파일 삭제 (M키 초기화용)
    public static void DeleteAll()
    {
        for (int i = 1; i <= SlotCount; i++)
        {
            string path = GetPath(i);
            if (File.Exists(path))
                File.Delete(path);
            if (File.Exists(path + ".meta"))   // 에디터에서 생기는 meta 파일도 같이 정리
                File.Delete(path + ".meta");
        }
        Debug.Log("모든 세이브 파일을 초기화했습니다.");
    }
}
