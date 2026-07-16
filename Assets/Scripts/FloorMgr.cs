using UnityEngine;

public class FloorMgr : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    // 아래 메서드들은 기존에 각 방별로 직접 처리하던 로직입니다.
    // IRoom 리팩토링 후에는 PlayerMove.OnCollisionEnter2D에서 
    // TryGetComponent<IRoom>()으로 자동 처리되므로, 외부에서 직접 호출할 경우에만 사용합니다.
    // 필요에 따라 삭제하거나 IRoom 기반으로 호출하도록 변경하세요.
}
