using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 사망 패널: 켜질 때마다 DeathMessageData(스크립터블 오브젝트)의 문구 중 하나를 랜덤으로 골라
// 한 글자씩 출력하고, 로드 버튼으로 로드 패널을 열거나 나가기 버튼으로 메뉴 씬으로 돌아갑니다.
public class DeathPanel : MonoBehaviour
{
    // ── 사망 문구 타자기 출력 ──
    public Text m_Text;                    // 문구가 출력될 UI Text
    public DeathMessageMgr m_MsgMgr;       // 사망 문구를 관리하는 매니저 (HudUI 자식)
    public float m_CharInterval = 0.05f;   // 글자 사이 간격 (초)

    // ── 버튼 ──
    public Button m_btnLoad;               // 로드 버튼 → 로드 패널 열기
    public Button m_btnExit;               // 나가기 버튼 → 메뉴 씬으로
    public LoadP m_LoadPanel;              // 로드 버튼이 열어줄 로드 패널

    Coroutine m_Typing;

    void Start()
    {
        m_btnLoad.onClick.AddListener(OnClickLoad);
        m_btnExit.onClick.AddListener(OnClickExit);
    }

    void OnEnable()
    {
        PlayRandom();   // 패널이 켜질 때마다 새 문구를 랜덤으로 출력
    }

    // 문구 하나를 랜덤으로 골라 처음부터 출력 시작
    public void PlayRandom()
    {
        if (m_MsgMgr == null)
        {
            Debug.LogWarning("DeathPanel: m_MsgMgr가 연결되지 않았습니다.");
            return;
        }

        string picked = m_MsgMgr.GetRandomMessage();
        if (string.IsNullOrEmpty(picked))
            return;

        if (m_Typing != null)
            StopCoroutine(m_Typing);
        m_Typing = StartCoroutine(TypeText(picked));
    }

    // 한 글자씩 이어 붙이며 출력
    IEnumerator TypeText(string full)
    {
        m_Text.text = "";
        foreach (char c in full)
        {
            m_Text.text += c;
            yield return new WaitForSeconds(m_CharInterval);
        }
        m_Typing = null;
    }

    void OnClickLoad()
    {
        m_LoadPanel.gameObject.SetActive(true);
    }

    void OnClickExit()
    {
        SceneManager.LoadScene("MenuScene");
    }
}
