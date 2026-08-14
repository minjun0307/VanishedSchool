using UnityEngine;

public class GameMgr 
{
     static GameMgr inst = new GameMgr();
    
    //최우선 만들거 : 몬스터 움직임, 몬스터 감지섹션 구축
    //만들거 또 : 옵젝폴링으로 아이템, 맵내에 방시스템(이건 좀 걸릴듯)
    //만들거 : 에셋매니저 (아이템매니징)
    public static GameMgr Inst()
    {
        if(inst == null)
            inst = new GameMgr();

        return inst;
    }
    public Menu m_TempMenu;
    public GameScene m_GameScene ;
    public MonsterScene m_MonsterScene;
    public ItemSpawner m_ItemSpawner;   // 맵 아이템 스포너 (세이브/로드에서 맵 아이템을 복원할 때 사용)
    public FogMgr m_FogMgr;             // 소화기 연막 관리자 (몬스터가 감지 차단 판정에 사용)

}
