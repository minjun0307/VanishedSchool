using UnityEngine;

public class Floor2 : MonoBehaviour
{
    public GameObject m_MainMap;
    public TeacherRoom m_TeacherR;
    public Princple m_PrincR;
    public C1 m_C1;
    public C2 m_C2;
    public ToiletMale m_ToiletMale;
    public ToiletWomen m_ToiletWomen;
    public MapTransfer m_Stair2;
    public bool m_CurStF2;
    public bool m_IsWayUp;
    void Start()
    {
        
    }
    public Vector3 F2Pos()
    {
        gameObject.SetActive(true);
        return gameObject.transform.Find("F2Spot").transform.position;

    }
    public Vector3 F2Pos2()
    {
        gameObject.SetActive(true);
        return gameObject.transform.Find("F2Spot2").transform.position;
    }
    public Vector3 F2Pos3()
    {
        gameObject.SetActive(true);
        return gameObject.transform.Find("F2Spot3").transform.position;
    }
    public void OnMap()
    {
        gameObject.SetActive(true);
        m_MainMap.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
