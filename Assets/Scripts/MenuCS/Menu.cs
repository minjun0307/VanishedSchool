using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public Button m_btnStart;
    public Button m_btnSettings;
    public Button m_btnQuit;      // 게임 실행 종료 버튼
    public Settings Setting;
    
    void Awake()
    {
        GameMgr.Inst().m_TempMenu = this;
    }

    void Start()
    {
        m_btnSettings.onClick.AddListener(onclickbtnSets);
        m_btnStart.onClick.AddListener(onclickbtnStart);
        m_btnQuit.onClick.AddListener(onclickbtnQuit);
    }
    void onclickbtnStart()
    {
        SaveFileMgr.ResetPlayTime();   // 새 게임은 플레이시간 00:00부터
        SceneManager.LoadScene("GameScene");
    }
    void onclickbtnSets()
    {
        Setting.Activate();
    }
    void onclickbtnQuit()
    {
        Application.Quit();   // 빌드에서 게임 종료 (에디터에서는 동작 안 함)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // 에디터 테스트용: 플레이 모드 정지
#endif
    }
    void Update()
    {
        
    }
}
