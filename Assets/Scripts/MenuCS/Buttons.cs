using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
public class Buttons : MonoBehaviour
{
    public Panels panels;
    public Button m_btnSettings;
    public Button m_btnSave;
    public Button m_btnLoad;
    void Start()
    {
        m_btnSettings.onClick.AddListener(onclickbtnSet);
        m_btnSave.onClick.AddListener(onclickbtnSave);
        m_btnLoad.onClick.AddListener(onclickbtnload);
    }
    void setsave()
    {
        panels.m_SettingPanel.saving();
    }
    void onclickbtnSet()
    {
        panels.m_SettingPanel.loading();
        if (panels.m_SettingPanel == true)
        {
            panels.m_SavePanel.gameObject.SetActive(false);
            panels.m_LoadPanel.gameObject.SetActive(false);
        }
        if (panels.m_SettingPanel != true)
            setsave();

        panels.m_SettingPanel.Active();
    }
    void onclickbtnSave()
    {
        setsave();
        if (panels.m_SavePanel == true)
        {
            panels.m_SettingPanel.gameObject.SetActive(false);
            panels.m_LoadPanel.gameObject.SetActive(false);
        }
        panels.m_SavePanel.gameObject.SetActive(true);

    }
    void onclickbtnload()
    {
        if (panels.m_LoadPanel == true)
        {
            panels.m_SavePanel.gameObject.SetActive(false);
            panels.m_SettingPanel.gameObject.SetActive(false);
        }
        panels.m_LoadPanel.gameObject.SetActive(true);

    }
    void TurnOFF()
    {
        panels.m_SavePanel.gameObject.SetActive(false);
        panels.m_SettingPanel.gameObject.SetActive(false);
        panels.m_LoadPanel.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TurnOFF();

        // M키: 모든 세이브 파일 초기화 후 슬롯 시간 표시를 00:00으로 되돌림
        if (Input.GetKeyDown(KeyCode.M))
        {
            SaveFileMgr.DeleteAll();
            panels.m_SavePanel.RefreshTimes();
            panels.m_LoadPanel.RefreshTimes();
        }
    }
}
