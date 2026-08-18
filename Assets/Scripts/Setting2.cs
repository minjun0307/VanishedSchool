using UnityEngine;
using UnityEngine.UI;

public class Setting2 : MonoBehaviour
{
    public Button m_btnSettings;
    public Button m_btnSave;
    public Button m_btnLoad;
    public Button m_btnInventory;
    public Panels panels;

    private MonsterMoves m_monsterMoves;

    void Start()
    {
        m_monsterMoves = GameMgr.Inst().m_MonsterScene.m_MonsterMoves;

        m_btnSettings.onClick.AddListener(onclickbtnSet);
        m_btnSave.onClick.AddListener(onclickbtnSave);
        m_btnLoad.onClick.AddListener(onclickbtnload);
        m_btnInventory.onClick.AddListener(onclickbtnInven);

    }
    public void Activate()
    {
        gameObject.SetActive(true);
    }

    // 열려 있는 패널을 모두 닫고 메뉴 자체를 끕니다. (Escape 키, 캐비넷에 숨을 때 사용)
    public void Deactivate()
    {
        TurnOFF();
        gameObject.SetActive(false);
    }
    void onclickbtnInven()
    {
        PanelActiveInven();
    }
    
    void onclickbtnSet()
    {
        PanelActiveSett();
       

    }
    void onclickbtnSave()
    {
        PanelActiveSave();
    }
    void onclickbtnload()
    {
        PanelActiveLoad();
    }
    void PanelActiveSett()
    {
        if (panels.m_SettingPanel == true)
        {
            panels.m_SavePanel.gameObject.SetActive(false);
            panels.m_LoadPanel.gameObject.SetActive(false);
            panels.m_InvenPanel.gameObject.SetActive(false);
        }
        panels.m_SettingPanel.gameObject.SetActive(true);
    }
    void PanelActiveInven()
    {
        if (panels.m_InvenPanel == true)
        {
            panels.m_SavePanel.gameObject.SetActive(false);
            panels.m_LoadPanel.gameObject.SetActive(false);
            panels.m_SettingPanel.gameObject.SetActive(false);
        }
        panels.m_InvenPanel.gameObject.SetActive(true);
    }
  
    void PanelActiveSave()
    {
        if (panels.m_SavePanel == true)
        {
            panels.m_InvenPanel.gameObject.SetActive(false);
            panels.m_LoadPanel.gameObject.SetActive(false);
            panels.m_SettingPanel.gameObject.SetActive(false);
        }
        panels.m_SavePanel.gameObject.SetActive(true);
    }
   
    void PanelActiveLoad()
    {
        if (panels.m_LoadPanel == true)
        {
            panels.m_SavePanel.gameObject.SetActive(false);
            panels.m_InvenPanel.gameObject.SetActive(false);
            panels.m_SettingPanel.gameObject.SetActive(false);
        }
        panels.m_LoadPanel.gameObject.SetActive(true);
    }
    
    void TurnOFF()
    {
        panels.m_SavePanel.gameObject.SetActive(false);
        panels.m_SettingPanel.gameObject.SetActive(false);
        panels.m_LoadPanel.gameObject.SetActive(false);
        panels.m_InvenPanel.gameObject.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Deactivate();
            m_monsterMoves.m_IsActive = true;
        }

    }
}

