using UnityEngine;

public class Floor3 : MonoBehaviour
{
    public GameObject m_MainMap;
    public F3C1 m_f3c1;
    public F3C2 m_f3c2;
    void Start()
    {
        
    }
    public Vector3 F3Pos()
    {
        gameObject.SetActive(true);
        return transform.Find("F3Spot").transform.position;
    }
    public Vector3 F3Pos2()
    {
        gameObject.SetActive(true);
        return transform.Find("F3Spot2").transform.position;
    }
    public void MapEnable()
    {
        m_MainMap.SetActive(true);
    }
    void Update()
    {
        
    }
}
