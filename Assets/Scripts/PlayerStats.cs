using UnityEngine;
using UnityEngine.UI;

// 플레이어 이중자원(HP/STR) 관리 스크립트 (플레이어 오브젝트에 부착)
// - HP/STR은 반드시 '정수' 자원으로 유지합니다.
// - 슬라이더 + Fill 이미지(HP 빨강 / STR 파랑) + 수치 텍스트로 표시합니다.
// - 스테미나는 달리는 동안(Shift+이동)에만 초당 m_StrDrainNormal씩 감소,
//   m_StrFastThreshold 이하가 되면 초당 m_StrDrainFast씩 감소합니다.
// - HP는 몬스터가 공격했을 때 TakeDamage(정수 데미지)로만 감소합니다.
public class PlayerStats : MonoBehaviour
{
    [Header("Resources (정수 자원)")]
    public int m_MaxHp = 100;    // HP 기본(최대)값
    public int m_MaxStr = 100;   // STR 기본(최대)값

    [Header("Stamina Drain (달리는 동안 초당 감소량 — 에디터에서 조절)")]
    public int m_StrDrainNormal = 2;     // 달리는 동안: 초당 2씩 감소
    public int m_StrDrainFast = 4;       // 임계값 이하: 초당 4씩 감소
    public int m_StrFastThreshold = 40;  // 스테미나가 이 값이 되면 빠른 감소로 전환

    [Header("UI")]
    public Slider m_HpSlider;    // HP 슬라이더
    public Slider m_StrSlider;   // STR 슬라이더
    public Image m_HpFill;       // HP 슬라이더의 Fill 이미지 (빨간색으로 표시)
    public Image m_StrFill;      // STR 슬라이더의 Fill 이미지 (파란색으로 표시)
    public Text m_HpText;        // 현재 HP 수치 텍스트
    public Text m_StrText;       // 현재 STR 수치 텍스트

    int m_Hp;         // 현재 HP (정수)
    int m_Str;        // 현재 STR (정수)
    float m_strAccum; // STR을 정수로 유지하기 위한 1 미만 감소분 누적 버퍼
    PlayerMove m_Move; // 달리기 중인지 확인용 (같은 오브젝트의 PlayerMove)

    // 캐비넷에 숨어 있는 동안 true — 그 사이에는 어떤 피해도 받지 않습니다. (Cabinet이 켜고 끕니다)
    [HideInInspector] public bool m_Invincible;

    public int Hp { get { return m_Hp; } }
    public int Str { get { return m_Str; } }

    void Start()
    {
        m_Move = GetComponent<PlayerMove>();
        m_Hp = m_MaxHp;
        m_Str = m_MaxStr;

        if (m_HpFill != null) m_HpFill.color = Color.red;
        if (m_StrFill != null) m_StrFill.color = Color.blue;
        InitSlider(m_HpSlider, m_MaxHp);
        InitSlider(m_StrSlider, m_MaxStr);
        RefreshUI();
    }

    void InitSlider(Slider slider, int max)
    {
        if (slider == null) return;
        slider.minValue = 0;
        slider.maxValue = max;
        slider.wholeNumbers = true;   // 정수 자원이므로 슬라이더도 정수 단위
        slider.value = max;
        slider.interactable = false;  // 표시 전용 (마우스로 조작 불가)
    }

    void Update()
    {
        DrainStamina();
    }

    // 달리는 동안(Shift+이동)에만 스테미나 감소. 정수 자원을 유지하기 위해
    // 프레임마다의 소수 감소분을 누적했다가 1 이상이 됐을 때만 정수로 뺍니다.
    void DrainStamina()
    {
        if (m_Str <= 0)
            return;

        // 달리기 중이 아니면 소모하지 않음
        if (m_Move == null || !m_Move.IsRunning)
            return;

        int rate = m_Str <= m_StrFastThreshold ? m_StrDrainFast : m_StrDrainNormal;
        m_strAccum += rate * Time.deltaTime;
        if (m_strAccum < 1f)
            return;

        int amount = (int)m_strAccum;
        m_strAccum -= amount;
        m_Str = Mathf.Max(0, m_Str - amount);
        RefreshUI();
    }

    // 몬스터가 플레이어를 공격했을 때 호출 (데미지는 정수)
    public void TakeDamage(int damage)
    {
        if (m_Invincible)
            return;   // 캐비넷에 숨어 있는 동안은 무적

        if (m_Hp <= 0)
            return;   // 이미 사망 상태면 중복 처리 방지

        m_Hp = Mathf.Max(0, m_Hp - damage);
        RefreshUI();

        // HP가 0이 되면 사망: BattleFSM을 결과 상태로 전환
        // (DeathPanel 표시/플레이어·몬스터 정지는 GameScene의 Result 콜백이 처리)
        if (m_Hp <= 0)
        {
            GameScene gameScene = GameMgr.Inst().m_GameScene;
            if (gameScene != null)
                gameScene.m_BattleFSM.SetResultState();
        }
    }

    void RefreshUI()
    {
        if (m_HpSlider != null) m_HpSlider.value = m_Hp;
        if (m_StrSlider != null) m_StrSlider.value = m_Str;
        if (m_HpText != null) m_HpText.text = "HP " + m_Hp;
        if (m_StrText != null) m_StrText.text = "STR " + m_Str;
    }
}
