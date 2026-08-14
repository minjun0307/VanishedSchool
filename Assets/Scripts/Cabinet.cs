using UnityEngine;

// 캐비넷 숨기 상호작용 (GameScene의 Cabinet 오브젝트에 부착)
// - 상호작용 범위는 자식 오브젝트 CabinetTrigger의 Is Trigger 콜라이더입니다.
//   아이템과 마찬가지로 범위 판정 콜라이더는 이쪽에 두고, 진입/이탈 감지와
//   안내 문구 "[F] 캐비넷에 숨기" 표시, F키 입력은 PlayerInventory가 함께 처리합니다.
// - 숨는 동안 플레이어는 무적이 되고 모습이 사라집니다.
// - 추격 중인 몬스터가 m_CaughtZone(자식 CaughtRange의 Box Collider 2D) 안에 들어와 있을 때
//   숨으면 들켜서 즉사합니다. 들키는 범위는 그 콜라이더의 크기/위치로 조절합니다.
public class Cabinet : MonoBehaviour
{
    [Tooltip("이 캐비넷이 있는 층 (1~3) — 몬스터가 찾아올 때 사용합니다.")]
    public int m_Floor = 1;

    [Tooltip("들키는 범위 콜라이더(자식 CaughtRange의 Box Collider 2D). 추격 중인 몬스터가 이 안에 있을 때 숨으면 즉사합니다.")]
    public Collider2D m_CaughtZone;

    // 지금 플레이어가 숨어 있는 캐비넷 (숨어 있지 않으면 null)
    public static Cabinet Hiding { get; private set; }

    SpriteRenderer m_PlayerSprite;   // 숨는 동안 꺼둘 플레이어 스프라이트
    Collider2D m_PlayerCollider;     // 숨는 동안 꺼둘 플레이어 충돌체 (몬스터가 밀치지 않도록)

    // 이 콜라이더가 '들키는 범위'인지 확인합니다.
    // 들키는 범위도 캐비넷의 자식이라, 상호작용 범위(CabinetTrigger)와 구분하기 위해 필요합니다.
    public bool IsCaughtZone(Collider2D col)
    {
        return m_CaughtZone != null && col == m_CaughtZone;
    }

    // 씬을 다시 시작해도 static 값이 남지 않도록 초기화합니다.
    void Awake()
    {
        Hiding = null;
    }

    // 캐비넷에 숨기 — 몬스터에게 이미 코앞까지 붙잡혔으면 대신 사망 처리합니다.
    public void Hide(PlayerMove player)
    {
        if (player == null || Hiding != null)
            return;

        PlayerStats stats = player.GetComponent<PlayerStats>();
        MonsterMoves monster = GetMonster();

        // 추격 중인 몬스터가 들키는 범위 콜라이더 안에 있으면, 숨는 모습을 들켜 그 자리에서 죽습니다.
        if (stats != null && monster != null && monster.m_Chasing && IsMonsterInCaughtZone(monster))
        {
            stats.TakeDamage(stats.Hp);   // HP를 0으로 만들어 기존 사망 처리(사망 패널)로 넘깁니다
            return;
        }

        bool wasChasing = monster != null && monster.m_Chasing;

        Hiding = this;
        player.m_IsActive = false;
        if (stats != null)
            stats.m_Invincible = true;

        m_PlayerSprite = player.GetComponent<SpriteRenderer>();
        if (m_PlayerSprite != null)
            m_PlayerSprite.enabled = false;

        m_PlayerCollider = player.GetComponent<Collider2D>();
        if (m_PlayerCollider != null)
            m_PlayerCollider.enabled = false;

        // 인벤토리를 열어둔 채로 숨었다면 함께 닫습니다.
        // (숨어 있는 동안에는 B키로 다시 열 수도 없으므로 아이템 장착/사용이 완전히 막힙니다)
        GameScene gameScene = GameMgr.Inst().m_GameScene;
        if (gameScene != null && gameScene.m_HudUI != null)
            gameScene.m_HudUI.CloseMenu();

        // 쫓기던 중에 숨는 데 성공했다면: 몬스터는 추격을 포기하고(Patrol) 캐비넷까지 찾아왔다가
        // 다른 층으로 떠납니다. 쫓기던 중이 아니었다면 몬스터는 하던 배회를 그대로 계속합니다.
        if (wasChasing)
            SendMonsterToCabinet(true);
    }

    // 캐비넷에서 나오기 — 숨기 전 상태로 되돌리고, 몬스터가 이 위치를 확인하러 오게 합니다.
    public void Exit(PlayerMove player)
    {
        if (Hiding != this)
            return;

        Hiding = null;

        if (m_PlayerSprite != null)
        {
            m_PlayerSprite.enabled = true;
            m_PlayerSprite = null;
        }
        if (m_PlayerCollider != null)
        {
            m_PlayerCollider.enabled = true;
            m_PlayerCollider = null;
        }

        if (player != null)
        {
            player.m_IsActive = true;

            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null)
                stats.m_Invincible = false;

            // 숨는 동안 콜라이더를 꺼둔 탓에 트리거 이탈로 처리됐을 수 있으므로,
            // 아직 캐비넷 앞에 서 있다는 사실을 다시 알려 안내 문구가 이어지게 합니다.
            PlayerInventory inven = player.GetComponent<PlayerInventory>();
            if (inven != null)
                inven.SetNearCabinet(this, true);
        }

        SendMonsterToCabinet(false);
    }

    // 몬스터를 Patrol 상태로 바꾸고 이 캐비넷 위치를 수색하게 합니다.
    // leaveAfter가 true면 수색을 마친 뒤 위/아래 다른 층으로 떠납니다.
    void SendMonsterToCabinet(bool leaveAfter)
    {
        MonsterScene scene = GameMgr.Inst().m_MonsterScene;
        if (scene == null || scene.m_MonsterMoves == null)
            return;

        scene.m_MonsterFSM.SetPatrolState();
        scene.m_MonsterMoves.SearchAt(transform.position, m_Floor, leaveAfter);
    }

    // 몬스터가 들키는 범위 콜라이더 안에 들어와 있는지 확인합니다.
    // 몬스터 몸통 콜라이더가 범위와 겹치면(스쳐도) 들킨 것으로 봅니다.
    // 레이어 충돌 설정이나 Is Trigger 여부에 영향받지 않도록 콜라이더의 영역(bounds)으로 비교합니다.
    bool IsMonsterInCaughtZone(MonsterMoves monster)
    {
        if (m_CaughtZone == null)
            return false;   // 범위를 지정하지 않았으면 들키지 않습니다

        Collider2D monsterCol = monster.GetComponent<Collider2D>();
        if (monsterCol != null)
            return m_CaughtZone.bounds.Intersects(monsterCol.bounds);

        return m_CaughtZone.OverlapPoint(monster.transform.position);
    }

    MonsterMoves GetMonster()
    {
        MonsterScene scene = GameMgr.Inst().m_MonsterScene;
        return scene != null ? scene.m_MonsterMoves : null;
    }
}
