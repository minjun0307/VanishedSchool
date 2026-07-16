using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public Buttons m_Buttons;
    public Panels m_Panels;
    void Start()
    {
    }
   
    public void Activate()
    {
        gameObject.SetActive(true);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            gameObject.SetActive(false);
    }
}

