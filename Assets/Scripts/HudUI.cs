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

    // ── 상호작용 안내/메시지 표시 (Canvas > interactText) ──
    [Header("Interact Text (상호작용 안내 문구)")]
    public Text m_InteractText;   // Canvas > interactText — 상호작용 안내와 메시지가 여기 출력됩니다

    // ── 장착 중인 아이템 표시 (Canvas > Icons, 화면 좌측 하단) ──
    [Header("Equipped Icon (좌측 하단 장착 표시)")]
    public Image m_EquipIcon;      // Icons > EquipIcon — 장착 아이템 그림
    public Text m_EquipNameTxt;    // Icons > IconName — 장착 아이템 이름

    ItemData m_ShownItem;   // 지금 화면에 그려둔 아이템 (바뀔 때만 다시 그리기 위해 기억)

    Image m_FadeImage;
    bool m_IsFading;

    public bool IsFading { get { return m_IsFading; } }

    void Awake()
    {
        CreateFadeImage();
        // OnEnable(로드)보다 먼저 슬라이더 범위/리스너가 준비되어야 하므로 Awake에서 초기화합니다.
        InitVolumeSliders();
        // 게임을 시작할 때는 장착한 것이 없으므로 좌측 하단 표시를 비워둡니다.
        ApplyEquipToHud(null);
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
        Debug.Log("testVersionManager");
        Debug.Log("testVersionManager");
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
        // 캐비넷에 숨어 있는 동안에는 메뉴(인벤토리)를 열 수 없습니다.
        if (Input.GetKeyDown(KeyCode.B) && Cabinet.Hiding == null)
        {
            m_Setts.Activate();
            GameMgr.Inst().m_MonsterScene.m_MonsterMoves.m_IsActive = false; //상태 유지하는거 만들기
        }

        RefreshEquipIcon();
    }

    // 열려 있는 메뉴(인벤토리 등)를 닫습니다. — 캐비넷에 숨을 때 호출됩니다.
    public void CloseMenu()
    {
        if (m_Setts != null)
            m_Setts.Deactivate();
    }

    // 상호작용 안내 문구/메시지를 화면에 표시합니다. (PlayerInventory가 매 프레임 넘겨줍니다)
    // 빈 문자열을 넣으면 아무것도 보이지 않습니다.
    // 같은 문구가 이어질 때 Text에 다시 넣으면 UI를 매번 새로 그리게 되므로, 바뀔 때만 반영합니다.
    public void ShowInteract(string message)
    {
        if (m_InteractText == null || m_InteractText.text == message)
            return;

        m_InteractText.text = message;
    }

    // 장착 아이템이 바뀌었을 때만 좌측 하단 표시를 다시 그립니다.
    // (매 프레임 참조 하나만 비교하므로 부담이 없습니다)
    void RefreshEquipIcon()
    {
        ItemData equipped = AssetMgr.Inst().GetEquipped();
        if (equipped == m_ShownItem)
            return;

        ApplyEquipToHud(equipped);
    }

    // Icons 밑의 EquipIcon(그림)과 IconName(이름)에 장착 아이템을 반영합니다.
    // 장착한 것이 없으면 그림을 감추고 이름을 지웁니다.
    void ApplyEquipToHud(ItemData equipped)
    {
        m_ShownItem = equipped;

        if (m_EquipIcon != null)
        {
            m_EquipIcon.sprite = equipped != null ? equipped.m_Icon : null;
            // 장착했더라도 아이콘 그림이 없으면 흰 네모가 뜨므로 함께 감춥니다.
            m_EquipIcon.enabled = m_EquipIcon.sprite != null;
        }

        if (m_EquipNameTxt != null)
            m_EquipNameTxt.text = equipped != null ? equipped.m_Name : "";
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
