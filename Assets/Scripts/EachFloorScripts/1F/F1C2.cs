using UnityEngine;

public class F1C2 : MonoBehaviour, IRoom
{
    [SerializeField] string spotName = "FSpot";
    [SerializeField] GameObject roomContent;

    public Vector3 Enter(PlayerMove player)
    {
        player.EnterRoom(Room.F1C2, Floor.F1);
        roomContent.SetActive(true);
        return roomContent.transform.Find(spotName).position;
    }

    public void Exit(PlayerMove player)
    {
        player.ExitRoom();
    }
}
