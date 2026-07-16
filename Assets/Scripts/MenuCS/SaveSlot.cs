using UnityEngine;
using UnityEngine.UI;

// 세이브/로드 ScrollView 안에 for문으로 생성되는 슬롯 1칸 (프리팹에 붙이는 스크립트)
public class SaveSlot : MonoBehaviour
{
    public Text m_Label;      // 슬롯에 표시될 텍스트 ("Save 1" 등)
    public Text m_Time;       // 저장된 플레이시간 표시용 텍스트 ("00:00" 등)
    public Button m_Button;   // 슬롯 클릭용 버튼

    int m_SlotNum;                    // 이 슬롯이 담당하는 파일 번호 (1~4)
    System.Action<int> m_OnClick;     // 클릭 시 슬롯 번호를 넘겨줄 콜백 (SaveP/LoadP가 지정)

    public void Initialize(int slotNum, string label, System.Action<int> onClick)
    {
        m_SlotNum = slotNum;
        m_OnClick = onClick;
        m_Label.text = label;
        m_Button.onClick.AddListener(OnClickSlot);
    }

    // 플레이시간 텍스트 갱신 (저장/M키 초기화 후 호출됨)
    public void SetTime(string time)
    {
        if (m_Time != null)   // 프리팹에 시간 Text가 연결 안 돼 있으면 무시
            m_Time.text = time;
    }

    void OnClickSlot()
    {
        if (m_OnClick != null)
            m_OnClick(m_SlotNum);
    }
}
