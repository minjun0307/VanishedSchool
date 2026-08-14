using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 사망 패널: 켜질 때마다 "GAME OVER"만 표시하고,
// 로드 버튼으로 로드 패널을 열거나 나가기 버튼으로 메뉴 씬으로 돌아갑니다.
public class DeathPanel : MonoBehaviour
{
    const string GameOverText = "GAME OVER";   // 사망 시 표시할 문구

    public Text m_Text;                    // 문구가 출력될 UI Text

    // ── 버튼 ──
    public Button m_btnLoad;               // 로드 버튼 → 로드 패널 열기
    public Button m_btnExit;               // 나가기 버튼 → 메뉴 씬으로
    public LoadP m_LoadPanel;              // 로드 버튼이 열어줄 로드 패널

    void Start()
    {
        m_btnLoad.onClick.AddListener(OnClickLoad);
        m_btnExit.onClick.AddListener(OnClickExit);
    }

    void OnEnable()
    {
        // 사망 문구(랜덤 문구 + 타자기 출력)는 사용하지 않고 "GAME OVER"만 띄웁니다.
        if (m_Text != null)
            m_Text.text = GameOverText;
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
