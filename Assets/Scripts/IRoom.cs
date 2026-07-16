using UnityEngine;

// IRoom.cs - 방 하나가 지켜야 할 규칙만 정의
public interface IRoom
{
    Vector3 Enter(PlayerMove player);  // 들어갈 때 이동할 위치 반환
    void Exit(PlayerMove player);       // 나갈 때 정리
}
