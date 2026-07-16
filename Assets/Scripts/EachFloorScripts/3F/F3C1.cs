using UnityEngine;

public class F3C1 : MonoBehaviour, IRoom
{
    [SerializeField] string spotName = "Spot";
    [SerializeField] GameObject roomContent;

    public Vector3 Enter(PlayerMove player)
    {
        player.EnterRoom(Room.F3C1, Floor.F3);
        roomContent.SetActive(true);
        return roomContent.transform.Find(spotName).position;
    }

    public void Exit(PlayerMove player)
    {
        player.ExitRoom();
    }
}
