using System;
using Unity.VisualScripting;
using UnityEngine;

public class Floor1 : MonoBehaviour
{
    public GameObject m_Mapobj;
    public F1C1 m_C1;
    public MedicRoom m_Medic;
    public F1C2 m_C2;
    public MapTransfer m_Stairs;
    public Utillity m_Utill;
    public bool m_CurStF1;


    void Start()
    {
        
    }
    public Vector3 F1Pos()   // 어차피 임마가 호출될때는 내려갈때만 호출될거라 d로 가져옴
    {
        gameObject.SetActive(true);
        return gameObject.transform.Find("F1SpotD").transform.position;
    }
    public Vector3 F1Pos2()   // 어차피 임마가 호출될때는 내려갈때만 호출될거라 d로 가져옴
    {
        gameObject.SetActive(true);
        return gameObject.transform.Find("F1SpotD2").transform.position;
    }
    public void MainMapEnable()
    {
        m_Mapobj.SetActive(true);
    }
   
    
    void Update()
    {
        
    }
}
