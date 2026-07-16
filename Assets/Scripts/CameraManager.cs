using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public GameObject Target;
    public float m_Speed;
    private Vector3 targetPos;
    void Start()
    {
        
    }

    // LateUpdate is called after all Update functions have been called
    void LateUpdate()
    {
        if (Target != null)
        {
            targetPos.Set(Target.transform.position.x, Target.transform.position.y, this.transform.position.z);

            this.transform.position = Vector3.Lerp(this.transform.position, targetPos, m_Speed*Time.deltaTime) ;
        }
                
    }
}
