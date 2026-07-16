using UnityEngine;

// 세이브 패널: ScrollView에 슬롯 4개를 생성하고, 클릭하면 해당 번호 파일에 저장합니다.
public class SaveP : MonoBehaviour
{
    public SaveSlot m_SlotPrefab;          // 슬롯 프리팹 (Button + Text)
    public Transform m_Content;            // ScrollView > Viewport > Content
    public string m_LabelPrefix = "Save";  // 슬롯 텍스트 앞부분 → "Save 1" ~ "Save 4"

    SaveSlot[] m_Slots = new SaveSlot[SaveFileMgr.SlotCount];   // 시간 표시 갱신용 (인덱스 0 = 슬롯 1)

    void Start()
    {
        for (int i = 1; i <= SaveFileMgr.SlotCount; i++)
        {
            SaveSlot slot = Instantiate(m_SlotPrefab, m_Content);
            slot.Initialize(i, m_LabelPrefix + " " + i, OnClickSlot);
            slot.SetTime(SaveFileMgr.GetSlotTimeText(i));   // 저장 파일 없으면 00:00
            m_Slots[i - 1] = slot;
        }
    }

    void OnEnable()
    {
        RefreshTimes();   // 패널 열 때마다 저장된 플레이시간 최신화
    }

    // 슬롯 플레이시간 표시 전체 갱신 (M키 초기화 후에도 호출됨)
    public void RefreshTimes()
    {
        for (int i = 1; i <= SaveFileMgr.SlotCount; i++)
        {
            if (m_Slots[i - 1] != null)
                m_Slots[i - 1].SetTime(SaveFileMgr.GetSlotTimeText(i));
        }
    }

    void OnClickSlot(int slotNum)
    {
        SaveFileMgr.Save(slotNum);
        RefreshTimes();   // 방금 저장한 슬롯의 플레이시간 표시
    }
}
