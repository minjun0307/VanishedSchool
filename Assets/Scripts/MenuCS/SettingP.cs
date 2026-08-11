using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SettingP : MonoBehaviour
{
    public Toggle m_BGMTog;
    public Toggle m_SFXTog;
    public Text m_BgmTxt;
    public Text m_SfxTxt;
    public List<string> m_Res = new List<string>(); 
    public int m_ResIndex = 0;
    public Text m_Restext;
    
    void Start()
    {
        
        m_BGMTog.onValueChanged.AddListener(SettovcBgmTog);
        m_SFXTog.onValueChanged.AddListener(SettovcSfxTog);

        SettovcBgmTog(true);
        SettovcSfxTog(true);

        // 해상도 목록은 SetovcResDrop의 인덱스 처리와 순서가 반드시 일치해야 하므로
        // 인스펙터가 아닌 코드에서 직접 채웁니다. (0: 1920x1080, 1: 1440x1080, 2: 1280x720)

    }
    public void Active()
    {
        gameObject.SetActive(true); 
    }
    void SetovcResDrop(int index)
    {
        // 이 함수에서 해상도 변경로직 x y 값 위치 바꿔
        m_ResIndex = index;
        if (m_ResIndex == 0)
        {
            
            Debug.Log("reso0");
            Screen.SetResolution(1920, 1080, true);
            m_Restext.text = "Your current resolution is 1920 x 1080 ";
        }
        else if (m_ResIndex == 1)
        {
            Debug.Log("reso1");
            Screen.SetResolution(1440, 1080, false);
            m_Restext.text = "Your current resolution is 1440 x 1080 ";
        }
        else if (m_ResIndex == 2)
        {
            Debug.Log("reso2");
            Screen.SetResolution(1280, 720, false);
            m_Restext.text = "Your current resolution is 1280 x 720";
        }

    }
    void SettovcSfxTog(bool isOn)
    {
        if (m_SFXTog.isOn)
        {
            m_SfxTxt.text = "SFX ON";
        }
        else
        {
            m_SfxTxt.text = "SFX OFF";
        }
    }
    void SettovcBgmTog(bool isOn)
    {
        if (m_BGMTog.isOn)
        {
            m_BgmTxt.text = "BGM ON";
        }
        else
        {
            m_BgmTxt.text = "BGM OFF";
        }
    }
    public void saving()
    {
        int bgm = 0;
        int sfx = 0;
        if (m_SFXTog.isOn) sfx = 1;
        if (m_BGMTog.isOn) bgm = 1;
        PlayerPrefs.SetInt("bgm", bgm);
        PlayerPrefs.SetInt("sfx", sfx);
    }
    public void loading()
    {
        
        int bgm = PlayerPrefs.GetInt("bgm");
        int sfx = PlayerPrefs.GetInt("sfx");

        bool bBgm = bgm == 1 ? true : false;
        if (bgm == 1) m_BgmTxt.text = "BGM ON";
        
        if (bgm == 0)
        {
            m_BgmTxt.text = "BGM OFF";
            m_BGMTog.isOn = false;
        }
        bool bSfx = sfx == 1 ? true : false;
        if (sfx == 1)
        {
            m_SfxTxt.text = "SFX ON";
        }
        if(sfx == 0)
        {
            m_SfxTxt.text = "SFX OFF";
            m_SFXTog.isOn = false;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
