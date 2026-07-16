using Unity.VisualScripting;
using UnityEngine;


using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class HudUI : MonoBehaviour
{
    public Setting2 m_Setts;
    //public Button m_btngo;

    private MonsterMoves m_monsterMoves;

    // ── BGM / SFX 볼륨 조절 ──
    [Header("Volume Settings")]
    public Slider m_BgmSlider;
    public Slider m_SfxSlider;
    public Text m_BgmVolTxt;
    public Text m_SfxVolTxt;
    public Toggle m_BgmTog;              // BGM 켜기/끄기 토글
    public Toggle m_SfxTog;              // SFX 켜기/끄기 토글
    public Text m_BgmTogTxt;             // "BGM ON" / "BGM OFF" 표시 텍스트
    public Text m_SfxTogTxt;             // "SFX ON" / "SFX OFF" 표시 텍스트
    public AudioSource m_BgmSource;      // BGM을 재생하는 AudioSource (없으면 비워둬도 됨)
    public AudioSource[] m_SfxSources;   // 효과음을 재생하는 AudioSource들

    // ── 화면 페이드 전환 ──
    public float m_FadeTime = 0.25f;  // 어두워지는 데 걸리는 시간 (밝아질 때도 동일)
    public float m_HoldTime = 0.15f;  // 완전히 검은 상태를 유지하는 시간

    Image m_FadeImage;
    bool m_IsFading;

    public bool IsFading { get { return m_IsFading; } }

    void Awake()
    {
        CreateFadeImage();
        // OnEnable(로드)보다 먼저 슬라이더 범위/리스너가 준비되어야 하므로 Awake에서 초기화합니다.
        InitVolumeSliders();
    }

    // UI가 켜질 때: 저장된 볼륨/토글 값을 불러와 슬라이더 위치, 토글 상태, 볼륨, 텍스트에 반영합니다.
    // 저장된 값이 없으면 볼륨 100, 토글 ON을 사용합니다.
    void OnEnable()
    {
        m_BgmSlider.value = PlayerPrefs.GetInt("BgmVolume", 100);
        m_SfxSlider.value = PlayerPrefs.GetInt("SfxVolume", 100);
        m_BgmTog.isOn = PlayerPrefs.GetInt("BgmOn", 1) == 1;
        m_SfxTog.isOn = PlayerPrefs.GetInt("SfxOn", 1) == 1;
        // 값이 이미 같으면 onValueChanged가 호출되지 않으므로 한 번 직접 갱신합니다.
        OnBgmVolumeChanged(m_BgmSlider.value);
        OnSfxVolumeChanged(m_SfxSlider.value);
        OnBgmToggleChanged(m_BgmTog.isOn);
        OnSfxToggleChanged(m_SfxTog.isOn);
    }

    // UI가 꺼질 때: 현재 슬라이더 값과 토글 상태를 저장합니다.
    void OnDisable()
    {
        PlayerPrefs.SetInt("BgmVolume", (int)m_BgmSlider.value);
        PlayerPrefs.SetInt("SfxVolume", (int)m_SfxSlider.value);
        PlayerPrefs.SetInt("BgmOn", m_BgmTog.isOn ? 1 : 0);
        PlayerPrefs.SetInt("SfxOn", m_SfxTog.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    void Start()
    {
        //m_btngo.onClick.AddListener(onclickad);
    }

    // 슬라이더 범위를 0~100 정수로 설정하고, 슬라이더/토글의 값 변경 리스너를 연결합니다.
    void InitVolumeSliders()
    {
        m_BgmSlider.minValue = 0;
        m_BgmSlider.maxValue = 100;
        m_BgmSlider.wholeNumbers = true;
        m_SfxSlider.minValue = 0;
        m_SfxSlider.maxValue = 100;
        m_SfxSlider.wholeNumbers = true;

        m_BgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        m_SfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        m_BgmTog.onValueChanged.AddListener(OnBgmToggleChanged);
        m_SfxTog.onValueChanged.AddListener(OnSfxToggleChanged);
    }

    // 토글이 꺼지면 음소거(mute)합니다. 슬라이더 볼륨 값은 그대로 유지되어
    // 다시 켰을 때 이전 볼륨으로 돌아옵니다.
    void OnBgmToggleChanged(bool isOn)
    {
        m_BgmTogTxt.text = isOn ? "BGM ON" : "BGM OFF";
        if (m_BgmSource != null)
            m_BgmSource.mute = !isOn;
    }

    void OnSfxToggleChanged(bool isOn)
    {
        m_SfxTogTxt.text = isOn ? "SFX ON" : "SFX OFF";
        foreach (AudioSource src in m_SfxSources)
        {
            if (src != null)
                src.mute = !isOn;
        }
    }

    void OnBgmVolumeChanged(float value)
    {
        m_BgmVolTxt.text = "BGM : " + (int)value;
        if (m_BgmSource != null)
            m_BgmSource.volume = value / 100f;   // AudioSource.volume은 0~1
    }

    void OnSfxVolumeChanged(float value)
    {
        m_SfxVolTxt.text = "SFX : " + (int)value;
        foreach (AudioSource src in m_SfxSources)
        {
            if (src != null)
                src.volume = value / 100f;
        }
    }

    // 사망 패널 등에서 모든 BGM/SFX를 잠시 음소거 (저장된 볼륨/토글 설정은 건드리지 않음)
    public void MuteAll()
    {
        if (m_BgmSource != null)
            m_BgmSource.mute = true;
        foreach (AudioSource src in m_SfxSources)
        {
            if (src != null)
                src.mute = true;
        }
    }

    // 저장된 설정(BGM/SFX 토글 상태) 기준으로 음소거 상태 복원
    public void RestoreVolumes()
    {
        OnBgmToggleChanged(m_BgmTog.isOn);
        OnSfxToggleChanged(m_SfxTog.isOn);
    }
    void onclickad()
    {
        SceneManager.LoadScene("2FloorScene");
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            m_Setts.Activate();
            GameMgr.Inst().m_MonsterScene.m_MonsterMoves.m_IsActive = false; //상태 유지하는거 만들기
        }
    }

    // 항상 최상위에 그려지는 전체 화면 검은 이미지를 코드로 생성 (씬에 미리 배치할 필요 없음)
    void CreateFadeImage()
    {
        GameObject canvasGo = new GameObject("FadeCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        GameObject imgGo = new GameObject("FadeImage");
        imgGo.transform.SetParent(canvasGo.transform, false);
        m_FadeImage = imgGo.AddComponent<Image>();
        m_FadeImage.color = new Color(0f, 0f, 0f, 0f);
        m_FadeImage.raycastTarget = false;

        RectTransform rt = m_FadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // 화면이 완전히 덮인 순간 onCovered 실행, 다시 밝아진 뒤 onComplete 실행
    public void FadeTransition(Action onCovered, Action onComplete = null)
    {
        if (m_IsFading)
            return;
        StartCoroutine(FadeRoutine(onCovered, onComplete));
    }

    IEnumerator FadeRoutine(Action onCovered, Action onComplete)
    {
        m_IsFading = true;

        yield return Fade(0f, 1f);
        // 콜백에서 예외가 나도 페이드가 끝까지 진행되도록 보호합니다.
        // (예외로 코루틴이 죽으면 m_IsFading이 true로 남아 화면이 검은 채 조작 불능이 됨)
        try
        {
            onCovered?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        yield return new WaitForSeconds(m_HoldTime);
        yield return Fade(1f, 0f);

        m_IsFading = false;
        onComplete?.Invoke();
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < m_FadeTime)
        {
            t += Time.deltaTime;
            m_FadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / m_FadeTime));
            yield return null;
        }
        m_FadeImage.color = new Color(0f, 0f, 0f, to);
    }
}
