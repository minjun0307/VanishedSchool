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
            TurnOFF();
            gameObject.SetActive(false);
            //m_monsterMoves.m_IsActive = true;
        }

    }
}

