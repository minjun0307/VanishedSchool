using System.Collections.Generic;

// 아이템/인벤토리를 관리하는 싱글톤 (GameMgr와 같은 패턴)
// 추후 아이템 시스템이 생기면 아이템 이름(또는 ID 문자열)을 그대로 넣어서 쓰면 됩니다.
// 예) AssetMgr.Inst().AddItem("Key");  AssetMgr.Inst().HasItem("Flashlight")
public class AssetMgr
{
    static AssetMgr inst = new AssetMgr();

    public static AssetMgr Inst()
    {
        if (inst == null)
            inst = new AssetMgr();

        return inst;
    }

    // 플레이어가 수집한 아이템 목록 (= 플레이어의 인벤토리)
    public List<string> m_Inventory = new List<string>();

    public void AddItem(string itemName)
    {
        m_Inventory.Add(itemName);
    }

    public bool HasItem(string itemName)
    {
        return m_Inventory.Contains(itemName);
    }

    public void RemoveItem(string itemName)
    {
        m_Inventory.Remove(itemName);
    }

    // ── 세이브/로드 연동 (SaveFileMgr가 호출) ──
    public List<string> GetInventoryForSave()
    {
        return new List<string>(m_Inventory);
    }

    public void SetInventoryFromLoad(List<string> items)
    {
        m_Inventory = items != null ? new List<string>(items) : new List<string>();
    }
}
