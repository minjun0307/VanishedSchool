using UnityEngine;

public class BroadCast : MonoBehaviour, IRoom
{
    [SerializeField] string spotName = "Spot";
    [SerializeField] GameObject roomContent;

    public Vector3 Enter(PlayerMove player)
    {
        player.EnterRoom(Room.BroadCast, Floor.F3);
        roomContent.SetActive(true);
        return roomContent.transform.Find(spotName).position;
    }

    public void Exit(PlayerMove player)
    {
        player.ExitRoom();
    }
}
