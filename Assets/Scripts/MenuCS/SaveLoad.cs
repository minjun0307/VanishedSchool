using UnityEngine;
using UnityEngine.UI;

public class SaveLoad : MonoBehaviour
{
    public Button m_btn;
    void Start()
    {
        m_btn.onClick.AddListener(onclick);      
    }
    void onclick()
    {
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
