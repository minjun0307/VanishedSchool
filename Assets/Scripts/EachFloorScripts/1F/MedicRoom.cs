using UnityEngine;

public class MedicRoom : MonoBehaviour, IRoom
{
    [SerializeField] string spotName = "Spot";
    [SerializeField] GameObject roomContent;  // 방 타일맵 오브젝트를 Inspector에서 지정

    public Vector3 Enter(PlayerMove player)
    {
        player.EnterRoom(Room.MedicRoom, Floor.F1);
        roomContent.SetActive(true);
        return roomContent.transform.Find(spotName).position;
    }

    public void Exit(PlayerMove player)
    {
        player.ExitRoom();
    }
}
